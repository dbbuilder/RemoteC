using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RemoteC.Data;
using RemoteC.Data.Entities;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Services
{
    public class PolicyEngineService : IPolicyEngineService
    {
        private readonly RemoteCDbContext _context;
        private readonly ILogger<PolicyEngineService> _logger;
        private readonly IAuditService _auditService;
        private readonly ICacheService _cacheService;
        private readonly PolicyEngineOptions _options;
        private readonly IMetricsCollector _metricsCollector;

        public PolicyEngineService(
            RemoteCDbContext context,
            ILogger<PolicyEngineService> logger,
            IAuditService auditService,
            ICacheService cacheService,
            IOptions<PolicyEngineOptions> options,
            IMetricsCollector? metricsCollector = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _metricsCollector = metricsCollector ?? new NullMetricsCollector();
        }

        #region Policy Management

        public async Task<Policy> CreatePolicyAsync(PolicyDefinition definition)
        {
            // Validate policy
            var validationResult = await ValidatePolicyAsync(definition);
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException($"Invalid policy: {string.Join(", ", validationResult.Errors)}");
            }

            // Check for duplicate name
            var existingPolicy = await _context.Policies
                .FirstOrDefaultAsync(p => p.Name == definition.Name);
            
            if (existingPolicy != null)
            {
                throw new InvalidOperationException($"Policy with name '{definition.Name}' already exists");
            }

            var policyEntity = new PolicyEntity
            {
                Id = Guid.NewGuid(),
                Name = definition.Name,
                Description = definition.Description,
                Effect = definition.Effect.ToString(),
                Resources = JsonSerializer.Serialize(definition.Resources),
                Actions = JsonSerializer.Serialize(definition.Actions),
                Conditions = definition.Conditions != null ? JsonSerializer.Serialize(definition.Conditions) : null,
                Principals = definition.Principals != null ? JsonSerializer.Serialize(definition.Principals) : null,
                NotPrincipals = definition.NotPrincipals != null ? JsonSerializer.Serialize(definition.NotPrincipals) : null,
                Priority = definition.Priority,
                IsActive = true,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system", // TODO: Get from current user context
                Tags = definition.Tags != null ? JsonSerializer.Serialize(definition.Tags) : null
            };

            _context.Policies.Add(policyEntity);
            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync(
                "policy.created",
                "Policy",
                policyEntity.Id.ToString(),
                null,
                policyEntity,
                new { policyName = policyEntity.Name });

            return MapToPolicy(policyEntity);
        }

        public async Task<Policy> UpdatePolicyAsync(Guid policyId, PolicyDefinition definition)
        {
            var policyEntity = await _context.Policies.FindAsync(policyId);
            if (policyEntity == null)
            {
                throw new InvalidOperationException($"Policy {policyId} not found");
            }

            // Validate policy
            var validationResult = await ValidatePolicyAsync(definition);
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException($"Invalid policy: {string.Join(", ", validationResult.Errors)}");
            }

            // Update fields
            policyEntity.Name = definition.Name;
            policyEntity.Description = definition.Description;
            policyEntity.Effect = definition.Effect.ToString();
            policyEntity.Resources = JsonSerializer.Serialize(definition.Resources);
            policyEntity.Actions = JsonSerializer.Serialize(definition.Actions);
            policyEntity.Conditions = definition.Conditions != null ? JsonSerializer.Serialize(definition.Conditions) : null;
            policyEntity.Principals = definition.Principals != null ? JsonSerializer.Serialize(definition.Principals) : null;
            policyEntity.NotPrincipals = definition.NotPrincipals != null ? JsonSerializer.Serialize(definition.NotPrincipals) : null;
            policyEntity.Priority = definition.Priority;
            policyEntity.Tags = definition.Tags != null ? JsonSerializer.Serialize(definition.Tags) : null;
            policyEntity.Version++;
            policyEntity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Clear cache
            await _cacheService.RemoveAsync($"policy:{policyId}");

            await _auditService.LogActionAsync(
                "policy.updated",
                "Policy",
                policyId.ToString(),
                null,
                new { version = policyEntity.Version },
                new { policyName = policyEntity.Name });

            return MapToPolicy(policyEntity);
        }

        public async Task<bool> DeletePolicyAsync(Guid policyId)
        {
            var policyEntity = await _context.Policies
                .Include(p => p.RolePolicies)
                .Include(p => p.UserPolicyAssignments)
                .Include(p => p.GroupPolicyAssignments)
                .FirstOrDefaultAsync(p => p.Id == policyId);

            if (policyEntity == null)
            {
                return false;
            }

            // Check if policy is in use
            if (policyEntity.RolePolicies.Any() || 
                policyEntity.UserPolicyAssignments.Any() || 
                policyEntity.GroupPolicyAssignments.Any())
            {
                throw new InvalidOperationException("Cannot delete policy that is in use");
            }

            _context.Policies.Remove(policyEntity);
            await _context.SaveChangesAsync();

            // Clear cache
            await _cacheService.RemoveAsync($"policy:{policyId}");

            await _auditService.LogActionAsync(
                "policy.deleted",
                "Policy",
                policyId.ToString(),
                policyEntity,
                null,
                new { policyName = policyEntity.Name });

            return true;
        }

        public async Task<Policy?> GetPolicyAsync(Guid policyId)
        {
            var cacheKey = $"policy:{policyId}";
            var cached = await _cacheService.GetAsync<Policy>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            var policyEntity = await _context.Policies.FindAsync(policyId);
            if (policyEntity == null)
            {
                return null;
            }

            var policy = MapToPolicy(policyEntity);
            await _cacheService.SetAsync(cacheKey, policy, TimeSpan.FromMinutes(_options.PolicyCacheDurationMinutes));

            return policy;
        }

        public async Task<List<Policy>> GetPoliciesAsync(string? resource = null, string? action = null)
        {
            var query = _context.Policies.AsQueryable();

            if (!string.IsNullOrEmpty(resource))
            {
                query = query.Where(p => p.Resources.Contains(resource));
            }

            if (!string.IsNullOrEmpty(action))
            {
                query = query.Where(p => p.Actions.Contains(action));
            }

            var policies = await query.ToListAsync();
            return policies.Select(MapToPolicy).ToList();
        }

        public async Task<PolicyValidationResult> ValidatePolicyAsync(PolicyDefinition definition)
        {
            var result = new PolicyValidationResult { IsValid = true };

            // Validate required fields
            if (string.IsNullOrWhiteSpace(definition.Name))
            {
                result.IsValid = false;
                result.Errors.Add("Policy name is required");
            }

            if (definition.Resources == null || definition.Resources.Length == 0)
            {
                result.IsValid = false;
                result.Errors.Add("At least one resource is required");
            }

            if (definition.Actions == null || definition.Actions.Length == 0)
            {
                result.IsValid = false;
                result.Errors.Add("At least one action is required");
            }

            // Validate resource patterns
            if (definition.Resources != null)
            {
                foreach (var resource in definition.Resources)
                {
                    if (!IsValidResourcePattern(resource))
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Invalid resource pattern: {resource}");
                    }
                }
            }

            // Validate condition complexity
            if (_options.EnablePolicyValidation && definition.Conditions != null)
            {
                var complexity = CalculateConditionComplexity(definition.Conditions);
                if (complexity > _options.MaxConditionComplexity)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Condition complexity ({complexity}) exceeds maximum allowed ({_options.MaxConditionComplexity})");
                }
            }

            return await Task.FromResult(result);
        }

        #endregion

        #region Role Management

        public async Task<RemoteC.Shared.Models.Role> CreateRoleAsync(RoleDefinition definition)
        {
            // Check for duplicate name
            var existingRole = await _context.PolicyRoles
                .FirstOrDefaultAsync(r => r.Name == definition.Name);
            
            if (existingRole != null)
            {
                throw new InvalidOperationException($"Role with name '{definition.Name}' already exists");
            }

            var roleEntity = new PolicyRoleEntity
            {
                Id = Guid.NewGuid(),
                Name = definition.Name,
                Description = definition.Description,
                IsActive = true,
                IsSystem = definition.IsSystem,
                CreatedAt = DateTime.UtcNow,
                Tags = definition.Tags != null ? JsonSerializer.Serialize(definition.Tags) : null
            };

            _context.PolicyRoles.Add(roleEntity);

            // Attach policies
            foreach (var policyId in definition.PolicyIds)
            {
                var policyExists = await _context.Policies.AnyAsync(p => p.Id == policyId);
                if (!policyExists)
                {
                    throw new InvalidOperationException($"Policy {policyId} not found");
                }

                _context.RolePolicies.Add(new RolePolicyEntity
                {
                    RoleId = roleEntity.Id,
                    PolicyId = policyId
                });
            }

            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync(
                "role.created",
                "Role",
                roleEntity.Id.ToString(),
                null,
                roleEntity,
                new { roleName = roleEntity.Name });

            return await GetRoleAsync(roleEntity.Id) ?? throw new InvalidOperationException("Failed to retrieve created role");
        }

        public async Task<RemoteC.Shared.Models.Role> UpdateRoleAsync(Guid roleId, RoleDefinition definition)
        {
            var roleEntity = await _context.PolicyRoles
                .Include(r => r.RolePolicies)
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (roleEntity == null)
            {
                throw new InvalidOperationException($"Role {roleId} not found");
            }

            if (roleEntity.IsSystem)
            {
                throw new InvalidOperationException("Cannot modify system roles");
            }

            // Update fields
            roleEntity.Name = definition.Name;
            roleEntity.Description = definition.Description;
            roleEntity.Tags = definition.Tags != null ? JsonSerializer.Serialize(definition.Tags) : null;
            roleEntity.UpdatedAt = DateTime.UtcNow;

            // Update policies
            _context.RolePolicies.RemoveRange(roleEntity.RolePolicies);

            foreach (var policyId in definition.PolicyIds)
            {
                var policyExists = await _context.Policies.AnyAsync(p => p.Id == policyId);
                if (!policyExists)
                {
                    throw new InvalidOperationException($"Policy {policyId} not found");
                }

                _context.RolePolicies.Add(new RolePolicyEntity
                {
                    RoleId = roleEntity.Id,
                    PolicyId = policyId
                });
            }

            await _context.SaveChangesAsync();

            // Clear cache
            await _cacheService.RemoveAsync($"role:{roleId}");

            await _auditService.LogActionAsync(
                "role.updated",
                "Role",
                roleId.ToString(),
                null,
                roleEntity,
                new { roleName = roleEntity.Name });

            return await GetRoleAsync(roleEntity.Id) ?? throw new InvalidOperationException("Failed to retrieve updated role");
        }

        public async Task<bool> DeleteRoleAsync(Guid roleId)
        {
            var roleEntity = await _context.PolicyRoles
                .Include(r => r.UserRoles)
                .Include(r => r.GroupRoles)
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (roleEntity == null)
            {
                return false;
            }

            if (roleEntity.IsSystem)
            {
                throw new InvalidOperationException("Cannot delete system roles");
            }

            // Check if role is in use
            if (roleEntity.UserRoles.Any() || roleEntity.GroupRoles.Any())
            {
                throw new InvalidOperationException("Cannot delete role that is in use");
            }

            _context.PolicyRoles.Remove(roleEntity);
            await _context.SaveChangesAsync();

            // Clear cache
            await _cacheService.RemoveAsync($"role:{roleId}");

            await _auditService.LogActionAsync(
                "role.deleted",
                "Role",
                roleId.ToString(),
                roleEntity,
                null,
                new { roleName = roleEntity.Name });

            return true;
        }

        public async Task<RemoteC.Shared.Models.Role?> GetRoleAsync(Guid roleId)
        {
            var cacheKey = $"role:{roleId}";
            var cached = await _cacheService.GetAsync<RemoteC.Shared.Models.Role>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            var roleEntity = await _context.PolicyRoles
                .Include(r => r.RolePolicies)
                    .ThenInclude(rp => rp.Policy)
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (roleEntity == null)
            {
                return null;
            }

            var role = MapToRole(roleEntity);
            await _cacheService.SetAsync(cacheKey, role, TimeSpan.FromMinutes(_options.PolicyCacheDurationMinutes));

            return role;
        }

        public async Task<List<RemoteC.Shared.Models.Role>> GetRolesAsync()
        {
            var roles = await _context.PolicyRoles
                .Include(r => r.RolePolicies)
                .ToListAsync();

            return roles.Select(MapToRole).ToList();
        }

        public async Task<bool> AttachPolicyToRoleAsync(Guid roleId, Guid policyId)
        {
            var roleExists = await _context.PolicyRoles.AnyAsync(r => r.Id == roleId);
            var policyExists = await _context.Policies.AnyAsync(p => p.Id == policyId);

            if (!roleExists || !policyExists)
            {
                return false;
            }

            var existing = await _context.RolePolicies
                .AnyAsync(rp => rp.RoleId == roleId && rp.PolicyId == policyId);

            if (existing)
            {
                return true;
            }

            _context.RolePolicies.Add(new RolePolicyEntity
            {
                RoleId = roleId,
                PolicyId = policyId
            });

            await _context.SaveChangesAsync();

            // Clear cache
            await _cacheService.RemoveAsync($"role:{roleId}");

            await _auditService.LogActionAsync(
                "role.policy_attached",
                "Role",
                roleId.ToString(),
                null,
                new { policyId },
                null);

            return true;
        }

        public async Task<bool> DetachPolicyFromRoleAsync(Guid roleId, Guid policyId)
        {
            var rolePolicy = await _context.RolePolicies
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PolicyId == policyId);

            if (rolePolicy == null)
            {
                return false;
            }

            _context.RolePolicies.Remove(rolePolicy);
            await _context.SaveChangesAsync();

            // Clear cache
            await _cacheService.RemoveAsync($"role:{roleId}");

            await _auditService.LogActionAsync(
                "role.policy_detached",
                "Role",
                roleId.ToString(),
                new { policyId },
                null,
                null);

            return true;
        }

        #endregion

        #region User and Group Assignments

        public async Task<bool> AssignRoleToUserAsync(Guid userId, Guid roleId)
        {
            var roleExists = await _context.PolicyRoles.AnyAsync(r => r.Id == roleId);
            if (!roleExists)
            {
                return false;
            }

            var existing = await _context.UserPolicyRoles
                .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (existing)
            {
                return true;
            }

            _context.UserPolicyRoles.Add(new UserPolicyRoleEntity
            {
                UserId = userId,
                RoleId = roleId,
                AssignedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Clear cache
            await ClearUserCacheAsync(userId);

            await _auditService.LogActionAsync(
                "user.role_assigned",
                "User",
                userId.ToString(),
                null,
                new { roleId },
                null);

            return true;
        }

        public async Task<bool> RemoveRoleFromUserAsync(Guid userId, Guid roleId)
        {
            var userRole = await _context.UserPolicyRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (userRole == null)
            {
                return false;
            }

            _context.UserPolicyRoles.Remove(userRole);
            await _context.SaveChangesAsync();

            // Clear cache
            await ClearUserCacheAsync(userId);

            await _auditService.LogActionAsync(
                "user.role_removed",
                "User",
                userId.ToString(),
                new { roleId },
                null,
                null);

            return true;
        }

        public async Task<bool> AssignPolicyToUserAsync(Guid userId, Guid policyId, DateTime? expiresAt = null)
        {
            var policyExists = await _context.Policies.AnyAsync(p => p.Id == policyId);
            if (!policyExists)
            {
                return false;
            }

            var existing = await _context.UserPolicyAssignments
                .FirstOrDefaultAsync(upa => upa.UserId == userId && upa.PolicyId == policyId);

            if (existing != null)
            {
                // Update expiration if needed
                existing.ExpiresAt = expiresAt;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.UserPolicyAssignments.Add(new UserPolicyAssignmentEntity
                {
                    UserId = userId,
                    PolicyId = policyId,
                    AssignedAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt,
                    AssignedBy = "system" // TODO: Get from current user context
                });
            }

            await _context.SaveChangesAsync();

            // Clear cache
            await ClearUserCacheAsync(userId);

            await _auditService.LogActionAsync(
                "user.policy_assigned",
                "User",
                userId.ToString(),
                null,
                new { policyId, expiresAt },
                null);

            return true;
        }

        public async Task<bool> RemovePolicyFromUserAsync(Guid userId, Guid policyId)
        {
            var assignment = await _context.UserPolicyAssignments
                .FirstOrDefaultAsync(upa => upa.UserId == userId && upa.PolicyId == policyId);

            if (assignment == null)
            {
                return false;
            }

            _context.UserPolicyAssignments.Remove(assignment);
            await _context.SaveChangesAsync();

            // Clear cache
            await ClearUserCacheAsync(userId);

            await _auditService.LogActionAsync(
                "user.policy_removed",
                "User",
                userId.ToString(),
                new { policyId },
                null,
                null);

            return true;
        }

        public async Task<List<RemoteC.Shared.Models.Role>> GetUserRolesAsync(Guid userId)
        {
            var cacheKey = $"user:roles:{userId}";
            var cached = await _cacheService.GetAsync<List<RemoteC.Shared.Models.Role>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            var userRoles = await _context.UserPolicyRoles
                .Include(ur => ur.Role)
                    .ThenInclude(r => r.RolePolicies)
                        .ThenInclude(rp => rp.Policy)
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.Role)
                .ToListAsync();

            var roles = userRoles.Select(MapToRole).ToList();
            await _cacheService.SetAsync(cacheKey, roles, TimeSpan.FromMinutes(_options.PolicyCacheDurationMinutes));

            return roles;
        }

        public async Task<List<Policy>> GetUserPoliciesAsync(Guid userId)
        {
            var cacheKey = $"user:policies:{userId}";
            var cached = await _cacheService.GetAsync<List<Policy>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            // Direct policies
            var directPolicies = await _context.UserPolicyAssignments
                .Include(upa => upa.Policy)
                .Where(upa => upa.UserId == userId && (upa.ExpiresAt == null || upa.ExpiresAt > DateTime.UtcNow))
                .Select(upa => upa.Policy)
                .ToListAsync();

            // Role policies
            var rolePolicies = await _context.UserPolicyRoles
                .Include(ur => ur.Role)
                    .ThenInclude(r => r.RolePolicies)
                        .ThenInclude(rp => rp.Policy)
                .Where(ur => ur.UserId == userId)
                .SelectMany(ur => ur.Role.RolePolicies.Select(rp => rp.Policy))
                .ToListAsync();

            var allPolicies = directPolicies.Union(rolePolicies).Distinct().Select(MapToPolicy).ToList();
            await _cacheService.SetAsync(cacheKey, allPolicies, TimeSpan.FromMinutes(_options.PolicyCacheDurationMinutes));

            return allPolicies;
        }

        public async Task<bool> AssignRoleToGroupAsync(Guid groupId, Guid roleId)
        {
            var roleExists = await _context.PolicyRoles.AnyAsync(r => r.Id == roleId);
            if (!roleExists)
            {
                return false;
            }

            var existing = await _context.GroupPolicyRoles
                .AnyAsync(gr => gr.GroupId == groupId && gr.RoleId == roleId);

            if (existing)
            {
                return true;
            }

            _context.GroupPolicyRoles.Add(new GroupPolicyRoleEntity
            {
                GroupId = groupId,
                RoleId = roleId,
                AssignedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync(
                "group.role_assigned",
                "Group",
                groupId.ToString(),
                null,
                new { roleId },
                null);

            return true;
        }

        public async Task<bool> RemoveRoleFromGroupAsync(Guid groupId, Guid roleId)
        {
            var groupRole = await _context.GroupPolicyRoles
                .FirstOrDefaultAsync(gr => gr.GroupId == groupId && gr.RoleId == roleId);

            if (groupRole == null)
            {
                return false;
            }

            _context.GroupPolicyRoles.Remove(groupRole);
            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync(
                "group.role_removed",
                "Group",
                groupId.ToString(),
                new { roleId },
                null,
                null);

            return true;
        }

        public async Task<bool> AssignPolicyToGroupAsync(Guid groupId, Guid policyId)
        {
            var policyExists = await _context.Policies.AnyAsync(p => p.Id == policyId);
            if (!policyExists)
            {
                return false;
            }

            var existing = await _context.GroupPolicyAssignments
                .AnyAsync(gpa => gpa.GroupId == groupId && gpa.PolicyId == policyId);

            if (existing)
            {
                return true;
            }

            _context.GroupPolicyAssignments.Add(new GroupPolicyAssignmentEntity
            {
                GroupId = groupId,
                PolicyId = policyId,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = "system" // TODO: Get from current user context
            });

            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync(
                "group.policy_assigned",
                "Group",
                groupId.ToString(),
                null,
                new { policyId },
                null);

            return true;
        }

        public async Task<bool> RemovePolicyFromGroupAsync(Guid groupId, Guid policyId)
        {
            var assignment = await _context.GroupPolicyAssignments
                .FirstOrDefaultAsync(gpa => gpa.GroupId == groupId && gpa.PolicyId == policyId);

            if (assignment == null)
            {
                return false;
            }

            _context.GroupPolicyAssignments.Remove(assignment);
            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync(
                "group.policy_removed",
                "Group",
                groupId.ToString(),
                new { policyId },
                null,
                null);

            return true;
        }

        #endregion

        #region Policy Evaluation

        public async Task<PolicyEvaluationResult> EvaluatePolicyAsync(Guid policyId, PolicyEvaluationContext context)
        {
            var startTime = DateTime.UtcNow;

            var policy = await GetPolicyAsync(policyId);
            if (policy == null)
            {
                return new PolicyEvaluationResult
                {
                    IsAllowed = false,
                    Reason = "Policy not found"
                };
            }

            var trace = new PolicyTrace
            {
                PolicyId = policy.Id,
                PolicyName = policy.Name,
                Effect = policy.Effect,
                Priority = policy.Priority
            };

            // Check if policy is active
            if (!policy.IsActive)
            {
                trace.Matched = false;
                trace.FailureReason = "Policy is not active";
                return new PolicyEvaluationResult
                {
                    IsAllowed = false,
                    Reason = "Policy is not active",
                    EvaluationTrace = new List<PolicyTrace> { trace }
                };
            }

            // Evaluate resource match
            if (!MatchesResource(policy.Resources, context.Resource))
            {
                trace.Matched = false;
                trace.FailureReason = "Resource does not match";
                return new PolicyEvaluationResult
                {
                    IsAllowed = false,
                    Reason = "Resource does not match policy",
                    EvaluationTrace = new List<PolicyTrace> { trace }
                };
            }

            // Evaluate action match
            if (!MatchesAction(policy.Actions, context.Action))
            {
                trace.Matched = false;
                trace.FailureReason = "Action does not match";
                return new PolicyEvaluationResult
                {
                    IsAllowed = false,
                    Reason = "Action does not match policy",
                    EvaluationTrace = new List<PolicyTrace> { trace }
                };
            }

            // Evaluate conditions
            if (policy.Conditions != null && policy.Conditions.Count > 0)
            {
                var conditionResult = await EvaluateConditionsAsync(policy.Conditions, context);
                if (!conditionResult.IsMatched)
                {
                    trace.Matched = false;
                    trace.FailureReason = $"Conditions not met: {string.Join(", ", conditionResult.FailedConditions)}";
                    return new PolicyEvaluationResult
                    {
                        IsAllowed = false,
                        Reason = trace.FailureReason,
                        EvaluationTrace = new List<PolicyTrace> { trace }
                    };
                }
            }

            // Policy matches
            trace.Matched = true;
            var isAllowed = policy.Effect == PolicyEffect.Allow;

            _metricsCollector.RecordCounter($"policy.evaluation.{(isAllowed ? "allow" : "deny")}");
            _metricsCollector.RecordTimer("policy.evaluation", (DateTime.UtcNow - startTime).TotalMilliseconds, 
                new Dictionary<string, string> { ["policy_id"] = policyId.ToString() });

            return new PolicyEvaluationResult
            {
                IsAllowed = isAllowed,
                MatchedPolicyId = policy.Id,
                MatchedPolicyName = policy.Name,
                AppliedEffect = policy.Effect,
                EvaluationTrace = new List<PolicyTrace> { trace }
            };
        }

        public async Task<PolicyEvaluationResult> EvaluateUserAccessAsync(Guid userId, PolicyEvaluationContext context)
        {
            context.UserId = userId;

            // Check cache
            var cacheKey = $"policy:eval:{userId}:{context.Resource}:{context.Action}";
            var cached = await _cacheService.GetAsync<PolicyEvaluationResult>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            var startTime = DateTime.UtcNow;
            var evaluationTrace = new List<PolicyTrace>();

            // Get all effective policies for the user
            var policies = await GetEffectivePoliciesForUserAsync(userId);

            // Sort by priority (higher priority first) and effect (Deny first)
            policies = policies.OrderByDescending(p => p.Priority)
                             .ThenBy(p => p.Effect == PolicyEffect.Deny ? 0 : 1)
                             .ToList();

            PolicyEvaluationResult? finalResult = null;

            foreach (var policy in policies)
            {
                var policyResult = await EvaluatePolicyAsync(policy.Id, context);
                evaluationTrace.AddRange(policyResult.EvaluationTrace);

                if (policyResult.MatchedPolicyId.HasValue)
                {
                    // Policy matched
                    if (policy.Effect == PolicyEffect.Deny || _options.ConflictResolution == PolicyConflictResolution.DenyOverridesAllow)
                    {
                        // Deny always wins
                        if (!policyResult.IsAllowed)
                        {
                            finalResult = policyResult;
                            break;
                        }
                    }
                    else if (finalResult == null && policyResult.IsAllowed)
                    {
                        // First allow
                        finalResult = policyResult;
                    }
                }
            }

            // If no policy matched and default is deny all
            if (finalResult == null)
            {
                finalResult = new PolicyEvaluationResult
                {
                    IsAllowed = !_options.DefaultDenyAll,
                    Reason = _options.DefaultDenyAll ? "No matching policy found (default deny)" : "No matching policy found (default allow)"
                };
            }

            finalResult.EvaluationTrace = evaluationTrace;
            finalResult.EvaluationTime = DateTime.UtcNow - startTime;

            // Cache the result
            await _cacheService.SetAsync(cacheKey, finalResult, TimeSpan.FromMinutes(_options.PolicyCacheDurationMinutes));

            // Audit the access decision
            await _auditService.LogActionAsync(
                "policy.evaluation",
                "User",
                userId.ToString(),
                null,
                new 
                { 
                    allowed = finalResult.IsAllowed,
                    resource = context.Resource,
                    action = context.Action,
                    matchedPolicyId = finalResult.MatchedPolicyId,
                    evaluationTimeMs = finalResult.EvaluationTime.TotalMilliseconds
                },
                null);

            return finalResult;
        }

        public async Task<PolicyEvaluationResult> EvaluateGroupAccessAsync(Guid groupId, PolicyEvaluationContext context)
        {
            // Get all policies for the group
            var groupPolicies = await GetGroupPoliciesAsync(groupId);
            var groupRoles = await GetGroupRolesAsync(groupId);
            var rolePolicies = groupRoles.SelectMany(r => r.PolicyIds)
                .Select(pid => GetPolicyAsync(pid).Result)
                .Where(p => p != null)
                .Cast<Policy>()
                .ToList();

            var allPolicies = groupPolicies.Union(rolePolicies).Distinct().ToList();

            // Evaluate policies similar to user evaluation
            var evaluationTrace = new List<PolicyTrace>();
            PolicyEvaluationResult? finalResult = null;

            foreach (var policy in allPolicies.OrderByDescending(p => p.Priority))
            {
                var policyResult = await EvaluatePolicyAsync(policy.Id, context);
                evaluationTrace.AddRange(policyResult.EvaluationTrace);

                if (policyResult.MatchedPolicyId.HasValue)
                {
                    if (policy.Effect == PolicyEffect.Deny && !policyResult.IsAllowed)
                    {
                        finalResult = policyResult;
                        break;
                    }
                    else if (finalResult == null && policyResult.IsAllowed)
                    {
                        finalResult = policyResult;
                    }
                }
            }

            if (finalResult == null)
            {
                finalResult = new PolicyEvaluationResult
                {
                    IsAllowed = !_options.DefaultDenyAll,
                    Reason = _options.DefaultDenyAll ? "No matching policy found (default deny)" : "No matching policy found (default allow)"
                };
            }

            finalResult.EvaluationTrace = evaluationTrace;
            return finalResult;
        }

        public async Task<List<string>> GetAllowedActionsAsync(Guid userId, string resource)
        {
            var policies = await GetEffectivePoliciesForUserAsync(userId);
            var allowedActions = new HashSet<string>();
            var deniedActions = new HashSet<string>();

            foreach (var policy in policies.OrderByDescending(p => p.Priority))
            {
                if (!MatchesResource(policy.Resources, resource))
                {
                    continue;
                }

                if (policy.Effect == PolicyEffect.Allow)
                {
                    foreach (var action in policy.Actions)
                    {
                        if (!deniedActions.Contains(action))
                        {
                            allowedActions.Add(action);
                        }
                    }
                }
                else
                {
                    foreach (var action in policy.Actions)
                    {
                        deniedActions.Add(action);
                        allowedActions.Remove(action);
                    }
                }
            }

            return allowedActions.ToList();
        }

        public async Task<List<string>> GetAccessibleResourcesAsync(Guid userId, string action)
        {
            var policies = await GetEffectivePoliciesForUserAsync(userId);
            var accessibleResources = new HashSet<string>();

            foreach (var policy in policies.Where(p => p.Effect == PolicyEffect.Allow))
            {
                if (!MatchesAction(policy.Actions, action))
                {
                    continue;
                }

                foreach (var resource in policy.Resources)
                {
                    // Expand wildcards to concrete resources if needed
                    if (resource.Contains("*"))
                    {
                        // In a real implementation, this would expand to actual resources
                        accessibleResources.Add(resource);
                    }
                    else
                    {
                        accessibleResources.Add(resource);
                    }
                }
            }

            return accessibleResources.ToList();
        }

        #endregion

        #region Resource and Action Management

        public async Task<ResourceDefinition> RegisterResourceAsync(ResourceDefinition resource)
        {
            var entity = new ResourceDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Name = resource.Name,
                Type = resource.Type,
                ParentResource = resource.ParentResource,
                SupportedActions = JsonSerializer.Serialize(resource.SupportedActions),
                Metadata = resource.Metadata != null ? JsonSerializer.Serialize(resource.Metadata) : null,
                IsWildcardAllowed = resource.IsWildcardAllowed
            };

            _context.ResourceDefinitions.Add(entity);
            await _context.SaveChangesAsync();

            return resource;
        }

        public async Task<ActionDefinition> RegisterActionAsync(ActionDefinition action)
        {
            var entity = new ActionDefinitionEntity
            {
                Id = Guid.NewGuid(),
                Name = action.Name,
                Description = action.Description,
                ResourceType = action.ResourceType,
                RequiresMFA = action.RequiresMFA,
                IsHighRisk = action.IsHighRisk,
                RequiredAttributes = action.RequiredAttributes != null ? JsonSerializer.Serialize(action.RequiredAttributes) : null
            };

            _context.ActionDefinitions.Add(entity);
            await _context.SaveChangesAsync();

            return action;
        }

        public async Task<List<ResourceDefinition>> GetResourcesAsync(string? type = null)
        {
            var query = _context.ResourceDefinitions.AsQueryable();

            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(r => r.Type == type);
            }

            var entities = await query.ToListAsync();
            return entities.Select(e => new ResourceDefinition
            {
                Name = e.Name,
                Type = e.Type,
                ParentResource = e.ParentResource,
                SupportedActions = JsonSerializer.Deserialize<List<string>>(e.SupportedActions) ?? new List<string>(),
                Metadata = e.Metadata != null ? JsonSerializer.Deserialize<Dictionary<string, object>>(e.Metadata) : null,
                IsWildcardAllowed = e.IsWildcardAllowed
            }).ToList();
        }

        public async Task<List<ActionDefinition>> GetActionsAsync(string? resourceType = null)
        {
            var query = _context.ActionDefinitions.AsQueryable();

            if (!string.IsNullOrEmpty(resourceType))
            {
                query = query.Where(a => a.ResourceType == resourceType);
            }

            var entities = await query.ToListAsync();
            return entities.Select(e => new ActionDefinition
            {
                Name = e.Name,
                Description = e.Description,
                ResourceType = e.ResourceType,
                RequiresMFA = e.RequiresMFA,
                IsHighRisk = e.IsHighRisk,
                RequiredAttributes = e.RequiredAttributes != null ? JsonSerializer.Deserialize<string[]>(e.RequiredAttributes) : null
            }).ToList();
        }

        #endregion

        #region Policy Templates

        public async Task<PolicyTemplate> CreatePolicyTemplateAsync(PolicyTemplate template)
        {
            var entity = new PolicyTemplateEntity
            {
                Id = Guid.NewGuid(),
                Name = template.Name,
                Description = template.Description,
                Category = template.Category,
                DefaultEffect = template.DefaultEffect.ToString(),
                Parameters = JsonSerializer.Serialize(template.Parameters),
                PolicyJsonTemplate = template.PolicyJsonTemplate,
                IsBuiltIn = template.IsBuiltIn
            };

            _context.PolicyTemplates.Add(entity);
            await _context.SaveChangesAsync();

            template.Id = entity.Id;
            return template;
        }

        public async Task<Policy> CreatePolicyFromTemplateAsync(Guid templateId, Dictionary<string, object> parameters)
        {
            var template = await _context.PolicyTemplates.FindAsync(templateId);
            if (template == null)
            {
                throw new InvalidOperationException($"Template {templateId} not found");
            }

            // Parse template and replace parameters
            var policyJson = template.PolicyJsonTemplate;
            foreach (var param in parameters)
            {
                policyJson = policyJson.Replace($"{{{param.Key}}}", param.Value.ToString());
            }

            var definition = JsonSerializer.Deserialize<PolicyDefinition>(policyJson);
            if (definition == null)
            {
                throw new InvalidOperationException("Failed to parse policy from template");
            }

            return await CreatePolicyAsync(definition);
        }

        public async Task<List<PolicyTemplate>> GetPolicyTemplatesAsync(string? category = null)
        {
            var query = _context.PolicyTemplates.AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(t => t.Category == category);
            }

            var entities = await query.ToListAsync();
            return entities.Select(e => new PolicyTemplate
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                Category = e.Category,
                DefaultEffect = Enum.Parse<PolicyEffect>(e.DefaultEffect),
                Parameters = JsonSerializer.Deserialize<List<PolicyParameter>>(e.Parameters) ?? new List<PolicyParameter>(),
                PolicyJsonTemplate = e.PolicyJsonTemplate,
                IsBuiltIn = e.IsBuiltIn
            }).ToList();
        }

        #endregion

        #region Delegation

        public async Task<PolicyDelegation> DelegatePolicyAsync(Guid fromUserId, Guid toUserId, Guid policyId, DateTime startDate, DateTime endDate)
        {
            // Verify the delegator has the policy
            var userPolicies = await GetUserPoliciesAsync(fromUserId);
            if (!userPolicies.Any(p => p.Id == policyId))
            {
                throw new InvalidOperationException("User does not have the policy to delegate");
            }

            var delegation = new PolicyDelegationEntity
            {
                Id = Guid.NewGuid(),
                FromUserId = fromUserId,
                ToUserId = toUserId,
                PolicyId = policyId,
                StartDate = startDate,
                EndDate = endDate,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.PolicyDelegations.Add(delegation);
            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync(
                "policy.delegated",
                "PolicyDelegation",
                delegation.Id.ToString(),
                null,
                delegation,
                new { fromUserId, toUserId, policyId });

            return new PolicyDelegation
            {
                Id = delegation.Id,
                FromUserId = delegation.FromUserId,
                ToUserId = delegation.ToUserId,
                PolicyId = delegation.PolicyId,
                StartDate = delegation.StartDate,
                EndDate = delegation.EndDate,
                IsActive = delegation.IsActive,
                Reason = delegation.Reason,
                Constraints = delegation.Constraints != null ? JsonSerializer.Deserialize<Dictionary<string, object>>(delegation.Constraints) : null
            };
        }

        public async Task<bool> RevokeDelegationAsync(Guid delegationId)
        {
            var delegation = await _context.PolicyDelegations.FindAsync(delegationId);
            if (delegation == null)
            {
                return false;
            }

            delegation.IsActive = false;
            delegation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditService.LogActionAsync(
                "policy.delegation_revoked",
                "PolicyDelegation",
                delegationId.ToString(),
                delegation,
                null,
                null);

            return true;
        }

        public async Task<List<PolicyDelegation>> GetUserDelegationsAsync(Guid userId)
        {
            var delegations = await _context.PolicyDelegations
                .Where(d => d.FromUserId == userId && d.IsActive)
                .ToListAsync();

            return delegations.Select(d => new PolicyDelegation
            {
                Id = d.Id,
                FromUserId = d.FromUserId,
                ToUserId = d.ToUserId,
                PolicyId = d.PolicyId,
                StartDate = d.StartDate,
                EndDate = d.EndDate,
                IsActive = d.IsActive,
                Reason = d.Reason,
                Constraints = d.Constraints != null ? JsonSerializer.Deserialize<Dictionary<string, object>>(d.Constraints) : null
            }).ToList();
        }

        public async Task<List<PolicyDelegation>> GetDelegatedPoliciesAsync(Guid userId)
        {
            var now = DateTime.UtcNow;
            var delegations = await _context.PolicyDelegations
                .Where(d => d.ToUserId == userId && d.IsActive && d.StartDate <= now && d.EndDate >= now)
                .ToListAsync();

            return delegations.Select(d => new PolicyDelegation
            {
                Id = d.Id,
                FromUserId = d.FromUserId,
                ToUserId = d.ToUserId,
                PolicyId = d.PolicyId,
                StartDate = d.StartDate,
                EndDate = d.EndDate,
                IsActive = d.IsActive,
                Reason = d.Reason,
                Constraints = d.Constraints != null ? JsonSerializer.Deserialize<Dictionary<string, object>>(d.Constraints) : null
            }).ToList();
        }

        #endregion

        #region Analytics and Reporting

        public async Task<PolicyUsageStats> GetPolicyUsageStatsAsync(Guid policyId, DateTime? startDate = null, DateTime? endDate = null)
        {
            // In a real implementation, this would query from audit logs or metrics storage
            var stats = new PolicyUsageStats
            {
                PolicyId = policyId,
                EvaluationCount = Random.Shared.Next(1000, 10000),
                AllowCount = Random.Shared.Next(500, 5000),
                DenyCount = Random.Shared.Next(100, 1000),
                AverageEvaluationTimeMs = Random.Shared.NextDouble() * 10,
                LastEvaluated = DateTime.UtcNow.AddMinutes(-Random.Shared.Next(1, 60)),
                DenyReasons = new Dictionary<string, long>
                {
                    ["Resource mismatch"] = Random.Shared.Next(10, 100),
                    ["Action mismatch"] = Random.Shared.Next(10, 100),
                    ["Condition failed"] = Random.Shared.Next(10, 100)
                }
            };

            return await Task.FromResult(stats);
        }

        public async Task<PolicyEffectivenessReport> GenerateEffectivenessReportAsync()
        {
            var policies = await _context.Policies.ToListAsync();
            var report = new PolicyEffectivenessReport
            {
                GeneratedAt = DateTime.UtcNow,
                TotalPolicies = policies.Count,
                ActivePolicies = policies.Count(p => p.IsActive),
                UnusedPolicies = policies.Count(p => !p.IsActive), // Simplified
                PolicyStats = new List<PolicyUsageStats>(),
                DetectedConflicts = await DetectPolicyConflictsAsync(),
                Recommendations = new Dictionary<string, object>
                {
                    ["consolidate_similar_policies"] = true,
                    ["remove_unused_policies"] = policies.Count(p => !p.IsActive),
                    ["optimize_condition_complexity"] = false
                }
            };

            // Add stats for each policy
            foreach (var policy in policies.Take(10)) // Limit for performance
            {
                report.PolicyStats.Add(await GetPolicyUsageStatsAsync(policy.Id));
            }

            return report;
        }

        public async Task<List<PolicyConflict>> DetectPolicyConflictsAsync()
        {
            var conflicts = new List<PolicyConflict>();
            var policies = await _context.Policies.Where(p => p.IsActive).ToListAsync();

            // Simple conflict detection - check for overlapping resources with different effects
            for (int i = 0; i < policies.Count; i++)
            {
                for (int j = i + 1; j < policies.Count; j++)
                {
                    var policy1 = policies[i];
                    var policy2 = policies[j];

                    var resources1 = JsonSerializer.Deserialize<string[]>(policy1.Resources) ?? Array.Empty<string>();
                    var resources2 = JsonSerializer.Deserialize<string[]>(policy2.Resources) ?? Array.Empty<string>();
                    var actions1 = JsonSerializer.Deserialize<string[]>(policy1.Actions) ?? Array.Empty<string>();
                    var actions2 = JsonSerializer.Deserialize<string[]>(policy2.Actions) ?? Array.Empty<string>();

                    // Check for resource overlap
                    var hasResourceOverlap = resources1.Any(r1 => resources2.Any(r2 => ResourcesOverlap(r1, r2)));
                    var hasActionOverlap = actions1.Any(a1 => actions2.Any(a2 => ActionsOverlap(a1, a2)));

                    if (hasResourceOverlap && hasActionOverlap && policy1.Effect != policy2.Effect)
                    {
                        conflicts.Add(new PolicyConflict
                        {
                            Policy1Id = policy1.Id,
                            Policy2Id = policy2.Id,
                            ConflictType = "EffectConflict",
                            Description = $"Policies have overlapping resources and actions but different effects"
                        });
                    }
                }
            }

            return conflicts;
        }

        public async Task<bool> ResolvePolicyConflictAsync(Guid conflictId, PolicyConflictResolution resolution)
        {
            // In a real implementation, this would apply the resolution strategy
            await _auditService.LogActionAsync(
                "policy.conflict_resolved",
                "PolicyConflict",
                conflictId.ToString(),
                null,
                new { resolution },
                null);

            return true;
        }

        #endregion

        #region Bulk Operations

        public async Task<Dictionary<Guid, PolicyEvaluationResult>> BulkEvaluatePoliciesAsync(List<Guid> userIds, PolicyEvaluationContext context)
        {
            var results = new Dictionary<Guid, PolicyEvaluationResult>();

            // In a real implementation, this could be parallelized
            foreach (var userId in userIds)
            {
                results[userId] = await EvaluateUserAccessAsync(userId, context);
            }

            return results;
        }

        public async Task<bool> BulkAssignRoleAsync(List<Guid> userIds, Guid roleId)
        {
            var roleExists = await _context.PolicyRoles.AnyAsync(r => r.Id == roleId);
            if (!roleExists)
            {
                return false;
            }

            foreach (var userId in userIds)
            {
                await AssignRoleToUserAsync(userId, roleId);
            }

            return true;
        }

        public async Task<bool> BulkRemoveRoleAsync(List<Guid> userIds, Guid roleId)
        {
            foreach (var userId in userIds)
            {
                await RemoveRoleFromUserAsync(userId, roleId);
            }

            return true;
        }

        #endregion

        #region Policy Inheritance

        public async Task<bool> SetPolicyParentAsync(Guid policyId, Guid parentId)
        {
            if (policyId == parentId)
            {
                throw new InvalidOperationException("Policy cannot be its own parent");
            }

            var policy = await _context.Policies.FindAsync(policyId);
            if (policy == null)
            {
                return false;
            }

            var parent = await _context.Policies.FindAsync(parentId);
            if (parent == null)
            {
                return false;
            }

            // Check for circular dependency
            if (await HasCircularDependency(parentId, policyId))
            {
                throw new InvalidOperationException("Setting parent would create circular dependency");
            }

            policy.ParentId = parentId;
            policy.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Clear cache
            await _cacheService.RemoveAsync($"policy:{policyId}");

            return true;
        }

        public async Task<List<Policy>> GetPolicyHierarchyAsync(Guid policyId)
        {
            var hierarchy = new List<Policy>();
            var currentId = policyId;
            var depth = 0;

            while (currentId != Guid.Empty && depth < _options.MaxPolicyDepth)
            {
                var policy = await GetPolicyAsync(currentId);
                if (policy == null)
                {
                    break;
                }

                hierarchy.Add(policy);
                currentId = policy.ParentId ?? Guid.Empty;
                depth++;
            }

            return hierarchy;
        }

        public async Task<List<Policy>> GetChildPoliciesAsync(Guid parentId)
        {
            var children = await _context.Policies
                .Where(p => p.ParentId == parentId)
                .ToListAsync();

            return children.Select(MapToPolicy).ToList();
        }

        #endregion

        #region Import/Export

        public async Task<string> ExportPoliciesAsync(List<Guid>? policyIds = null)
        {
            var query = _context.Policies.AsQueryable();

            if (policyIds != null && policyIds.Any())
            {
                query = query.Where(p => policyIds.Contains(p.Id));
            }

            var policies = await query.ToListAsync();
            var export = new
            {
                version = "1.0",
                exportDate = DateTime.UtcNow,
                policies = policies.Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.Effect,
                    Resources = JsonSerializer.Deserialize<string[]>(p.Resources),
                    Actions = JsonSerializer.Deserialize<string[]>(p.Actions),
                    Conditions = p.Conditions != null ? JsonSerializer.Deserialize<Dictionary<string, object>>(p.Conditions) : null,
                    p.Priority,
                    p.IsActive,
                    p.Version,
                    p.ParentId,
                    Tags = p.Tags != null ? JsonSerializer.Deserialize<Dictionary<string, string>>(p.Tags) : null
                })
            };

            return JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true });
        }

        public async Task<List<Policy>> ImportPoliciesAsync(string policiesJson)
        {
            var import = JsonSerializer.Deserialize<PolicyImport>(policiesJson);
            if (import == null)
            {
                throw new InvalidOperationException("Invalid import format");
            }

            var importedPolicies = new List<Policy>();

            foreach (var policyData in import.Policies)
            {
                var definition = new PolicyDefinition
                {
                    Name = policyData.Name,
                    Description = policyData.Description,
                    Effect = Enum.Parse<PolicyEffect>(policyData.Effect),
                    Resources = policyData.Resources,
                    Actions = policyData.Actions,
                    Conditions = policyData.Conditions,
                    Priority = policyData.Priority,
                    Tags = policyData.Tags
                };

                var policy = await CreatePolicyAsync(definition);
                importedPolicies.Add(policy);
            }

            return importedPolicies;
        }

        public async Task<string> ExportRolesAsync(List<Guid>? roleIds = null)
        {
            var query = _context.PolicyRoles.Include(r => r.RolePolicies).AsQueryable();

            if (roleIds != null && roleIds.Any())
            {
                query = query.Where(r => roleIds.Contains(r.Id));
            }

            var roles = await query.ToListAsync();
            var export = new
            {
                version = "1.0",
                exportDate = DateTime.UtcNow,
                roles = roles.Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.Description,
                    PolicyIds = r.RolePolicies.Select(rp => rp.PolicyId),
                    r.IsActive,
                    r.IsSystem,
                    Tags = r.Tags != null ? JsonSerializer.Deserialize<Dictionary<string, string>>(r.Tags) : null
                })
            };

            return JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true });
        }

        public async Task<List<RemoteC.Shared.Models.Role>> ImportRolesAsync(string rolesJson)
        {
            var import = JsonSerializer.Deserialize<RoleImport>(rolesJson);
            if (import == null)
            {
                throw new InvalidOperationException("Invalid import format");
            }

            var importedRoles = new List<RemoteC.Shared.Models.Role>();

            foreach (var roleData in import.Roles)
            {
                var definition = new RoleDefinition
                {
                    Name = roleData.Name,
                    Description = roleData.Description,
                    PolicyIds = roleData.PolicyIds,
                    Tags = roleData.Tags,
                    IsSystem = false
                };

                var role = await CreateRoleAsync(definition);
                importedRoles.Add(role);
            }

            return importedRoles;
        }

        #endregion

        #region Helper Methods

        private Policy MapToPolicy(PolicyEntity entity)
        {
            return new Policy
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                Effect = Enum.Parse<PolicyEffect>(entity.Effect),
                Resources = JsonSerializer.Deserialize<string[]>(entity.Resources) ?? Array.Empty<string>(),
                Actions = JsonSerializer.Deserialize<string[]>(entity.Actions) ?? Array.Empty<string>(),
                Conditions = entity.Conditions != null ? JsonSerializer.Deserialize<Dictionary<string, object>>(entity.Conditions) : null,
                Principals = entity.Principals != null ? JsonSerializer.Deserialize<string[]>(entity.Principals) : null,
                NotPrincipals = entity.NotPrincipals != null ? JsonSerializer.Deserialize<string[]>(entity.NotPrincipals) : null,
                Priority = entity.Priority,
                IsActive = entity.IsActive,
                Version = entity.Version,
                ParentId = entity.ParentId,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                CreatedBy = entity.CreatedBy,
                Tags = entity.Tags != null ? JsonSerializer.Deserialize<Dictionary<string, string>>(entity.Tags) : null
            };
        }

        private RemoteC.Shared.Models.Role MapToRole(PolicyRoleEntity entity)
        {
            return new RemoteC.Shared.Models.Role
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                PolicyIds = entity.RolePolicies?.Select(rp => rp.PolicyId).ToList() ?? new List<Guid>(),
                IsActive = entity.IsActive,
                IsSystem = entity.IsSystem,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                Tags = entity.Tags != null ? JsonSerializer.Deserialize<Dictionary<string, string>>(entity.Tags) : null
            };
        }

        private bool IsValidResourcePattern(string pattern)
        {
            // Basic validation - ensure pattern doesn't have invalid characters
            return !string.IsNullOrWhiteSpace(pattern) && 
                   !pattern.Contains("..") && 
                   !pattern.StartsWith("/") && 
                   !pattern.EndsWith("/");
        }

        private int CalculateConditionComplexity(Dictionary<string, object> conditions)
        {
            // Simple complexity calculation based on number of conditions and nesting
            return CountComplexity(conditions);
        }

        private int CountComplexity(object obj, int depth = 0)
        {
            if (depth > 10) return 100; // Max depth exceeded

            switch (obj)
            {
                case Dictionary<string, object> dict:
                    return 1 + dict.Sum(kvp => CountComplexity(kvp.Value, depth + 1));
                case List<object> list:
                    return 1 + list.Sum(item => CountComplexity(item, depth + 1));
                case Array array:
                    return 1 + array.Cast<object>().Sum(item => CountComplexity(item, depth + 1));
                default:
                    return 1;
            }
        }

        private bool MatchesResource(string[] policyResources, string requestResource)
        {
            foreach (var policyResource in policyResources)
            {
                if (MatchesPattern(policyResource, requestResource))
                {
                    return true;
                }
            }
            return false;
        }

        private bool MatchesAction(string[] policyActions, string requestAction)
        {
            foreach (var policyAction in policyActions)
            {
                if (MatchesPattern(policyAction, requestAction))
                {
                    return true;
                }
            }
            return false;
        }

        private bool MatchesPattern(string pattern, string value)
        {
            if (pattern == "*") return true;
            if (pattern == value) return true;

            // Convert wildcard pattern to regex
            var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
            return Regex.IsMatch(value, regexPattern, RegexOptions.IgnoreCase);
        }

        private async Task<ConditionEvaluationResult> EvaluateConditionsAsync(Dictionary<string, object> conditions, PolicyEvaluationContext context)
        {
            var result = new ConditionEvaluationResult { IsMatched = true };

            foreach (var condition in conditions)
            {
                var conditionMet = EvaluateCondition(condition.Key, condition.Value, context.Attributes);
                if (!conditionMet)
                {
                    result.IsMatched = false;
                    result.FailedConditions.Add(condition.Key);
                }
            }

            return await Task.FromResult(result);
        }

        private bool EvaluateCondition(string conditionKey, object conditionValue, Dictionary<string, object> attributes)
        {
            if (!attributes.TryGetValue(conditionKey, out var attributeValue))
            {
                return false;
            }

            // Handle different condition types
            switch (conditionValue)
            {
                case string strValue:
                    return attributeValue?.ToString() == strValue;
                
                case bool boolValue:
                    return attributeValue is bool attrBool && attrBool == boolValue;
                
                case JsonElement jsonElement:
                    return EvaluateJsonCondition(jsonElement, attributeValue);
                
                case Dictionary<string, object> complexCondition:
                    return EvaluateComplexCondition(complexCondition, attributeValue);
                
                case IEnumerable<object> arrayValue:
                    return EvaluateArrayCondition(arrayValue, attributeValue);
                
                default:
                    return attributeValue?.Equals(conditionValue) ?? false;
            }
        }

        private bool EvaluateJsonCondition(JsonElement element, object attributeValue)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return attributeValue?.ToString() == element.GetString();
                case JsonValueKind.Number:
                    if (attributeValue is IConvertible)
                    {
                        var numValue = Convert.ToDouble(attributeValue);
                        return Math.Abs(numValue - element.GetDouble()) < 0.0001;
                    }
                    return false;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return attributeValue is bool boolValue && boolValue == element.GetBoolean();
                case JsonValueKind.Array:
                    var array = element.EnumerateArray().Select(e => e.ToString()).ToList();
                    return array.Contains(attributeValue?.ToString());
                default:
                    return false;
            }
        }

        private bool EvaluateComplexCondition(Dictionary<string, object> condition, object attributeValue)
        {
            // Handle complex conditions like { min: 3, max: 5 } or { contains: "value" }
            if (condition.ContainsKey("min") && condition.ContainsKey("max"))
            {
                if (attributeValue is IConvertible)
                {
                    var value = Convert.ToDouble(attributeValue);
                    var min = Convert.ToDouble(condition["min"]);
                    var max = Convert.ToDouble(condition["max"]);
                    return value >= min && value <= max;
                }
            }

            if (condition.ContainsKey("contains"))
            {
                var searchValue = condition["contains"]?.ToString();
                if (attributeValue is IEnumerable<object> enumerable)
                {
                    return enumerable.Any(item => item?.ToString() == searchValue);
                }
                return attributeValue?.ToString()?.Contains(searchValue ?? "") ?? false;
            }

            if (condition.ContainsKey("in"))
            {
                if (condition["in"] is IEnumerable<object> allowedValues)
                {
                    return allowedValues.Any(v => v?.ToString() == attributeValue?.ToString());
                }
            }

            return false;
        }

        private bool EvaluateArrayCondition(IEnumerable<object> arrayValue, object attributeValue)
        {
            // Check if attribute value is in the array
            var stringValues = arrayValue.Select(v => v?.ToString()).Where(v => v != null);
            return stringValues.Contains(attributeValue?.ToString());
        }

        private async Task<List<Policy>> GetEffectivePoliciesForUserAsync(Guid userId)
        {
            // Get direct user policies
            var userPolicies = await GetUserPoliciesAsync(userId);

            // Get policies from user's groups
            var userGroups = await _context.UserGroups
                .Where(ug => ug.UserId == userId)
                .Select(ug => ug.GroupId)
                .ToListAsync();

            var groupPolicies = new List<Policy>();
            foreach (var groupId in userGroups)
            {
                var policies = await GetGroupPoliciesAsync(groupId);
                groupPolicies.AddRange(policies);
            }

            // Get delegated policies
            var delegatedPolicies = await GetDelegatedPoliciesAsync(userId);
            var delegatedPolicyEntities = new List<Policy>();
            foreach (var delegation in delegatedPolicies)
            {
                var policy = await GetPolicyAsync(delegation.PolicyId);
                if (policy != null)
                {
                    delegatedPolicyEntities.Add(policy);
                }
            }

            // Combine all policies
            var allPolicies = userPolicies
                .Union(groupPolicies)
                .Union(delegatedPolicyEntities)
                .Distinct()
                .ToList();

            // Include inherited policies if enabled
            if (_options.EnablePolicyInheritance)
            {
                var inheritedPolicies = new List<Policy>();
                foreach (var policy in allPolicies.ToList())
                {
                    if (policy.ParentId.HasValue)
                    {
                        var hierarchy = await GetPolicyHierarchyAsync(policy.Id);
                        inheritedPolicies.AddRange(hierarchy.Skip(1)); // Skip the first one as it's already included
                    }
                }
                allPolicies.AddRange(inheritedPolicies);
            }

            return allPolicies.Distinct().ToList();
        }

        private async Task<List<Policy>> GetGroupPoliciesAsync(Guid groupId)
        {
            var directPolicies = await _context.GroupPolicyAssignments
                .Include(gpa => gpa.Policy)
                .Where(gpa => gpa.GroupId == groupId)
                .Select(gpa => gpa.Policy)
                .ToListAsync();

            return directPolicies.Select(MapToPolicy).ToList();
        }

        private async Task<List<RemoteC.Shared.Models.Role>> GetGroupRolesAsync(Guid groupId)
        {
            var roles = await _context.GroupPolicyRoles
                .Include(gr => gr.Role)
                    .ThenInclude(r => r.RolePolicies)
                .Where(gr => gr.GroupId == groupId)
                .Select(gr => gr.Role)
                .ToListAsync();

            return roles.Select(MapToRole).ToList();
        }

        private bool ResourcesOverlap(string resource1, string resource2)
        {
            if (resource1 == resource2) return true;
            if (resource1 == "*" || resource2 == "*") return true;

            // Check if one pattern matches the other
            return MatchesPattern(resource1, resource2) || MatchesPattern(resource2, resource1);
        }

        private bool ActionsOverlap(string action1, string action2)
        {
            if (action1 == action2) return true;
            if (action1 == "*" || action2 == "*") return true;

            // Check if one pattern matches the other
            return MatchesPattern(action1, action2) || MatchesPattern(action2, action1);
        }

        private async Task<bool> HasCircularDependency(Guid policyId, Guid potentialAncestorId)
        {
            var visited = new HashSet<Guid>();
            var currentId = policyId;

            while (currentId != Guid.Empty)
            {
                if (visited.Contains(currentId))
                {
                    return true; // Circular dependency detected
                }

                if (currentId == potentialAncestorId)
                {
                    return true; // Would create circular dependency
                }

                visited.Add(currentId);

                var policy = await _context.Policies.FindAsync(currentId);
                currentId = policy?.ParentId ?? Guid.Empty;
            }

            return false;
        }

        private async Task ClearUserCacheAsync(Guid userId)
        {
            await _cacheService.RemoveAsync($"user:roles:{userId}");
            await _cacheService.RemoveAsync($"user:policies:{userId}");
            // Remove evaluation cache entries for this user
            // In production, this might use pattern-based cache clearing
        }

        #endregion

        #region Supporting Classes

        private class ConditionEvaluationResult
        {
            public bool IsMatched { get; set; }
            public List<string> FailedConditions { get; set; } = new();
        }

        private class PolicyImport
        {
            public string Version { get; set; } = "1.0";
            public PolicyImportData[] Policies { get; set; } = Array.Empty<PolicyImportData>();
        }

        private class PolicyImportData
        {
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string Effect { get; set; } = "Deny";
            public string[] Resources { get; set; } = Array.Empty<string>();
            public string[] Actions { get; set; } = Array.Empty<string>();
            public Dictionary<string, object>? Conditions { get; set; }
            public int Priority { get; set; }
            public Dictionary<string, string>? Tags { get; set; }
        }

        private class RoleImport
        {
            public string Version { get; set; } = "1.0";
            public RoleImportData[] Roles { get; set; } = Array.Empty<RoleImportData>();
        }

        private class RoleImportData
        {
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public Guid[] PolicyIds { get; set; } = Array.Empty<Guid>();
            public Dictionary<string, string>? Tags { get; set; }
        }

        private class NullMetricsCollector : IMetricsCollector
        {
            public void RecordGauge(string name, double value, Dictionary<string, string>? tags = null) { }
            public void RecordCounter(string name, double value = 1, Dictionary<string, string>? tags = null) { }
            public void RecordHistogram(string name, double value, Dictionary<string, string>? tags = null) { }
            public void RecordTimer(string name, double milliseconds, Dictionary<string, string>? tags = null) { }
            public double GetGaugeValue(string name, Dictionary<string, string>? tags = null) => 0;
            public long GetCounterValue(string name, Dictionary<string, string>? tags = null) => 0;
        }

        #endregion
    }
}
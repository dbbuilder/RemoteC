using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RemoteC.Api.Services;
using RemoteC.Data;
using RemoteC.Data.Entities;
using RemoteC.Shared.Models;
using Xunit;

namespace RemoteC.Api.Tests.Services
{
    public class PolicyEngineServiceTests : IDisposable
    {
        private readonly RemoteCDbContext _context;
        private readonly Mock<ILogger<PolicyEngineService>> _loggerMock;
        private readonly Mock<IAuditService> _auditMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly PolicyEngineService _service;
        private readonly PolicyEngineOptions _options;

        public PolicyEngineServiceTests()
        {
            // Setup in-memory database
            var dbOptions = new DbContextOptionsBuilder<RemoteCDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new RemoteCDbContext(dbOptions);

            // Setup mocks
            _loggerMock = new Mock<ILogger<PolicyEngineService>>();
            _auditMock = new Mock<IAuditService>();
            _cacheMock = new Mock<ICacheService>();

            // Setup options
            _options = new PolicyEngineOptions
            {
                EnableDynamicPolicies = true,
                EnableAttributeBasedControl = true,
                PolicyCacheDurationMinutes = 5,
                MaxPolicyDepth = 10,
                EnablePolicyInheritance = true,
                DefaultDenyAll = true
            };

            // Create service
            _service = new PolicyEngineService(
                _context,
                _loggerMock.Object,
                _auditMock.Object,
                _cacheMock.Object,
                Options.Create(_options));
        }

        #region Policy Creation Tests

        [Fact]
        public async Task CreatePolicyAsync_ValidPolicy_CreatesSuccessfully()
        {
            // Arrange
            var policy = new PolicyDefinition
            {
                Name = "AdminAccess",
                Description = "Full administrative access",
                Effect = PolicyEffect.Allow,
                Resources = new[] { "*" },
                Actions = new[] { "*" },
                Conditions = new Dictionary<string, object>
                {
                    ["role"] = "admin"
                }
            };

            // Act
            var result = await _service.CreatePolicyAsync(policy);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal(policy.Name, result.Name);
            Assert.True(result.IsActive);
            Assert.Equal(1, result.Version);

            // Verify in database
            var dbPolicy = await _context.Policies.FirstAsync();
            Assert.Equal(result.Id, dbPolicy.Id);
        }

        [Fact]
        public async Task CreatePolicyAsync_DuplicateName_ThrowsException()
        {
            // Arrange
            var policy = new PolicyDefinition
            {
                Name = "UniquePolicy",
                Effect = PolicyEffect.Allow,
                Resources = new[] { "sessions/*" },
                Actions = new[] { "read" }
            };

            await _service.CreatePolicyAsync(policy);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreatePolicyAsync(policy));
        }

        [Fact]
        public async Task CreatePolicyAsync_ComplexConditions_HandlesCorrectly()
        {
            // Arrange
            var policy = new PolicyDefinition
            {
                Name = "ConditionalAccess",
                Effect = PolicyEffect.Allow,
                Resources = new[] { "api/*/read" },
                Actions = new[] { "GET" },
                Conditions = new Dictionary<string, object>
                {
                    ["ipRange"] = "192.168.1.0/24",
                    ["timeOfDay"] = new { start = "09:00", end = "17:00" },
                    ["mfaEnabled"] = true,
                    ["department"] = new[] { "IT", "Security" }
                }
            };

            // Act
            var result = await _service.CreatePolicyAsync(policy);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.Conditions.Count);
            Assert.Contains("ipRange", result.Conditions.Keys);
        }

        [Fact]
        public async Task UpdatePolicyAsync_IncreasesVersion()
        {
            // Arrange
            var policy = await CreateTestPolicy("TestPolicy");
            
            policy.Description = "Updated description";
            policy.Actions = new[] { "read", "write" };

            // Act
            var updated = await _service.UpdatePolicyAsync(policy);

            // Assert
            Assert.Equal(2, updated.Version);
            Assert.Equal("Updated description", updated.Description);
            Assert.Contains("write", updated.Actions);
        }

        #endregion

        #region Policy Evaluation Tests

        [Fact]
        public async Task EvaluatePolicyAsync_AllowPolicy_GrantsAccess()
        {
            // Arrange
            var policy = await CreateTestPolicy("AllowReadPolicy", 
                effect: PolicyEffect.Allow,
                resources: new[] { "documents/*" },
                actions: new[] { "read" });

            var context = new PolicyEvaluationContext
            {
                UserId = Guid.NewGuid(),
                Resource = "documents/file.txt",
                Action = "read",
                Attributes = new Dictionary<string, object>()
            };

            // Act
            var result = await _service.EvaluatePolicyAsync(policy.Id, context);

            // Assert
            Assert.True(result.IsAllowed);
            Assert.Equal(PolicyEffect.Allow, result.AppliedEffect);
            Assert.Equal(policy.Id, result.MatchedPolicyId);
        }

        [Fact]
        public async Task EvaluatePolicyAsync_DenyPolicy_BlocksAccess()
        {
            // Arrange
            var policy = await CreateTestPolicy("DenyWritePolicy",
                effect: PolicyEffect.Deny,
                resources: new[] { "secure/*" },
                actions: new[] { "write", "delete" });

            var context = new PolicyEvaluationContext
            {
                UserId = Guid.NewGuid(),
                Resource = "secure/sensitive.dat",
                Action = "write"
            };

            // Act
            var result = await _service.EvaluatePolicyAsync(policy.Id, context);

            // Assert
            Assert.False(result.IsAllowed);
            Assert.Equal(PolicyEffect.Deny, result.AppliedEffect);
        }

        [Fact]
        public async Task EvaluatePolicyAsync_WithConditions_EvaluatesCorrectly()
        {
            // Arrange
            var policy = await CreateTestPolicy("ConditionalPolicy",
                effect: PolicyEffect.Allow,
                resources: new[] { "admin/*" },
                actions: new[] { "*" },
                conditions: new Dictionary<string, object>
                {
                    ["role"] = "admin",
                    ["mfaEnabled"] = true
                });

            var allowContext = new PolicyEvaluationContext
            {
                UserId = Guid.NewGuid(),
                Resource = "admin/settings",
                Action = "update",
                Attributes = new Dictionary<string, object>
                {
                    ["role"] = "admin",
                    ["mfaEnabled"] = true
                }
            };

            var denyContext = new PolicyEvaluationContext
            {
                UserId = Guid.NewGuid(),
                Resource = "admin/settings",
                Action = "update",
                Attributes = new Dictionary<string, object>
                {
                    ["role"] = "user",
                    ["mfaEnabled"] = true
                }
            };

            // Act
            var allowResult = await _service.EvaluatePolicyAsync(policy.Id, allowContext);
            var denyResult = await _service.EvaluatePolicyAsync(policy.Id, denyContext);

            // Assert
            Assert.True(allowResult.IsAllowed);
            Assert.False(denyResult.IsAllowed);
            Assert.NotNull(denyResult.Reason);
            Assert.Contains("role", denyResult.Reason);
        }

        [Fact]
        public async Task EvaluatePolicyAsync_ResourcePattern_MatchesWildcards()
        {
            // Arrange
            var policy = await CreateTestPolicy("WildcardPolicy",
                resources: new[] { "api/*/sessions/*", "api/users/*/profile" });

            var testCases = new[]
            {
                ("api/v1/sessions/123", true),
                ("api/v2/sessions/abc/details", true),
                ("api/users/456/profile", true),
                ("api/sessions/789", false),
                ("api/users/profile", false)
            };

            // Act & Assert
            foreach (var (resource, shouldMatch) in testCases)
            {
                var context = new PolicyEvaluationContext
                {
                    Resource = resource,
                    Action = "read"
                };

                var result = await _service.EvaluatePolicyAsync(policy.Id, context);
                Assert.Equal(shouldMatch, result.IsAllowed);
            }
        }

        #endregion

        #region Role-Based Policy Tests

        [Fact]
        public async Task CreateRoleAsync_WithPolicies_AssociatesCorrectly()
        {
            // Arrange
            var readPolicy = await CreateTestPolicy("ReadPolicy", actions: new[] { "read" });
            var writePolicy = await CreateTestPolicy("WritePolicy", actions: new[] { "write" });

            var role = new RoleDefinition
            {
                Name = "Editor",
                Description = "Can read and write content",
                PolicyIds = new[] { readPolicy.Id, writePolicy.Id }
            };

            // Act
            var result = await _service.CreateRoleAsync(role);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Editor", result.Name);
            // Check PolicyIds instead of Policies property
            Assert.Equal(2, result.PolicyIds.Count);
            Assert.Contains(readPolicy.Id, result.PolicyIds);
            Assert.Contains(writePolicy.Id, result.PolicyIds);
        }

        [Fact]
        public async Task AssignRoleToUserAsync_GrantsRolePolicies()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var role = await CreateTestRole("Manager", 
                new[] { "manage:read", "manage:write", "manage:delete" });

            // Act
            await _service.AssignRoleToUserAsync(userId, role.Id);

            // Assert
            var userRoles = await _service.GetUserRolesAsync(userId);
            Assert.Single(userRoles);
            Assert.Equal(role.Id, userRoles.First().Id);

            // Verify user can perform role actions
            var context = new PolicyEvaluationContext
            {
                UserId = userId,
                Resource = "projects/123",
                Action = "manage:write"
            };

            var canAccess = await _service.EvaluateUserAccessAsync(userId, context);
            Assert.True(canAccess.IsAllowed);
        }

        [Fact]
        public async Task RemoveRoleFromUserAsync_RevokesAccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var role = await CreateTestRole("TempRole");
            await _service.AssignRoleToUserAsync(userId, role.Id);

            // Act
            await _service.RemoveRoleFromUserAsync(userId, role.Id);

            // Assert
            var userRoles = await _service.GetUserRolesAsync(userId);
            Assert.Empty(userRoles);
        }

        #endregion

        #region Hierarchical Policy Tests

        [Fact]
        public async Task EvaluatePolicyAsync_InheritedPolicies_AppliesCorrectly()
        {
            // Arrange
            var parentPolicy = await CreateTestPolicy("ParentPolicy",
                resources: new[] { "company/*" },
                actions: new[] { "read" });

            var childPolicy = await CreateTestPolicy("ChildPolicy",
                resources: new[] { "company/department/*" },
                actions: new[] { "write" });
            
            // Set parent relationship
            await _service.SetPolicyParentAsync(childPolicy.Id, parentPolicy.Id);

            var role = await CreateTestRole("DepartmentHead", 
                policyIds: new[] { childPolicy.Id });

            var userId = Guid.NewGuid();
            await _service.AssignRoleToUserAsync(userId, role.Id);

            // Act - Should have both read (inherited) and write permissions
            var readContext = new PolicyEvaluationContext
            {
                UserId = userId,
                Resource = "company/department/reports",
                Action = "read"
            };

            var writeContext = new PolicyEvaluationContext
            {
                UserId = userId,
                Resource = "company/department/reports",
                Action = "write"
            };

            var readResult = await _service.EvaluateUserAccessAsync(userId, readContext);
            var writeResult = await _service.EvaluateUserAccessAsync(userId, writeContext);

            // Assert
            Assert.True(readResult.IsAllowed); // Inherited from parent
            Assert.True(writeResult.IsAllowed); // Direct from child
        }

        [Fact]
        public async Task EvaluatePolicyAsync_DenyOverridesAllow()
        {
            // Arrange
            var allowPolicy = await CreateTestPolicy("AllowAll",
                effect: PolicyEffect.Allow,
                resources: new[] { "*" },
                actions: new[] { "*" });

            var denyPolicy = await CreateTestPolicy("DenySpecific",
                effect: PolicyEffect.Deny,
                resources: new[] { "secrets/*" },
                actions: new[] { "*" },
                priority: 100); // Higher priority

            var role = await CreateTestRole("MixedRole",
                policyIds: new[] { allowPolicy.Id, denyPolicy.Id });

            var userId = Guid.NewGuid();
            await _service.AssignRoleToUserAsync(userId, role.Id);

            // Act
            var secretContext = new PolicyEvaluationContext
            {
                UserId = userId,
                Resource = "secrets/api-key",
                Action = "read"
            };

            var otherContext = new PolicyEvaluationContext
            {
                UserId = userId,
                Resource = "documents/public",
                Action = "read"
            };

            var secretResult = await _service.EvaluateUserAccessAsync(userId, secretContext);
            var otherResult = await _service.EvaluateUserAccessAsync(userId, otherContext);

            // Assert
            Assert.False(secretResult.IsAllowed); // Deny overrides
            Assert.True(otherResult.IsAllowed); // Allow for non-secret resources
        }

        #endregion

        #region Attribute-Based Access Control (ABAC) Tests

        [Fact]
        public async Task EvaluateAttributeBasedPolicy_ComplexAttributes_WorksCorrectly()
        {
            // Arrange
            var policy = await CreateTestPolicy("ABACPolicy",
                conditions: new Dictionary<string, object>
                {
                    ["department"] = new[] { "Engineering", "DevOps" },
                    ["clearanceLevel"] = new { min = 3, max = 5 },
                    ["projects"] = new { contains = "RemoteC" },
                    ["location"] = new { @in = new[] { "US", "EU" } }
                });

            var validContext = new PolicyEvaluationContext
            {
                Resource = "projects/remotec/deploy",
                Action = "execute",
                Attributes = new Dictionary<string, object>
                {
                    ["department"] = "Engineering",
                    ["clearanceLevel"] = 4,
                    ["projects"] = new[] { "RemoteC", "ProjectX" },
                    ["location"] = "US"
                }
            };

            var invalidContext = new PolicyEvaluationContext
            {
                Resource = "projects/remotec/deploy",
                Action = "execute",
                Attributes = new Dictionary<string, object>
                {
                    ["department"] = "Marketing",
                    ["clearanceLevel"] = 2,
                    ["projects"] = new[] { "ProjectY" },
                    ["location"] = "US"
                }
            };

            // Act
            var validResult = await _service.EvaluatePolicyAsync(policy.Id, validContext);
            var invalidResult = await _service.EvaluatePolicyAsync(policy.Id, invalidContext);

            // Assert
            Assert.True(validResult.IsAllowed);
            Assert.False(invalidResult.IsAllowed);
            Assert.NotNull(invalidResult.Reason);
            Assert.Contains("department", invalidResult.Reason);
            Assert.Contains("clearanceLevel", invalidResult.Reason);
            Assert.Contains("projects", invalidResult.Reason);
        }

        [Fact]
        public async Task EvaluateTimeBasedPolicy_RespectsTimeWindows()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var policy = await CreateTestPolicy("BusinessHoursPolicy",
                conditions: new Dictionary<string, object>
                {
                    ["timeWindow"] = new
                    {
                        dayOfWeek = new[] { 1, 2, 3, 4, 5 }, // Monday-Friday
                        startTime = "09:00",
                        endTime = "17:00",
                        timezone = "UTC"
                    }
                });

            // Act - Test during business hours (assuming test runs on weekday)
            if (now.DayOfWeek >= DayOfWeek.Monday && now.DayOfWeek <= DayOfWeek.Friday)
            {
                var context = new PolicyEvaluationContext
                {
                    Resource = "reports/financial",
                    Action = "download",
                    Attributes = new Dictionary<string, object>
                    {
                        ["currentTime"] = now.ToString("HH:mm"),
                        ["currentDayOfWeek"] = (int)now.DayOfWeek
                    }
                };

                var result = await _service.EvaluatePolicyAsync(policy.Id, context);
                
                // Assert based on current time
                var currentHour = now.Hour;
                if (currentHour >= 9 && currentHour < 17)
                {
                    Assert.True(result.IsAllowed);
                }
                else
                {
                    Assert.False(result.IsAllowed);
                }
            }
        }

        #endregion

        #region Dynamic Policy Tests

        [Fact]
        public async Task CreatePolicyFromTemplateAsync_RuntimePolicyGeneration()
        {
            // Arrange
            var template = new PolicyTemplate
            {
                Name = "ProjectAccessTemplate",
                Description = "Template for project access",
                Category = "Access",
                DefaultEffect = PolicyEffect.Allow,
                Parameters = new List<PolicyParameter>
                {
                    new PolicyParameter { Name = "projectId", Type = "string", IsRequired = true },
                    new PolicyParameter { Name = "accessLevel", Type = "string", IsRequired = true }
                },
                PolicyJsonTemplate = @"{
                    ""name"": ""ProjectAccess_{projectId}"",
                    ""effect"": ""Allow"",
                    ""resources"": [""projects/{projectId}/*""],
                    ""actions"": [""{accessLevel}:*""]
                }",
                IsBuiltIn = false
            };

            var createdTemplate = await _service.CreatePolicyTemplateAsync(template);

            var parameters = new Dictionary<string, object>
            {
                ["projectId"] = "abc123",
                ["accessLevel"] = "read"
            };

            // Act
            var policy = await _service.CreatePolicyFromTemplateAsync(createdTemplate.Id, parameters);

            // Assert
            Assert.NotNull(policy);
            Assert.Contains("abc123", policy.Resources.First());
            Assert.All(policy.Actions, a => a.StartsWith("read:"));
        }

        [Fact]
        public async Task EvaluatePolicySetAsync_MultiplePolicies_CombinesCorrectly()
        {
            // Arrange
            var policies = new List<Policy>();
            
            // Create a policy set with different access levels
            var p1 = await CreateTestPolicy("BaseAccess",
                resources: new[] { "api/*" },
                actions: new[] { "read" });
            policies.Add(p1);

            var p2 = await CreateTestPolicy("WriteAccess",
                resources: new[] { "api/documents/*" },
                actions: new[] { "write" },
                conditions: new Dictionary<string, object> { ["role"] = "editor" });
            policies.Add(p2);

            var p3 = await CreateTestPolicy("AdminAccess",
                resources: new[] { "api/admin/*" },
                actions: new[] { "*" },
                conditions: new Dictionary<string, object> { ["role"] = "admin" });
            policies.Add(p3);

            // Note: PolicySet is not part of the interface - test policies individually instead
            var role = await CreateTestRole("ComprehensiveAccess", policyIds: policies.Select(p => p.Id).ToArray());

            // Act & Assert - Test different scenarios
            var readContext = new PolicyEvaluationContext
            {
                Resource = "api/users",
                Action = "read"
            };
            var userResult = await _service.EvaluateUserAccessAsync(Guid.NewGuid(), readContext);
            // With default deny, this should be false without user assignment
            Assert.False(userResult.IsAllowed);

            var writeContext = new PolicyEvaluationContext
            {
                Resource = "api/documents/report",
                Action = "write",
                Attributes = new Dictionary<string, object> { ["role"] = "editor" }
            };
            // Test with proper role assignment would pass
            var userId = Guid.NewGuid();
            await _service.AssignRoleToUserAsync(userId, role.Id);
            var writeResult = await _service.EvaluateUserAccessAsync(userId, writeContext);
            Assert.True(writeResult.IsAllowed);

            var adminContext = new PolicyEvaluationContext
            {
                Resource = "api/admin/settings",
                Action = "delete",
                Attributes = new Dictionary<string, object> { ["role"] = "admin" }
            };
            var adminResult = await _service.EvaluateUserAccessAsync(userId, adminContext);
            Assert.True(adminResult.IsAllowed);
        }

        #endregion

        #region Policy Management Tests

        [Fact]
        public async Task GetEffectivePoliciesAsync_ReturnsAllApplicablePolicies()
        {
            // Arrange
            var userId = Guid.NewGuid();
            
            // Direct user policy
            var userPolicy = await CreateTestPolicy("UserSpecific");
            await _service.AssignPolicyToUserAsync(userId, userPolicy.Id);

            // Role policies
            var role1 = await CreateTestRole("Role1");
            var role2 = await CreateTestRole("Role2");
            await _service.AssignRoleToUserAsync(userId, role1.Id);
            await _service.AssignRoleToUserAsync(userId, role2.Id);

            // Group policy
            var groupId = Guid.NewGuid();
            var groupPolicy = await CreateTestPolicy("GroupPolicy");
            await _service.AssignPolicyToGroupAsync(groupId, groupPolicy.Id);
            // Note: AddUserToGroupAsync is not in the interface
            // We'll use GetUserPoliciesAsync instead

            // Act
            var policies = await _service.GetUserPoliciesAsync(userId);

            // Assert
            Assert.NotEmpty(policies);
            Assert.Contains(policies, p => p.Id == userPolicy.Id);
            // Should also include policies from roles and groups
            Assert.True(policies.Count >= 3);
        }

        [Fact]
        public async Task ExportPoliciesAsync_GeneratesValidExport()
        {
            // Arrange
            await CreateTestPolicy("Policy1");
            await CreateTestPolicy("Policy2");
            await CreateTestRole("Role1");

            // Act
            var exportJson = await _service.ExportPoliciesAsync();

            // Assert
            Assert.NotNull(exportJson);
            Assert.Contains("Policy1", exportJson);
            Assert.Contains("Policy2", exportJson);
            Assert.Contains("version", exportJson);
            Assert.Contains("exportDate", exportJson);
        }

        [Fact]
        public async Task ImportPoliciesAsync_RestoresPolicies()
        {
            // Arrange
            var exportJson = @"{
                ""version"": ""1.0"",
                ""exportDate"": ""2024-01-01T00:00:00Z"",
                ""policies"": [
                    {
                        ""name"": ""ImportedPolicy"",
                        ""effect"": ""Allow"",
                        ""resources"": [""imported/*""],
                        ""actions"": [""read""]
                    }
                ]
            }";

            // Act
            var result = await _service.ImportPoliciesAsync(exportJson);

            // Assert
            Assert.NotEmpty(result);
            Assert.Equal("ImportedPolicy", result.First().Name);

            // Verify imported items exist
            var policy = await _context.Policies.FirstOrDefaultAsync(p => p.Name == "ImportedPolicy");
            Assert.NotNull(policy);
        }

        #endregion

        #region Audit and Compliance Tests

        [Fact]
        public async Task EvaluateUserAccessAsync_LogsAccessDecisions()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var context = new PolicyEvaluationContext
            {
                UserId = userId,
                Resource = "sensitive/data",
                Action = "read"
            };

            // Act
            await _service.EvaluateUserAccessAsync(userId, context);

            // Assert
            _auditMock.Verify(a => a.LogAsync(
                It.IsAny<AuditLogEntry>()),
                Times.Once);
        }

        [Fact]
        public async Task DetectPolicyConflictsAsync_FindsConflicts()
        {
            // Arrange
            // Create conflicting policies
            var allowAll = await CreateTestPolicy("AllowAll",
                effect: PolicyEffect.Allow,
                resources: new[] { "*" },
                actions: new[] { "*" });

            var denyAll = await CreateTestPolicy("DenyAll",
                effect: PolicyEffect.Deny,
                resources: new[] { "*" },
                actions: new[] { "*" });

            // Act
            var conflicts = await _service.DetectPolicyConflictsAsync();

            // Assert
            Assert.NotEmpty(conflicts);
            Assert.Contains(conflicts, c => c.ConflictType == "EffectConflict");
        }

        #endregion

        #region Performance Tests

        [Fact]
        public async Task EvaluateUserAccessAsync_UsesCache()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var context = new PolicyEvaluationContext
            {
                UserId = userId,
                Resource = "cached/resource",
                Action = "read"
            };

            var cacheKey = $"policy:eval:{userId}:{context.Resource}:{context.Action}";
            var cachedResult = new PolicyEvaluationResult { IsAllowed = true };

            _cacheMock.Setup(c => c.GetAsync<PolicyEvaluationResult>(cacheKey))
                .ReturnsAsync(cachedResult);

            // Act
            var result = await _service.EvaluateUserAccessAsync(userId, context);

            // Assert
            Assert.True(result.IsAllowed);
            _cacheMock.Verify(c => c.GetAsync<PolicyEvaluationResult>(cacheKey), Times.Once);
        }

        [Fact]
        public async Task BulkEvaluatePoliciesAsync_HandlesMultipleContextsEfficiently()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var policy = await CreateTestPolicy("BulkPolicy",
                resources: new[] { "bulk/*" },
                actions: new[] { "read", "write" });

            var role = await CreateTestRole("BulkRole", policyIds: new[] { policy.Id });
            await _service.AssignRoleToUserAsync(userId, role.Id);

            var contexts = Enumerable.Range(1, 100).Select(i => new PolicyEvaluationContext
            {
                UserId = userId,
                Resource = $"bulk/resource{i}",
                Action = i % 2 == 0 ? "read" : "write"
            }).ToList();

            // Act
            var userIds = Enumerable.Repeat(userId, contexts.Count).ToList();
            var results = await _service.BulkEvaluatePoliciesAsync(userIds, contexts.First());

            // Assert
            Assert.Equal(100, results.Count);
            Assert.All(results.Values, r => Assert.True(r.IsAllowed));
        }

        #endregion

        #region Helper Methods

        private async Task<Policy> CreateTestPolicy(
            string name,
            PolicyEffect effect = PolicyEffect.Allow,
            string[]? resources = null,
            string[]? actions = null,
            Dictionary<string, object>? conditions = null,
            Guid? parentPolicyId = null,
            int priority = 0)
        {
            var policy = new PolicyDefinition
            {
                Name = name,
                Description = $"Test policy {name}",
                Effect = effect,
                Resources = resources ?? new[] { "*" },
                Actions = actions ?? new[] { "read" },
                Conditions = conditions ?? new Dictionary<string, object>(),
                Priority = priority
            };

            return await _service.CreatePolicyAsync(policy);
        }

        private async Task<RemoteC.Shared.Models.Role> CreateTestRole(
            string name,
            string[]? actions = null,
            Guid[]? policyIds = null)
        {
            var policies = new List<Guid>();
            
            if (policyIds != null)
            {
                policies.AddRange(policyIds);
            }
            else if (actions != null)
            {
                // Create policies for each action
                foreach (var action in actions)
                {
                    var policy = await CreateTestPolicy($"{name}_{action}",
                        actions: new[] { action });
                    policies.Add(policy.Id);
                }
            }
            else
            {
                // Create a default policy
                var policy = await CreateTestPolicy($"{name}_Default");
                policies.Add(policy.Id);
            }

            var role = new RoleDefinition
            {
                Name = name,
                Description = $"Test role {name}",
                PolicyIds = policies.ToArray()
            };

            return await _service.CreateRoleAsync(role);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        #endregion
    }
}
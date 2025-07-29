using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using RemoteC.Data;
using RemoteC.Data.Entities;
using RemoteC.Shared.Models;
using System.Text.Json;

namespace RemoteC.Api.Services
{
    /// <summary>
    /// Advanced permission service with granular RBAC support
    /// </summary>
    public class AdvancedPermissionService : IAdvancedPermissionService
    {
        private readonly RemoteCDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly IAuditService _auditService;
        private readonly ILogger<AdvancedPermissionService> _logger;
        
        private const string PERMISSION_CACHE_PREFIX = "permissions:";
        private const int CACHE_DURATION_MINUTES = 5;

        public AdvancedPermissionService(
            RemoteCDbContext context,
            IDistributedCache cache,
            IAuditService auditService,
            ILogger<AdvancedPermissionService> logger)
        {
            _context = context;
            _cache = cache;
            _auditService = auditService;
            _logger = logger;
        }

        /// <summary>
        /// Check if user has permission for a specific resource and action
        /// </summary>
        public async Task<bool> CheckPermissionAsync(
            Guid userId, 
            string resource, 
            string action, 
            Guid? resourceId = null,
            Dictionary<string, object>? context = null)
        {
            try
            {
                // Check cache first
                var cacheKey = $"{PERMISSION_CACHE_PREFIX}{userId}:{resource}:{action}:{resourceId}";
                var cachedResult = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedResult))
                {
                    return bool.Parse(cachedResult);
                }

                // Get user with roles and organization
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                            .ThenInclude(r => r.RolePermissions)
                                .ThenInclude(rp => rp.Permission)
                    .Include(u => u.Organization)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null || !user.IsActive)
                {
                    await LogPermissionCheck(userId, resource, action, false, "User not found or inactive");
                    return false;
                }

                // Super admin bypass
                if (user.IsSuperAdmin)
                {
                    await CachePermissionResult(cacheKey, true);
                    return true;
                }

                // Check organization-level restrictions
                if (!await CheckOrganizationPermissionsAsync(user, resource, action))
                {
                    await LogPermissionCheck(userId, resource, action, false, "Organization restriction");
                    return false;
                }

                // Collect all permissions from roles
                var permissions = GetUserPermissions(user);

                // Check for explicit permission
                var hasPermission = CheckExplicitPermission(permissions, resource, action);

                // Check resource-specific permissions
                if (!hasPermission && resourceId.HasValue)
                {
                    hasPermission = await CheckResourcePermissionAsync(
                        user, permissions, resource, action, resourceId.Value, context);
                }

                // Check for wildcard permissions
                if (!hasPermission)
                {
                    hasPermission = CheckWildcardPermission(permissions, resource, action);
                }

                // Cache the result
                await CachePermissionResult(cacheKey, hasPermission);

                // Audit the permission check
                await LogPermissionCheck(userId, resource, action, hasPermission);

                return hasPermission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error checking permission for user {UserId}, resource {Resource}, action {Action}", 
                    userId, resource, action);
                return false;
            }
        }

        /// <summary>
        /// Get all effective permissions for a user
        /// </summary>
        public async Task<IEnumerable<EffectivePermission>> GetEffectivePermissionsAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return Enumerable.Empty<EffectivePermission>();
            }

            var effectivePermissions = new List<EffectivePermission>();

            // Super admin gets all permissions
            if (user.IsSuperAdmin)
            {
                var allPermissions = await _context.Permissions.ToListAsync();
                return allPermissions.Select(p => new EffectivePermission
                {
                    Resource = p.Resource,
                    Action = p.Action,
                    Effect = PermissionEffect.Allow,
                    Source = "SuperAdmin",
                    Constraints = new Dictionary<string, object>()
                });
            }

            // Collect permissions from all roles
            var rolePermissions = user.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission)
                .Distinct();

            foreach (var permission in rolePermissions)
            {
                var constraints = new Dictionary<string, object>();

                // Add organization constraint
                if (user.OrganizationId.HasValue)
                {
                    constraints["OrganizationId"] = user.OrganizationId.Value;
                }

                // Check for resource-specific constraints
                var resourceConstraints = await GetResourceConstraintsAsync(
                    user, permission.Resource, permission.Action);
                
                foreach (var constraint in resourceConstraints)
                {
                    constraints[constraint.Key] = constraint.Value;
                }

                effectivePermissions.Add(new EffectivePermission
                {
                    Resource = permission.Resource,
                    Action = permission.Action,
                    Effect = PermissionEffect.Allow,
                    Source = string.Join(", ", user.UserRoles.Select(ur => ur.Role.Name)),
                    Constraints = constraints
                });
            }

            return effectivePermissions;
        }

        /// <summary>
        /// Grant permission to a role
        /// </summary>
        public async Task<bool> GrantPermissionToRoleAsync(
            Guid roleId, 
            string resource, 
            string action,
            Guid grantedBy)
        {
            try
            {
                // Find or create permission
                var permission = await _context.Permissions
                    .FirstOrDefaultAsync(p => p.Resource == resource && p.Action == action);

                if (permission == null)
                {
                    permission = new Permission
                    {
                        Id = Guid.NewGuid(),
                        Resource = resource,
                        Action = action,
                        Description = $"{action} permission for {resource}"
                    };
                    _context.Permissions.Add(permission);
                }

                // Check if already granted
                var existing = await _context.RolePermissions
                    .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permission.Id);

                if (!existing)
                {
                    _context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = roleId,
                        PermissionId = permission.Id
                    });

                    await _context.SaveChangesAsync();

                    // Clear permission cache for all users with this role
                    await ClearRolePermissionCacheAsync(roleId);

                    // Audit the change
                    await _auditService.LogAsync(new AuditLogEntry
                    {
                        UserId = grantedBy,
                        Action = "GrantPermission",
                        ResourceType = "Role",
                        ResourceId = roleId.ToString(),
                        Details = $"Granted {resource}:{action} permission",
                        Timestamp = DateTime.UtcNow
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error granting permission {Resource}:{Action} to role {RoleId}", 
                    resource, action, roleId);
                return false;
            }
        }

        /// <summary>
        /// Create a custom role with specific permissions
        /// </summary>
        public async Task<RemoteC.Data.Entities.Role?> CreateCustomRoleAsync(
            string name, 
            string description,
            Guid organizationId,
            IEnumerable<string> permissionKeys,
            Guid createdBy)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Create role
                var role = new RemoteC.Data.Entities.Role
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = description,
                    IsSystem = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Roles.Add(role);

                // Add permissions
                foreach (var permissionKey in permissionKeys)
                {
                    var parts = permissionKey.Split(':');
                    if (parts.Length != 2) continue;

                    var resource = parts[0];
                    var action = parts[1];

                    var permission = await _context.Permissions
                        .FirstOrDefaultAsync(p => p.Resource == resource && p.Action == action);

                    if (permission != null)
                    {
                        _context.RolePermissions.Add(new RolePermission
                        {
                            RoleId = role.Id,
                            PermissionId = permission.Id
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Audit
                await _auditService.LogAsync(new AuditLogEntry
                {
                    UserId = createdBy,
                    Action = "CreateRole",
                    ResourceType = "Role",
                    ResourceId = role.Id.ToString(),
                    Details = $"Created custom role: {name}",
                    Timestamp = DateTime.UtcNow,
                    OrganizationId = organizationId
                });

                return role;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating custom role {RoleName}", name);
                return null;
            }
        }

        #region Private Methods

        private HashSet<string> GetUserPermissions(User user)
        {
            var permissions = new HashSet<string>();

            foreach (var userRole in user.UserRoles)
            {
                foreach (var rolePermission in userRole.Role.RolePermissions)
                {
                    var key = $"{rolePermission.Permission.Resource}:{rolePermission.Permission.Action}";
                    permissions.Add(key);
                }
            }

            return permissions;
        }

        private bool CheckExplicitPermission(HashSet<string> permissions, string resource, string action)
        {
            return permissions.Contains($"{resource}:{action}");
        }

        private bool CheckWildcardPermission(HashSet<string> permissions, string resource, string action)
        {
            // Check for wildcard permissions like "devices:*" or "*:read"
            return permissions.Contains($"{resource}:*") || 
                   permissions.Contains($"*:{action}") ||
                   permissions.Contains("*:*");
        }

        private async Task<bool> CheckResourcePermissionAsync(
            User user,
            HashSet<string> permissions,
            string resource,
            string action,
            Guid resourceId,
            Dictionary<string, object>? context)
        {
            // Resource-specific permission checks
            switch (resource.ToLower())
            {
                case "device":
                    return await CheckDevicePermissionAsync(user, resourceId, action, permissions);
                
                case "session":
                    return await CheckSessionPermissionAsync(user, resourceId, action, permissions);
                
                case "user":
                    return await CheckUserPermissionAsync(user, resourceId, action, permissions);
                
                default:
                    return false;
            }
        }

        private async Task<bool> CheckDevicePermissionAsync(
            User user, 
            Guid deviceId, 
            string action,
            HashSet<string> permissions)
        {
            var device = await _context.Devices
                .Include(d => d.DeviceGroupMembers)
                .FirstOrDefaultAsync(d => d.Id == deviceId);

            if (device == null) return false;

            // Check organization match
            if (device.OrganizationId != user.OrganizationId) return false;

            // Check if user has device group permissions
            if (device.DeviceGroupMembers.Any())
            {
                foreach (var member in device.DeviceGroupMembers)
                {
                    if (permissions.Contains($"devicegroup:{member.DeviceGroupId}:{action}"))
                        return true;
                }
            }

            // Check if user is device owner
            if (device.RegisteredBy == user.Id && permissions.Contains($"owndevices:{action}"))
                return true;

            return false;
        }

        private async Task<bool> CheckSessionPermissionAsync(
            User user,
            Guid sessionId,
            string action,
            HashSet<string> permissions)
        {
            var session = await _context.Sessions
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null) return false;

            // Check organization match
            if (session.OrganizationId != user.OrganizationId) return false;

            // Check if user is session creator
            if (session.CreatedBy == user.Id && permissions.Contains($"ownsessions:{action}"))
                return true;

            // Check if user is participant
            var isParticipant = await _context.SessionParticipants
                .AnyAsync(sp => sp.SessionId == sessionId && sp.UserId == user.Id);

            if (isParticipant && permissions.Contains($"participantsessions:{action}"))
                return true;

            return false;
        }

        private async Task<bool> CheckUserPermissionAsync(
            User user,
            Guid targetUserId,
            string action,
            HashSet<string> permissions)
        {
            // Can always read own profile
            if (targetUserId == user.Id && action == "read")
                return true;

            var targetUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == targetUserId);

            if (targetUser == null) return false;

            // Check organization match
            if (targetUser.OrganizationId != user.OrganizationId) return false;

            // Check department-based permissions
            if (!string.IsNullOrEmpty(user.Department) && 
                user.Department == targetUser.Department &&
                permissions.Contains($"departmentusers:{action}"))
                return true;

            return false;
        }

        private async Task<bool> CheckOrganizationPermissionsAsync(
            User user,
            string resource,
            string action)
        {
            if (!user.OrganizationId.HasValue) return true;

            var orgSettings = await _context.OrganizationSettings
                .FirstOrDefaultAsync(os => os.OrganizationId == user.OrganizationId);

            if (orgSettings == null) return true;

            // Check organization-specific restrictions
            if (resource == "session" && action == "record" && !orgSettings.SessionRecordingEnabled)
                return false;

            if (resource == "session" && action == "pin" && !orgSettings.AllowPinAccess)
                return false;

            return true;
        }

        private async Task<Dictionary<string, object>> GetResourceConstraintsAsync(
            User user,
            string resource,
            string action)
        {
            var constraints = new Dictionary<string, object>();

            // Add time-based constraints
            var now = DateTime.UtcNow;
            constraints["CurrentTime"] = now;

            // Add IP-based constraints if available
            if (!string.IsNullOrEmpty(user.LastLoginIp))
            {
                constraints["UserIP"] = user.LastLoginIp;
            }

            // Add organization settings constraints
            if (user.OrganizationId.HasValue)
            {
                var orgSettings = await _context.OrganizationSettings
                    .FirstOrDefaultAsync(os => os.OrganizationId == user.OrganizationId);

                if (orgSettings != null)
                {
                    if (!string.IsNullOrEmpty(orgSettings.IpWhitelist))
                    {
                        constraints["AllowedIPs"] = orgSettings.IpWhitelist.Split(',');
                    }
                }
            }

            return constraints;
        }

        private async Task CachePermissionResult(string cacheKey, bool result)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_DURATION_MINUTES)
            };

            await _cache.SetStringAsync(cacheKey, result.ToString(), options);
        }

        private async Task ClearRolePermissionCacheAsync(Guid roleId)
        {
            // Get all users with this role
            var userIds = await _context.UserRoles
                .Where(ur => ur.RoleId == roleId)
                .Select(ur => ur.UserId)
                .ToListAsync();

            // Clear cache for each user
            foreach (var userId in userIds)
            {
                var pattern = $"{PERMISSION_CACHE_PREFIX}{userId}:*";
                // Note: This would need Redis SCAN in production
                _logger.LogInformation("Clearing permission cache for user {UserId}", userId);
            }
        }

        private async Task LogPermissionCheck(
            Guid userId,
            string resource,
            string action,
            bool granted,
            string? reason = null)
        {
            if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Permission check: User={UserId}, Resource={Resource}, Action={Action}, Granted={Granted}, Reason={Reason}",
                    userId, resource, action, granted, reason);
            }

            // Log denied permissions as audit events
            if (!granted)
            {
                await _auditService.LogAsync(new AuditLogEntry
                {
                    UserId = userId,
                    Action = "PermissionDenied",
                    ResourceType = resource,
                    Details = $"Denied {action} on {resource}. Reason: {reason}",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        #endregion
    }

    public interface IAdvancedPermissionService
    {
        Task<bool> CheckPermissionAsync(Guid userId, string resource, string action, 
            Guid? resourceId = null, Dictionary<string, object>? context = null);
        Task<IEnumerable<EffectivePermission>> GetEffectivePermissionsAsync(Guid userId);
        Task<bool> GrantPermissionToRoleAsync(Guid roleId, string resource, string action, Guid grantedBy);
        Task<RemoteC.Data.Entities.Role?> CreateCustomRoleAsync(string name, string description, Guid organizationId,
            IEnumerable<string> permissionKeys, Guid createdBy);
    }

    public class EffectivePermission
    {
        public string Resource { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public PermissionEffect Effect { get; set; }
        public string Source { get; set; } = string.Empty;
        public Dictionary<string, object> Constraints { get; set; } = new();
    }

    public enum PermissionEffect
    {
        Allow,
        Deny
    }
}
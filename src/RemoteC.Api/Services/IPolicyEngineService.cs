using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RemoteC.Shared.Models;

namespace RemoteC.Api.Services
{
    public interface IPolicyEngineService
    {
        // Policy Management
        Task<Policy> CreatePolicyAsync(PolicyDefinition definition);
        Task<Policy> UpdatePolicyAsync(Guid policyId, PolicyDefinition definition);
        Task<bool> DeletePolicyAsync(Guid policyId);
        Task<Policy?> GetPolicyAsync(Guid policyId);
        Task<List<Policy>> GetPoliciesAsync(string? resource = null, string? action = null);
        Task<PolicyValidationResult> ValidatePolicyAsync(PolicyDefinition definition);
        
        // Role Management
        Task<Role> CreateRoleAsync(RoleDefinition definition);
        Task<Role> UpdateRoleAsync(Guid roleId, RoleDefinition definition);
        Task<bool> DeleteRoleAsync(Guid roleId);
        Task<Role?> GetRoleAsync(Guid roleId);
        Task<List<Role>> GetRolesAsync();
        Task<bool> AttachPolicyToRoleAsync(Guid roleId, Guid policyId);
        Task<bool> DetachPolicyFromRoleAsync(Guid roleId, Guid policyId);
        
        // User and Group Assignments
        Task<bool> AssignRoleToUserAsync(Guid userId, Guid roleId);
        Task<bool> RemoveRoleFromUserAsync(Guid userId, Guid roleId);
        Task<bool> AssignPolicyToUserAsync(Guid userId, Guid policyId, DateTime? expiresAt = null);
        Task<bool> RemovePolicyFromUserAsync(Guid userId, Guid policyId);
        Task<List<Role>> GetUserRolesAsync(Guid userId);
        Task<List<Policy>> GetUserPoliciesAsync(Guid userId);
        
        // Group Management
        Task<bool> AssignRoleToGroupAsync(Guid groupId, Guid roleId);
        Task<bool> RemoveRoleFromGroupAsync(Guid groupId, Guid roleId);
        Task<bool> AssignPolicyToGroupAsync(Guid groupId, Guid policyId);
        Task<bool> RemovePolicyFromGroupAsync(Guid groupId, Guid policyId);
        
        // Policy Evaluation
        Task<PolicyEvaluationResult> EvaluatePolicyAsync(Guid policyId, PolicyEvaluationContext context);
        Task<PolicyEvaluationResult> EvaluateUserAccessAsync(Guid userId, PolicyEvaluationContext context);
        Task<PolicyEvaluationResult> EvaluateGroupAccessAsync(Guid groupId, PolicyEvaluationContext context);
        Task<List<string>> GetAllowedActionsAsync(Guid userId, string resource);
        Task<List<string>> GetAccessibleResourcesAsync(Guid userId, string action);
        
        // Resource and Action Management
        Task<ResourceDefinition> RegisterResourceAsync(ResourceDefinition resource);
        Task<ActionDefinition> RegisterActionAsync(ActionDefinition action);
        Task<List<ResourceDefinition>> GetResourcesAsync(string? type = null);
        Task<List<ActionDefinition>> GetActionsAsync(string? resourceType = null);
        
        // Policy Templates
        Task<PolicyTemplate> CreatePolicyTemplateAsync(PolicyTemplate template);
        Task<Policy> CreatePolicyFromTemplateAsync(Guid templateId, Dictionary<string, object> parameters);
        Task<List<PolicyTemplate>> GetPolicyTemplatesAsync(string? category = null);
        
        // Delegation
        Task<PolicyDelegation> DelegatePolicyAsync(Guid fromUserId, Guid toUserId, Guid policyId, DateTime startDate, DateTime endDate);
        Task<bool> RevokeDelegationAsync(Guid delegationId);
        Task<List<PolicyDelegation>> GetUserDelegationsAsync(Guid userId);
        Task<List<PolicyDelegation>> GetDelegatedPoliciesAsync(Guid userId);
        
        // Analytics and Reporting
        Task<PolicyUsageStats> GetPolicyUsageStatsAsync(Guid policyId, DateTime? startDate = null, DateTime? endDate = null);
        Task<PolicyEffectivenessReport> GenerateEffectivenessReportAsync();
        Task<List<PolicyConflict>> DetectPolicyConflictsAsync();
        Task<bool> ResolvePolicyConflictAsync(Guid conflictId, PolicyConflictResolution resolution);
        
        // Bulk Operations
        Task<Dictionary<Guid, PolicyEvaluationResult>> BulkEvaluatePoliciesAsync(List<Guid> userIds, PolicyEvaluationContext context);
        Task<bool> BulkAssignRoleAsync(List<Guid> userIds, Guid roleId);
        Task<bool> BulkRemoveRoleAsync(List<Guid> userIds, Guid roleId);
        
        // Policy Inheritance
        Task<bool> SetPolicyParentAsync(Guid policyId, Guid parentId);
        Task<List<Policy>> GetPolicyHierarchyAsync(Guid policyId);
        Task<List<Policy>> GetChildPoliciesAsync(Guid parentId);
        
        // Import/Export
        Task<string> ExportPoliciesAsync(List<Guid>? policyIds = null);
        Task<List<Policy>> ImportPoliciesAsync(string policiesJson);
        Task<string> ExportRolesAsync(List<Guid>? roleIds = null);
        Task<List<Role>> ImportRolesAsync(string rolesJson);
    }
}
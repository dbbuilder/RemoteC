using System;
using System.Collections.Generic;

namespace RemoteC.Shared.Models
{
    // Policy Definition Models
    public class PolicyDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public PolicyEffect Effect { get; set; } = PolicyEffect.Deny;
        public string[] Resources { get; set; } = Array.Empty<string>();
        public string[] Actions { get; set; } = Array.Empty<string>();
        public Dictionary<string, object>? Conditions { get; set; }
        public string[]? Principals { get; set; }
        public string[]? NotPrincipals { get; set; }
        public int Priority { get; set; } = 0;
        public Dictionary<string, string>? Tags { get; set; }
    }

    public class Policy
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public PolicyEffect Effect { get; set; }
        public string[] Resources { get; set; } = Array.Empty<string>();
        public string[] Actions { get; set; } = Array.Empty<string>();
        public Dictionary<string, object>? Conditions { get; set; }
        public string[]? Principals { get; set; }
        public string[]? NotPrincipals { get; set; }
        public int Priority { get; set; }
        public bool IsActive { get; set; }
        public int Version { get; set; }
        public Guid? ParentId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public Dictionary<string, string>? Tags { get; set; }
    }

    public enum PolicyEffect
    {
        Allow,
        Deny
    }

    // Role Models
    public class RoleDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid[] PolicyIds { get; set; } = Array.Empty<Guid>();
        public Dictionary<string, string>? Tags { get; set; }
        public bool IsSystem { get; set; }
    }

    public class Role
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<Guid> PolicyIds { get; set; } = new();
        public bool IsActive { get; set; }
        public bool IsSystem { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Dictionary<string, string>? Tags { get; set; }
    }

    // Permission Models
    public class PermissionSet
    {
        public string Name { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public List<string> AllowedActions { get; set; } = new();
        public List<string> DeniedActions { get; set; } = new();
        public Dictionary<string, object>? Conditions { get; set; }
    }

    // Policy Evaluation Models
    public class PolicyEvaluationContext
    {
        public Guid? UserId { get; set; }
        public string? SessionId { get; set; }
        public string Resource { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public Dictionary<string, object> Attributes { get; set; } = new();
        public string? IpAddress { get; set; }
        public DateTime RequestTime { get; set; } = DateTime.UtcNow;
        public Dictionary<string, string>? Headers { get; set; }
    }

    public class PolicyEvaluationResult
    {
        public bool IsAllowed { get; set; }
        public string? Reason { get; set; }
        public Guid? MatchedPolicyId { get; set; }
        public string? MatchedPolicyName { get; set; }
        public PolicyEffect? AppliedEffect { get; set; }
        public List<PolicyTrace> EvaluationTrace { get; set; } = new();
        public TimeSpan EvaluationTime { get; set; }
    }

    public class PolicyTrace
    {
        public Guid PolicyId { get; set; }
        public string PolicyName { get; set; } = string.Empty;
        public bool Matched { get; set; }
        public string? FailureReason { get; set; }
        public PolicyEffect Effect { get; set; }
        public int Priority { get; set; }
    }

    // Resource and Action Models
    public class ResourceDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? ParentResource { get; set; }
        public List<string> SupportedActions { get; set; } = new();
        public Dictionary<string, object>? Metadata { get; set; }
        public bool IsWildcardAllowed { get; set; }
    }

    public class ActionDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ResourceType { get; set; } = string.Empty;
        public bool RequiresMFA { get; set; }
        public bool IsHighRisk { get; set; }
        public string[]? RequiredAttributes { get; set; }
    }

    // Policy Templates
    public class PolicyTemplate
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public PolicyEffect DefaultEffect { get; set; }
        public List<PolicyParameter> Parameters { get; set; } = new();
        public string PolicyJsonTemplate { get; set; } = string.Empty;
        public bool IsBuiltIn { get; set; }
    }

    public class PolicyParameter
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // string, array, object, number, boolean
        public string? Description { get; set; }
        public bool IsRequired { get; set; }
        public object? DefaultValue { get; set; }
        public object[]? AllowedValues { get; set; }
    }

    // Delegation Models
    public class PolicyDelegation
    {
        public Guid Id { get; set; }
        public Guid FromUserId { get; set; }
        public Guid ToUserId { get; set; }
        public Guid PolicyId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public string? Reason { get; set; }
        public Dictionary<string, object>? Constraints { get; set; }
    }

    // Policy Assignment Models
    public class UserPolicyAssignment
    {
        public Guid UserId { get; set; }
        public Guid PolicyId { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? AssignedBy { get; set; }
        public Dictionary<string, object>? Context { get; set; }
    }

    public class GroupPolicyAssignment
    {
        public Guid GroupId { get; set; }
        public Guid PolicyId { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? AssignedBy { get; set; }
    }

    // Policy Validation Models
    public class PolicyValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public Dictionary<string, object>? Metadata { get; set; }
    }

    public class PolicyConflict
    {
        public Guid Policy1Id { get; set; }
        public Guid Policy2Id { get; set; }
        public string ConflictType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public PolicyConflictResolution? Resolution { get; set; }
    }

    public enum PolicyConflictResolution
    {
        HigherPriorityWins,
        DenyOverridesAllow,
        AllowOverridesDeny,
        Manual
    }

    // Policy Analytics Models
    public class PolicyUsageStats
    {
        public Guid PolicyId { get; set; }
        public long EvaluationCount { get; set; }
        public long AllowCount { get; set; }
        public long DenyCount { get; set; }
        public double AverageEvaluationTimeMs { get; set; }
        public DateTime LastEvaluated { get; set; }
        public Dictionary<string, long> DenyReasons { get; set; } = new();
    }

    public class PolicyEffectivenessReport
    {
        public DateTime GeneratedAt { get; set; }
        public int TotalPolicies { get; set; }
        public int ActivePolicies { get; set; }
        public int UnusedPolicies { get; set; }
        public List<PolicyUsageStats> PolicyStats { get; set; } = new();
        public List<PolicyConflict> DetectedConflicts { get; set; } = new();
        public Dictionary<string, object> Recommendations { get; set; } = new();
    }

    // Options
    public class PolicyEngineOptions
    {
        public bool EnableDynamicPolicies { get; set; } = true;
        public bool EnableAttributeBasedControl { get; set; } = true;
        public int PolicyCacheDurationMinutes { get; set; } = 5;
        public int MaxPolicyDepth { get; set; } = 10;
        public bool EnablePolicyInheritance { get; set; } = true;
        public bool DefaultDenyAll { get; set; } = true;
        public PolicyConflictResolution ConflictResolution { get; set; } = PolicyConflictResolution.DenyOverridesAllow;
        public bool EnablePolicyAuditing { get; set; } = true;
        public bool EnablePolicyValidation { get; set; } = true;
        public int MaxConditionComplexity { get; set; } = 20;
        public bool AllowWildcardResources { get; set; } = true;
        public bool AllowWildcardActions { get; set; } = true;
    }
}
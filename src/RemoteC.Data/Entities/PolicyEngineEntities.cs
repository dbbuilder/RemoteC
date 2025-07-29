using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RemoteC.Data.Entities
{
    public class PolicyEntity
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Effect { get; set; } = "Deny";
        
        [Required]
        public string Resources { get; set; } = "[]"; // JSON array
        
        [Required]
        public string Actions { get; set; } = "[]"; // JSON array
        
        public string? Conditions { get; set; } // JSON object
        
        public string? Principals { get; set; } // JSON array
        
        public string? NotPrincipals { get; set; } // JSON array
        
        public int Priority { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public int Version { get; set; } = 1;
        
        public Guid? ParentId { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime? UpdatedAt { get; set; }
        
        [StringLength(200)]
        public string? CreatedBy { get; set; }
        
        public string? Tags { get; set; } // JSON object

        // Navigation properties
        public virtual ICollection<RolePolicyEntity> RolePolicies { get; set; } = new List<RolePolicyEntity>();
        public virtual ICollection<UserPolicyAssignmentEntity> UserPolicyAssignments { get; set; } = new List<UserPolicyAssignmentEntity>();
        public virtual ICollection<GroupPolicyAssignmentEntity> GroupPolicyAssignments { get; set; } = new List<GroupPolicyAssignmentEntity>();
    }

    public class PolicyRoleEntity
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public bool IsSystem { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime? UpdatedAt { get; set; }
        
        public string? Tags { get; set; } // JSON object

        // Navigation properties
        public virtual ICollection<RolePolicyEntity> RolePolicies { get; set; } = new List<RolePolicyEntity>();
        public virtual ICollection<UserPolicyRoleEntity> UserRoles { get; set; } = new List<UserPolicyRoleEntity>();
        public virtual ICollection<GroupPolicyRoleEntity> GroupRoles { get; set; } = new List<GroupPolicyRoleEntity>();
    }

    public class RolePolicyEntity
    {
        public Guid RoleId { get; set; }
        public Guid PolicyId { get; set; }

        // Navigation properties
        public virtual PolicyRoleEntity Role { get; set; } = null!;
        public virtual PolicyEntity Policy { get; set; } = null!;
    }

    public class UserPolicyRoleEntity
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? AssignedBy { get; set; }

        // Navigation properties
        public virtual PolicyRoleEntity Role { get; set; } = null!;
    }

    public class GroupPolicyRoleEntity
    {
        public Guid GroupId { get; set; }
        public Guid RoleId { get; set; }
        public DateTime AssignedAt { get; set; }
        public string? AssignedBy { get; set; }

        // Navigation properties
        public virtual PolicyRoleEntity Role { get; set; } = null!;
    }

    public class UserPolicyAssignmentEntity
    {
        public Guid UserId { get; set; }
        public Guid PolicyId { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? AssignedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? Context { get; set; } // JSON object

        // Navigation properties
        public virtual PolicyEntity Policy { get; set; } = null!;
    }

    public class GroupPolicyAssignmentEntity
    {
        public Guid GroupId { get; set; }
        public Guid PolicyId { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? AssignedBy { get; set; }

        // Navigation properties
        public virtual PolicyEntity Policy { get; set; } = null!;
    }

    public class ResourceDefinitionEntity
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Type { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string? ParentResource { get; set; }
        
        [Required]
        public string SupportedActions { get; set; } = "[]"; // JSON array
        
        public string? Metadata { get; set; } // JSON object
        
        public bool IsWildcardAllowed { get; set; }
    }

    public class ActionDefinitionEntity
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        [StringLength(100)]
        public string ResourceType { get; set; } = string.Empty;
        
        public bool RequiresMFA { get; set; }
        
        public bool IsHighRisk { get; set; }
        
        public string? RequiredAttributes { get; set; } // JSON array
    }

    public class PolicyTemplateEntity
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string DefaultEffect { get; set; } = "Deny";
        
        [Required]
        public string Parameters { get; set; } = "[]"; // JSON array
        
        [Required]
        public string PolicyJsonTemplate { get; set; } = string.Empty;
        
        public bool IsBuiltIn { get; set; }
    }

    public class PolicyDelegationEntity
    {
        [Key]
        public Guid Id { get; set; }
        
        public Guid FromUserId { get; set; }
        
        public Guid ToUserId { get; set; }
        
        public Guid PolicyId { get; set; }
        
        public DateTime StartDate { get; set; }
        
        public DateTime EndDate { get; set; }
        
        public bool IsActive { get; set; }
        
        [StringLength(1000)]
        public string? Reason { get; set; }
        
        public string? Constraints { get; set; } // JSON object
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual PolicyEntity Policy { get; set; } = null!;
    }

    public class UserGroupEntity
    {
        public Guid UserId { get; set; }
        public Guid GroupId { get; set; }
        public DateTime JoinedAt { get; set; }
        public string? AddedBy { get; set; }
    }
}
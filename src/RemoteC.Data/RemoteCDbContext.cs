using Microsoft.EntityFrameworkCore;
using RemoteC.Data.Entities;

namespace RemoteC.Data;

public class RemoteCDbContext : DbContext
{
    public RemoteCDbContext(DbContextOptions<RemoteCDbContext> options)
        : base(options)
    {
    }

    public DbSet<Organization> Organizations { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<Permission> Permissions { get; set; } = null!;
    public DbSet<UserRole> UserRoles { get; set; } = null!;
    public DbSet<RolePermission> RolePermissions { get; set; } = null!;
    public DbSet<Device> Devices { get; set; } = null!;
    public DbSet<DeviceGroup> DeviceGroups { get; set; } = null!;
    public DbSet<DeviceGroupMember> DeviceGroupMembers { get; set; } = null!;
    public DbSet<Session> Sessions { get; set; } = null!;
    public DbSet<SessionParticipant> SessionParticipants { get; set; } = null!;
    public DbSet<SessionLog> SessionLogs { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<SessionPin> SessionPins { get; set; } = null!;
    public DbSet<FileTransfer> FileTransfers { get; set; } = null!;
    public DbSet<SessionRecording> SessionRecordings { get; set; } = null!;
    public DbSet<OrganizationSettings> OrganizationSettings { get; set; } = null!;
    
    // Compliance entities
    public DbSet<ComplianceSettings> ComplianceSettings { get; set; } = null!;
    public DbSet<PrivacyPolicy> PrivacyPolicies { get; set; } = null!;
    public DbSet<DataProcessingAgreement> DataProcessingAgreements { get; set; } = null!;
    public DbSet<ConsentRecord> ConsentRecords { get; set; } = null!;
    public DbSet<RetentionPolicy> RetentionPolicies { get; set; } = null!;
    public DbSet<PHIAccessLog> PHIAccessLogs { get; set; } = null!;
    public DbSet<DataBreach> DataBreaches { get; set; } = null!;
    
    // Analytics entities
    public DbSet<PerformanceMetric> PerformanceMetrics { get; set; } = null!;
    public DbSet<UserActivityLog> UserActivityLogs { get; set; } = null!;
    public DbSet<UserActivity> UserActivities { get; set; } = null!; // Added for test compatibility
    public DbSet<PHIAccess> PHIAccesses { get; set; } = null!; // Added for test compatibility
    public DbSet<BusinessEvent> BusinessEvents { get; set; } = null!;
    public DbSet<CustomAlertEntity> CustomAlerts { get; set; } = null!;
    public DbSet<AlertHistory> AlertHistory { get; set; } = null!;
    public DbSet<ScheduledReportEntity> ScheduledReports { get; set; } = null!;
    public DbSet<DataExportRequest> DataExportRequests { get; set; } = null!;
    public DbSet<HourlyAggregation> HourlyAggregations { get; set; } = null!;
    public DbSet<DailyAggregation> DailyAggregations { get; set; } = null!;
    
    // Edge deployment entities
    public DbSet<EdgeNodeEntity> EdgeNodes { get; set; } = null!;
    public DbSet<EdgeDeploymentEntity> EdgeDeployments { get; set; } = null!;
    public DbSet<ConfigurationHistoryEntity> ConfigurationHistories { get; set; } = null!;
    
    // Policy engine entities
    public DbSet<PolicyEntity> Policies { get; set; } = null!;
    public DbSet<PolicyRoleEntity> PolicyRoles { get; set; } = null!;
    public DbSet<RolePolicyEntity> RolePolicies { get; set; } = null!;
    public DbSet<UserPolicyRoleEntity> UserPolicyRoles { get; set; } = null!;
    public DbSet<GroupPolicyRoleEntity> GroupPolicyRoles { get; set; } = null!;
    public DbSet<UserPolicyAssignmentEntity> UserPolicyAssignments { get; set; } = null!;
    public DbSet<GroupPolicyAssignmentEntity> GroupPolicyAssignments { get; set; } = null!;
    public DbSet<ResourceDefinitionEntity> ResourceDefinitions { get; set; } = null!;
    public DbSet<ActionDefinitionEntity> ActionDefinitions { get; set; } = null!;
    public DbSet<PolicyTemplateEntity> PolicyTemplates { get; set; } = null!;
    public DbSet<PolicyDelegationEntity> PolicyDelegations { get; set; } = null!;
    public DbSet<UserGroupEntity> UserGroups { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure composite keys
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        modelBuilder.Entity<DeviceGroupMember>()
            .HasKey(dgm => new { dgm.DeviceGroupId, dgm.DeviceId });

        // Configure relationships
        modelBuilder.Entity<User>()
            .HasMany(u => u.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId);

        modelBuilder.Entity<Role>()
            .HasMany(r => r.UserRoles)
            .WithOne(ur => ur.Role)
            .HasForeignKey(ur => ur.RoleId);

        modelBuilder.Entity<Role>()
            .HasMany(r => r.RolePermissions)
            .WithOne(rp => rp.Role)
            .HasForeignKey(rp => rp.RoleId);

        modelBuilder.Entity<Permission>()
            .HasMany(p => p.RolePermissions)
            .WithOne(rp => rp.Permission)
            .HasForeignKey(rp => rp.PermissionId);

        modelBuilder.Entity<Session>()
            .HasOne(s => s.Device)
            .WithMany(d => d.Sessions)
            .HasForeignKey(s => s.DeviceId);

        modelBuilder.Entity<Session>()
            .HasOne(s => s.CreatedByUser)
            .WithMany(u => u.CreatedSessions)
            .HasForeignKey(s => s.CreatedBy);

        modelBuilder.Entity<SessionParticipant>()
            .HasOne(sp => sp.Session)
            .WithMany(s => s.Participants)
            .HasForeignKey(sp => sp.SessionId);

        modelBuilder.Entity<SessionParticipant>()
            .HasOne(sp => sp.User)
            .WithMany(u => u.SessionParticipants)
            .HasForeignKey(sp => sp.UserId);

        // Policy Engine configurations
        modelBuilder.Entity<RolePolicyEntity>()
            .HasKey(rp => new { rp.RoleId, rp.PolicyId });

        modelBuilder.Entity<UserPolicyRoleEntity>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        modelBuilder.Entity<GroupPolicyRoleEntity>()
            .HasKey(gr => new { gr.GroupId, gr.RoleId });

        modelBuilder.Entity<UserPolicyAssignmentEntity>()
            .HasKey(upa => new { upa.UserId, upa.PolicyId });

        modelBuilder.Entity<GroupPolicyAssignmentEntity>()
            .HasKey(gpa => new { gpa.GroupId, gpa.PolicyId });

        modelBuilder.Entity<UserGroupEntity>()
            .HasKey(ug => new { ug.UserId, ug.GroupId });

        // Configure stored procedures
        ConfigureStoredProcedures(modelBuilder);

        // Seed initial data
        SeedData(modelBuilder);
    }

    private void ConfigureStoredProcedures(ModelBuilder modelBuilder)
    {
        // Map stored procedures for User entity
        modelBuilder.Entity<User>()
            .ToTable("Users");

        // Map stored procedures for Session entity
        modelBuilder.Entity<Session>()
            .ToTable("Sessions");

        // Note: In EF Core 8, we'll use raw SQL with stored procedures
        // rather than the older stored procedure mapping approach
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed default roles
        var adminRoleId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var operatorRoleId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var viewerRoleId = Guid.Parse("00000000-0000-0000-0000-000000000003");

        var roles = new[]
        {
            new Role { Id = adminRoleId, Name = "Admin", Description = "Full system access", IsActive = true },
            new Role { Id = operatorRoleId, Name = "Operator", Description = "Can control sessions", IsActive = true },
            new Role { Id = viewerRoleId, Name = "Viewer", Description = "Can view sessions only", IsActive = true }
        };

        modelBuilder.Entity<Role>().HasData(roles);

        // Seed default permissions
        var permissions = new[]
        {
            new Permission { Id = Guid.NewGuid(), Name = "session.create", Description = "Create new sessions" },
            new Permission { Id = Guid.NewGuid(), Name = "session.view", Description = "View session details" },
            new Permission { Id = Guid.NewGuid(), Name = "session.control", Description = "Control remote sessions" },
            new Permission { Id = Guid.NewGuid(), Name = "session.admin", Description = "Administer all sessions" },
            new Permission { Id = Guid.NewGuid(), Name = "device.view", Description = "View device information" },
            new Permission { Id = Guid.NewGuid(), Name = "device.admin", Description = "Administer devices" },
            new Permission { Id = Guid.NewGuid(), Name = "user.view", Description = "View user information" },
            new Permission { Id = Guid.NewGuid(), Name = "user.admin", Description = "Administer users" },
            new Permission { Id = Guid.NewGuid(), Name = "audit.view", Description = "View audit logs" },
            new Permission { Id = Guid.NewGuid(), Name = "system.admin", Description = "System administration" }
        };

        modelBuilder.Entity<Permission>().HasData(permissions);
    }
}
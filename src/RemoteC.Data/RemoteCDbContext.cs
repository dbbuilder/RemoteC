using Microsoft.EntityFrameworkCore;
using RemoteC.Data.Entities;

namespace RemoteC.Data;

public class RemoteCDbContext : DbContext
{
    public RemoteCDbContext(DbContextOptions<RemoteCDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<Permission> Permissions { get; set; } = null!;
    public DbSet<UserRole> UserRoles { get; set; } = null!;
    public DbSet<RolePermission> RolePermissions { get; set; } = null!;
    public DbSet<Device> Devices { get; set; } = null!;
    public DbSet<DeviceGroup> DeviceGroups { get; set; } = null!;
    public DbSet<Session> Sessions { get; set; } = null!;
    public DbSet<SessionParticipant> SessionParticipants { get; set; } = null!;
    public DbSet<SessionLog> SessionLogs { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<SessionPin> SessionPins { get; set; } = null!;
    public DbSet<FileTransfer> FileTransfers { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure composite keys
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

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
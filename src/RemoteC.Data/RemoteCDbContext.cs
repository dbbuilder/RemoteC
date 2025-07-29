NewGuid(), Name = "session.create", Description = "Create new sessions" },
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
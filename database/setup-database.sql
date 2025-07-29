-- RemoteC Database Setup Script
-- This script creates the initial database structure for RemoteC application
-- Run with: sqlcmd -S localhost -i setup-database.sql

USE master
GO

-- Create database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'RemoteCDb')
BEGIN
    CREATE DATABASE RemoteCDb
    PRINT 'Database RemoteCDb created successfully'
END
ELSE
BEGIN
    PRINT 'Database RemoteCDb already exists'
END
GO

USE RemoteCDb
GO

-- Enable snapshot isolation for better concurrency
ALTER DATABASE RemoteCDb SET ALLOW_SNAPSHOT_ISOLATION ON
GO
ALTER DATABASE RemoteCDb SET READ_COMMITTED_SNAPSHOT ON
GO

PRINT 'Database configuration completed'
PRINT ''
PRINT 'Creating schema...'
GO

-- Run schema creation
:r initial-schema.sql

-- Run stored procedures
PRINT ''
PRINT 'Creating stored procedures...'
:r stored-procedures/session-procedures.sql
:r stored-procedures/device-procedures.sql
:r stored-procedures/pin-procedures.sql
:r stored-procedures/file-transfer-procedures.sql

-- Create initial data
PRINT ''
PRINT 'Inserting initial data...'
GO

-- Insert default roles (only if they don't exist)
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Id = '00000000-0000-0000-0000-000000000001')
BEGIN
    INSERT INTO Roles (Id, Name, Description, IsActive)
    VALUES 
        ('00000000-0000-0000-0000-000000000001', 'Admin', 'Full system access', 1),
        ('00000000-0000-0000-0000-000000000002', 'Operator', 'Can control sessions', 1),
        ('00000000-0000-0000-0000-000000000003', 'Viewer', 'Can view sessions only', 1)
    PRINT 'Default roles created'
END
GO

-- Insert default permissions (only if they don't exist)
IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'session.create')
BEGIN
    INSERT INTO Permissions (Name, Description, Category, IsActive)
    VALUES 
        ('session.create', 'Create new sessions', 'Session', 1),
        ('session.view', 'View session details', 'Session', 1),
        ('session.control', 'Control remote sessions', 'Session', 1),
        ('session.admin', 'Administer all sessions', 'Session', 1),
        ('device.view', 'View device information', 'Device', 1),
        ('device.admin', 'Administer devices', 'Device', 1),
        ('user.view', 'View user information', 'User', 1),
        ('user.admin', 'Administer users', 'User', 1),
        ('audit.view', 'View audit logs', 'Audit', 1),
        ('system.admin', 'System administration', 'System', 1)
    PRINT 'Default permissions created'
END
GO

-- Assign permissions to roles (only if not already assigned)
-- Admin gets all permissions
INSERT INTO RolePermissions (RoleId, PermissionId, GrantedBy)
SELECT 
    '00000000-0000-0000-0000-000000000001',
    p.Id,
    '00000000-0000-0000-0000-000000000001'
FROM Permissions p
WHERE NOT EXISTS (
    SELECT 1 FROM RolePermissions rp 
    WHERE rp.RoleId = '00000000-0000-0000-0000-000000000001' 
    AND rp.PermissionId = p.Id
)

-- Operator gets session and device permissions
INSERT INTO RolePermissions (RoleId, PermissionId, GrantedBy)
SELECT 
    '00000000-0000-0000-0000-000000000002',
    p.Id,
    '00000000-0000-0000-0000-000000000001'
FROM Permissions p
WHERE p.Name IN ('session.create', 'session.view', 'session.control', 'device.view')
AND NOT EXISTS (
    SELECT 1 FROM RolePermissions rp 
    WHERE rp.RoleId = '00000000-0000-0000-0000-000000000002' 
    AND rp.PermissionId = p.Id
)

-- Viewer gets view permissions only
INSERT INTO RolePermissions (RoleId, PermissionId, GrantedBy)
SELECT 
    '00000000-0000-0000-0000-000000000003',
    p.Id,
    '00000000-0000-0000-0000-000000000001'
FROM Permissions p
WHERE p.Name IN ('session.view', 'device.view')
AND NOT EXISTS (
    SELECT 1 FROM RolePermissions rp 
    WHERE rp.RoleId = '00000000-0000-0000-0000-000000000003' 
    AND rp.PermissionId = p.Id
)
GO

PRINT ''
PRINT 'Database setup completed successfully!'
PRINT ''
PRINT 'Next steps:'
PRINT '1. Update connection string in appsettings.json'
PRINT '2. Run Entity Framework migrations if needed:'
PRINT '   dotnet ef migrations add InitialCreate --project src/RemoteC.Data --startup-project src/RemoteC.Api'
PRINT '   dotnet ef database update --project src/RemoteC.Data --startup-project src/RemoteC.Api'
GO
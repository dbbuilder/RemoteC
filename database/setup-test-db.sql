-- Setup script for RemoteC test database
-- This script creates a new test database and seeds it with development data

-- Create database if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'RemoteC2Db')
BEGIN
    CREATE DATABASE RemoteC2Db;
END
GO

USE RemoteC2Db;
GO

-- Run the initial schema creation
:r initial-schema.sql

-- Seed development data

-- Development Organization (if Organizations table exists)
IF OBJECT_ID('dbo.Organizations', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT * FROM Organizations WHERE Id = '00000000-0000-0000-0000-000000000001')
    BEGIN
        INSERT INTO Organizations (Id, Name, IsActive, CreatedAt)
        VALUES ('00000000-0000-0000-0000-000000000001', 'Development Organization', 1, GETUTCDATE());
    END
END

-- Development User
IF NOT EXISTS (SELECT * FROM Users WHERE Id = '11111111-1111-1111-1111-111111111111')
BEGIN
    INSERT INTO Users (Id, Email, FirstName, LastName, IsActive, CreatedAt, OrganizationId)
    VALUES (
        '11111111-1111-1111-1111-111111111111', 
        'dev@remotec.test', 
        'Development', 
        'User', 
        1, 
        GETUTCDATE(),
        CASE WHEN OBJECT_ID('dbo.Organizations', 'U') IS NOT NULL 
             THEN '00000000-0000-0000-0000-000000000001' 
             ELSE NULL 
        END
    );
END

-- Test Device
IF NOT EXISTS (SELECT * FROM Devices WHERE Id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa')
BEGIN
    INSERT INTO Devices (Id, Name, CreatedBy, CreatedAt, IsOnline, LastSeenAt, OrganizationId)
    VALUES (
        'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 
        'Test Device 001', 
        '11111111-1111-1111-1111-111111111111',
        GETUTCDATE(),
        1,
        GETUTCDATE(),
        CASE WHEN OBJECT_ID('dbo.Organizations', 'U') IS NOT NULL 
             THEN '00000000-0000-0000-0000-000000000001' 
             ELSE NULL 
        END
    );
END

-- Additional test devices
IF NOT EXISTS (SELECT * FROM Devices WHERE Id = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb')
BEGIN
    INSERT INTO Devices (Id, Name, CreatedBy, CreatedAt, IsOnline, LastSeenAt, HostName, IpAddress, OperatingSystem, OrganizationId)
    VALUES (
        'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 
        'Test Device 002', 
        '11111111-1111-1111-1111-111111111111',
        GETUTCDATE(),
        0,
        DATEADD(HOUR, -1, GETUTCDATE()),
        'TESTPC002',
        '192.168.1.102',
        'Windows 11',
        CASE WHEN OBJECT_ID('dbo.Organizations', 'U') IS NOT NULL 
             THEN '00000000-0000-0000-0000-000000000001' 
             ELSE NULL 
        END
    );
END

-- System Roles
IF NOT EXISTS (SELECT * FROM Roles WHERE Name = 'Administrator')
BEGIN
    INSERT INTO Roles (Id, Name, Description, IsActive, IsSystem, CreatedAt)
    VALUES (
        NEWID(), 
        'Administrator', 
        'Full system access', 
        1, 
        1, 
        GETUTCDATE()
    );
END

IF NOT EXISTS (SELECT * FROM Roles WHERE Name = 'User')
BEGIN
    INSERT INTO Roles (Id, Name, Description, IsActive, IsSystem, CreatedAt)
    VALUES (
        NEWID(), 
        'User', 
        'Standard user access', 
        1, 
        1, 
        GETUTCDATE()
    );
END

-- Assign admin role to development user
DECLARE @AdminRoleId UNIQUEIDENTIFIER = (SELECT Id FROM Roles WHERE Name = 'Administrator');
IF @AdminRoleId IS NOT NULL AND NOT EXISTS (SELECT * FROM UserRoles WHERE UserId = '11111111-1111-1111-1111-111111111111' AND RoleId = @AdminRoleId)
BEGIN
    INSERT INTO UserRoles (UserId, RoleId, AssignedBy, AssignedAt)
    VALUES (
        '11111111-1111-1111-1111-111111111111',
        @AdminRoleId,
        '11111111-1111-1111-1111-111111111111',
        GETUTCDATE()
    );
END

-- Basic Permissions
IF NOT EXISTS (SELECT * FROM Permissions WHERE Name = 'session.create')
BEGIN
    INSERT INTO Permissions (Id, Name, Description, Resource, Action, IsActive, CreatedAt)
    VALUES (NEWID(), 'session.create', 'Create remote sessions', 'session', 'create', 1, GETUTCDATE());
END

IF NOT EXISTS (SELECT * FROM Permissions WHERE Name = 'session.start')
BEGIN
    INSERT INTO Permissions (Id, Name, Description, Resource, Action, IsActive, CreatedAt)
    VALUES (NEWID(), 'session.start', 'Start remote sessions', 'session', 'start', 1, GETUTCDATE());
END

IF NOT EXISTS (SELECT * FROM Permissions WHERE Name = 'session.stop')
BEGIN
    INSERT INTO Permissions (Id, Name, Description, Resource, Action, IsActive, CreatedAt)
    VALUES (NEWID(), 'session.stop', 'Stop remote sessions', 'session', 'stop', 1, GETUTCDATE());
END

IF NOT EXISTS (SELECT * FROM Permissions WHERE Name = 'device.view')
BEGIN
    INSERT INTO Permissions (Id, Name, Description, Resource, Action, IsActive, CreatedAt)
    VALUES (NEWID(), 'device.view', 'View devices', 'device', 'view', 1, GETUTCDATE());
END

IF NOT EXISTS (SELECT * FROM Permissions WHERE Name = 'device.control')
BEGIN
    INSERT INTO Permissions (Id, Name, Description, Resource, Action, IsActive, CreatedAt)
    VALUES (NEWID(), 'device.control', 'Control devices', 'device', 'control', 1, GETUTCDATE());
END

-- Grant all permissions to Administrator role
INSERT INTO RolePermissions (RoleId, PermissionId, GrantedBy, GrantedAt)
SELECT 
    r.Id,
    p.Id,
    '11111111-1111-1111-1111-111111111111',
    GETUTCDATE()
FROM Roles r
CROSS JOIN Permissions p
WHERE r.Name = 'Administrator'
AND NOT EXISTS (
    SELECT 1 FROM RolePermissions rp 
    WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
);

PRINT 'Test database setup completed successfully!';
PRINT 'Development user: dev@remotec.test (ID: 11111111-1111-1111-1111-111111111111)';
PRINT 'Test device: Test Device 001 (ID: aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa)';
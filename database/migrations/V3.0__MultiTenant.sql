-- RemoteC Multi-Tenant Schema Migration
-- Version 3.0 - Enterprise Multi-Tenant Support

-- Organizations table
CREATE TABLE Organizations (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(255) NOT NULL,
    Subdomain NVARCHAR(100) UNIQUE NOT NULL,
    TenantId UNIQUEIDENTIFIER UNIQUE NOT NULL DEFAULT NEWID(),
    PlanType NVARCHAR(50) NOT NULL DEFAULT 'Standard',
    MaxUsers INT NOT NULL DEFAULT 100,
    MaxDevices INT NOT NULL DEFAULT 500,
    MaxConcurrentSessions INT NOT NULL DEFAULT 50,
    StorageQuotaGB INT NOT NULL DEFAULT 100,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT CHK_Organization_PlanType CHECK (PlanType IN ('Free', 'Standard', 'Professional', 'Enterprise'))
);

-- Organization settings
CREATE TABLE OrganizationSettings (
    OrganizationId UNIQUEIDENTIFIER PRIMARY KEY,
    RequireMFA BIT NOT NULL DEFAULT 0,
    SessionRecordingEnabled BIT NOT NULL DEFAULT 0,
    SessionRecordingDays INT NOT NULL DEFAULT 30,
    AllowPinAccess BIT NOT NULL DEFAULT 1,
    RequireApproval BIT NOT NULL DEFAULT 0,
    IdleTimeoutMinutes INT NOT NULL DEFAULT 30,
    MaxSessionDurationMinutes INT NOT NULL DEFAULT 480,
    IpWhitelist NVARCHAR(MAX),
    CustomBranding NVARCHAR(MAX),
    WebhookUrl NVARCHAR(500),
    SsoEnabled BIT NOT NULL DEFAULT 0,
    SsoConfig NVARCHAR(MAX),
    CONSTRAINT FK_OrgSettings_Organization FOREIGN KEY (OrganizationId) REFERENCES Organizations(Id)
);

-- Update existing tables to support multi-tenancy
ALTER TABLE Users ADD OrganizationId UNIQUEIDENTIFIER NULL;
ALTER TABLE Users ADD IsSuperAdmin BIT NOT NULL DEFAULT 0;
ALTER TABLE Users ADD Department NVARCHAR(100);
ALTER TABLE Users ADD EmployeeId NVARCHAR(50);

ALTER TABLE Devices ADD OrganizationId UNIQUEIDENTIFIER NULL;
ALTER TABLE Sessions ADD OrganizationId UNIQUEIDENTIFIER NULL;
ALTER TABLE AuditLogs ADD OrganizationId UNIQUEIDENTIFIER NULL;

-- Create indexes for tenant filtering
CREATE INDEX IX_Users_OrganizationId ON Users(OrganizationId);
CREATE INDEX IX_Devices_OrganizationId ON Devices(OrganizationId);
CREATE INDEX IX_Sessions_OrganizationId ON Sessions(OrganizationId);
CREATE INDEX IX_AuditLogs_OrganizationId ON AuditLogs(OrganizationId);

-- Organization invitations
CREATE TABLE OrganizationInvitations (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    OrganizationId UNIQUEIDENTIFIER NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    RoleId UNIQUEIDENTIFIER NOT NULL,
    InvitedBy UNIQUEIDENTIFIER NOT NULL,
    InvitationToken NVARCHAR(255) UNIQUE NOT NULL,
    ExpiresAt DATETIME2 NOT NULL,
    AcceptedAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_OrgInvite_Organization FOREIGN KEY (OrganizationId) REFERENCES Organizations(Id),
    CONSTRAINT FK_OrgInvite_Role FOREIGN KEY (RoleId) REFERENCES Roles(Id),
    CONSTRAINT FK_OrgInvite_User FOREIGN KEY (InvitedBy) REFERENCES Users(Id)
);

-- Organization API keys
CREATE TABLE OrganizationApiKeys (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    OrganizationId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    KeyHash NVARCHAR(255) NOT NULL,
    Permissions NVARCHAR(MAX) NOT NULL,
    LastUsedAt DATETIME2 NULL,
    ExpiresAt DATETIME2 NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedBy UNIQUEIDENTIFIER NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_ApiKey_Organization FOREIGN KEY (OrganizationId) REFERENCES Organizations(Id),
    CONSTRAINT FK_ApiKey_User FOREIGN KEY (CreatedBy) REFERENCES Users(Id)
);

-- Device groups for organization
CREATE TABLE DeviceGroups (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    OrganizationId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(1000),
    ParentGroupId UNIQUEIDENTIFIER NULL,
    CreatedBy UNIQUEIDENTIFIER NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_DeviceGroup_Organization FOREIGN KEY (OrganizationId) REFERENCES Organizations(Id),
    CONSTRAINT FK_DeviceGroup_Parent FOREIGN KEY (ParentGroupId) REFERENCES DeviceGroups(Id),
    CONSTRAINT FK_DeviceGroup_User FOREIGN KEY (CreatedBy) REFERENCES Users(Id)
);

-- Device group assignments
CREATE TABLE DeviceGroupAssignments (
    DeviceId UNIQUEIDENTIFIER NOT NULL,
    GroupId UNIQUEIDENTIFIER NOT NULL,
    AssignedBy UNIQUEIDENTIFIER NOT NULL,
    AssignedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_DeviceGroupAssignment PRIMARY KEY (DeviceId, GroupId),
    CONSTRAINT FK_DeviceGroupAssign_Device FOREIGN KEY (DeviceId) REFERENCES Devices(Id),
    CONSTRAINT FK_DeviceGroupAssign_Group FOREIGN KEY (GroupId) REFERENCES DeviceGroups(Id),
    CONSTRAINT FK_DeviceGroupAssign_User FOREIGN KEY (AssignedBy) REFERENCES Users(Id)
);

-- Organization usage tracking
CREATE TABLE OrganizationUsage (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    OrganizationId UNIQUEIDENTIFIER NOT NULL,
    UsageDate DATE NOT NULL,
    ActiveUsers INT NOT NULL DEFAULT 0,
    ActiveDevices INT NOT NULL DEFAULT 0,
    SessionMinutes BIGINT NOT NULL DEFAULT 0,
    DataTransferredGB DECIMAL(10,2) NOT NULL DEFAULT 0,
    StorageUsedGB DECIMAL(10,2) NOT NULL DEFAULT 0,
    RecordedSessionsGB DECIMAL(10,2) NOT NULL DEFAULT 0,
    ApiCalls BIGINT NOT NULL DEFAULT 0,
    CONSTRAINT FK_OrgUsage_Organization FOREIGN KEY (OrganizationId) REFERENCES Organizations(Id),
    CONSTRAINT UQ_OrgUsage_Date UNIQUE (OrganizationId, UsageDate)
);

-- Enhanced audit log for compliance
ALTER TABLE AuditLogs ADD IpAddress NVARCHAR(50);
ALTER TABLE AuditLogs ADD UserAgent NVARCHAR(500);
ALTER TABLE AuditLogs ADD SessionId UNIQUEIDENTIFIER NULL;
ALTER TABLE AuditLogs ADD ResourceType NVARCHAR(50);
ALTER TABLE AuditLogs ADD ResourceId NVARCHAR(255);
ALTER TABLE AuditLogs ADD OldValue NVARCHAR(MAX);
ALTER TABLE AuditLogs ADD NewValue NVARCHAR(MAX);

-- Create views for tenant isolation
GO

CREATE VIEW vw_TenantUsers AS
SELECT u.*
FROM Users u
WHERE u.OrganizationId = CAST(SESSION_CONTEXT(N'TenantId') AS UNIQUEIDENTIFIER)
   OR u.IsSuperAdmin = 1;

GO

CREATE VIEW vw_TenantDevices AS
SELECT d.*
FROM Devices d
WHERE d.OrganizationId = CAST(SESSION_CONTEXT(N'TenantId') AS UNIQUEIDENTIFIER);

GO

CREATE VIEW vw_TenantSessions AS
SELECT s.*
FROM Sessions s
WHERE s.OrganizationId = CAST(SESSION_CONTEXT(N'TenantId') AS UNIQUEIDENTIFIER);

GO

-- Row-level security policies
CREATE SCHEMA rls;
GO

CREATE FUNCTION rls.fn_TenantAccessPredicate(@TenantId UNIQUEIDENTIFIER)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN SELECT 1 AS access_result
WHERE @TenantId = CAST(SESSION_CONTEXT(N'TenantId') AS UNIQUEIDENTIFIER)
   OR IS_ROLEMEMBER('db_owner') = 1;

GO

-- Apply RLS to tables
CREATE SECURITY POLICY TenantSecurityPolicy
ADD FILTER PREDICATE rls.fn_TenantAccessPredicate(OrganizationId) ON dbo.Users,
ADD FILTER PREDICATE rls.fn_TenantAccessPredicate(OrganizationId) ON dbo.Devices,
ADD FILTER PREDICATE rls.fn_TenantAccessPredicate(OrganizationId) ON dbo.Sessions,
ADD FILTER PREDICATE rls.fn_TenantAccessPredicate(OrganizationId) ON dbo.AuditLogs
WITH (STATE = ON);

GO

-- Stored procedures for multi-tenant operations
CREATE PROCEDURE sp_CreateOrganization
    @Name NVARCHAR(255),
    @Subdomain NVARCHAR(100),
    @PlanType NVARCHAR(50) = 'Standard',
    @AdminEmail NVARCHAR(255),
    @AdminName NVARCHAR(255),
    @OrganizationId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    
    BEGIN TRY
        -- Create organization
        SET @OrganizationId = NEWID();
        
        INSERT INTO Organizations (Id, Name, Subdomain, PlanType)
        VALUES (@OrganizationId, @Name, @Subdomain, @PlanType);
        
        -- Create default settings
        INSERT INTO OrganizationSettings (OrganizationId)
        VALUES (@OrganizationId);
        
        -- Create admin user
        DECLARE @AdminId UNIQUEIDENTIFIER = NEWID();
        INSERT INTO Users (Id, Email, FullName, OrganizationId, IsActive)
        VALUES (@AdminId, @AdminEmail, @AdminName, @OrganizationId, 1);
        
        -- Assign admin role
        DECLARE @AdminRoleId UNIQUEIDENTIFIER;
        SELECT @AdminRoleId = Id FROM Roles WHERE Name = 'OrganizationAdmin';
        
        INSERT INTO UserRoles (UserId, RoleId)
        VALUES (@AdminId, @AdminRoleId);
        
        -- Create default device group
        INSERT INTO DeviceGroups (OrganizationId, Name, CreatedBy)
        VALUES (@OrganizationId, 'All Devices', @AdminId);
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;

GO

-- Procedure to get organization usage summary
CREATE PROCEDURE sp_GetOrganizationUsageSummary
    @OrganizationId UNIQUEIDENTIFIER,
    @StartDate DATE,
    @EndDate DATE
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        SUM(ActiveUsers) as TotalActiveUsers,
        SUM(ActiveDevices) as TotalActiveDevices,
        SUM(SessionMinutes) as TotalSessionMinutes,
        SUM(DataTransferredGB) as TotalDataTransferredGB,
        AVG(StorageUsedGB) as AvgStorageUsedGB,
        SUM(ApiCalls) as TotalApiCalls,
        COUNT(DISTINCT UsageDate) as DaysActive
    FROM OrganizationUsage
    WHERE OrganizationId = @OrganizationId
      AND UsageDate BETWEEN @StartDate AND @EndDate;
END;

GO

-- Add sample data for testing
INSERT INTO Organizations (Name, Subdomain, PlanType) 
VALUES ('Acme Corporation', 'acme', 'Enterprise');

INSERT INTO Organizations (Name, Subdomain, PlanType) 
VALUES ('TechStart Inc', 'techstart', 'Standard');
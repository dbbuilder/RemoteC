-- Initial database schema creation
-- This will be replaced by Entity Framework migrations

--USE RemoteC2Db
GO

-- Create Users table
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Email NVARCHAR(255) NOT NULL UNIQUE,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    AzureAdB2CId NVARCHAR(255) NULL UNIQUE,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastLoginAt DATETIME2 NULL
)
GO

-- Create Roles table
CREATE TABLE Roles (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(500) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
)
GO

-- Create Permissions table
CREATE TABLE Permissions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(500) NULL,
    Category NVARCHAR(100) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
)
GO

-- Create UserRoles junction table
CREATE TABLE UserRoles (
    UserId UNIQUEIDENTIFIER NOT NULL,
    RoleId UNIQUEIDENTIFIER NOT NULL,
    AssignedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    AssignedBy UNIQUEIDENTIFIER NOT NULL,
    PRIMARY KEY (UserId, RoleId),
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE
)
GO

-- Create RolePermissions junction table
CREATE TABLE RolePermissions (
    RoleId UNIQUEIDENTIFIER NOT NULL,
    PermissionId UNIQUEIDENTIFIER NOT NULL,
    GrantedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    GrantedBy UNIQUEIDENTIFIER NOT NULL,
    PRIMARY KEY (RoleId, PermissionId),
    FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE,
    FOREIGN KEY (PermissionId) REFERENCES Permissions(Id) ON DELETE CASCADE
)
GO

-- Create Devices table
CREATE TABLE Devices (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(255) NOT NULL,
    HostName NVARCHAR(255) NULL,
    IpAddress NVARCHAR(45) NULL,
    MacAddress NVARCHAR(17) NULL,
    OperatingSystem NVARCHAR(100) NULL,
    Version NVARCHAR(50) NULL,
    IsOnline BIT NOT NULL DEFAULT 0,
    LastSeenAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy UNIQUEIDENTIFIER NOT NULL,
    FOREIGN KEY (CreatedBy) REFERENCES Users(Id)
)
GO

-- Create Sessions table
CREATE TABLE Sessions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(255) NOT NULL,
    DeviceId UNIQUEIDENTIFIER NOT NULL,
    CreatedBy UNIQUEIDENTIFIER NOT NULL,
    Status INT NOT NULL DEFAULT 0, -- SessionStatus enum
    Type INT NOT NULL DEFAULT 0,   -- SessionType enum
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    StartedAt DATETIME2 NULL,
    EndedAt DATETIME2 NULL,
    ConnectionInfo NVARCHAR(MAX) NULL,
    RequirePin BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (DeviceId) REFERENCES Devices(Id),
    FOREIGN KEY (CreatedBy) REFERENCES Users(Id)
)
GO

-- Create SessionParticipants table
CREATE TABLE SessionParticipants (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    SessionId UNIQUEIDENTIFIER NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    Role INT NOT NULL DEFAULT 0, -- ParticipantRole enum
    IsConnected BIT NOT NULL DEFAULT 0,
    JoinedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LeftAt DATETIME2 NULL,
    FOREIGN KEY (SessionId) REFERENCES Sessions(Id) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
)
GO

-- Create AuditLogs table
CREATE TABLE AuditLogs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Action NVARCHAR(100) NOT NULL,
    EntityType NVARCHAR(100) NULL,
    EntityId NVARCHAR(100) NULL,
    UserId NVARCHAR(255) NULL,
    IpAddress NVARCHAR(45) NULL,
    UserAgent NVARCHAR(1000) NULL,
    OldValues NVARCHAR(MAX) NULL,
    NewValues NVARCHAR(MAX) NULL,
    Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    Success BIT NOT NULL DEFAULT 1,
    ErrorMessage NVARCHAR(MAX) NULL
)
GO

-- Create DeviceGroups table
CREATE TABLE DeviceGroups (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy UNIQUEIDENTIFIER NOT NULL,
    FOREIGN KEY (CreatedBy) REFERENCES Users(Id)
)
GO

-- Create DeviceGroupMembers junction table
CREATE TABLE DeviceGroupMembers (
    DeviceGroupId UNIQUEIDENTIFIER NOT NULL,
    DeviceId UNIQUEIDENTIFIER NOT NULL,
    AddedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    AddedBy UNIQUEIDENTIFIER NOT NULL,
    PRIMARY KEY (DeviceGroupId, DeviceId),
    FOREIGN KEY (DeviceGroupId) REFERENCES DeviceGroups(Id) ON DELETE CASCADE,
    FOREIGN KEY (DeviceId) REFERENCES Devices(Id) ON DELETE CASCADE,
    FOREIGN KEY (AddedBy) REFERENCES Users(Id)
)
GO

-- Create SessionLogs table
CREATE TABLE SessionLogs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    SessionId UNIQUEIDENTIFIER NOT NULL,
    EventType NVARCHAR(100) NOT NULL,
    EventData NVARCHAR(MAX) NULL,
    UserId UNIQUEIDENTIFIER NULL,
    Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (SessionId) REFERENCES Sessions(Id) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
)
GO

-- Create SessionPins table
CREATE TABLE SessionPins (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    SessionId UNIQUEIDENTIFIER NOT NULL,
    Pin NVARCHAR(10) NOT NULL,
    ExpiresAt DATETIME2 NOT NULL,
    UsedAt DATETIME2 NULL,
    UsedByIp NVARCHAR(45) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (SessionId) REFERENCES Sessions(Id) ON DELETE CASCADE
)
GO

-- Create FileTransfers table
CREATE TABLE FileTransfers (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    SessionId UNIQUEIDENTIFIER NOT NULL,
    FileName NVARCHAR(500) NOT NULL,
    FileSize BIGINT NOT NULL,
    TransferDirection INT NOT NULL, -- 0=Upload, 1=Download
    Status INT NOT NULL DEFAULT 0, -- 0=Pending, 1=InProgress, 2=Completed, 3=Failed
    StartedAt DATETIME2 NULL,
    CompletedAt DATETIME2 NULL,
    TransferredBytes BIGINT NOT NULL DEFAULT 0,
    ErrorMessage NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy UNIQUEIDENTIFIER NOT NULL,
    FOREIGN KEY (SessionId) REFERENCES Sessions(Id) ON DELETE CASCADE,
    FOREIGN KEY (CreatedBy) REFERENCES Users(Id)
)
GO

-- Create indexes for performance
CREATE INDEX IX_Users_Email ON Users(Email)
CREATE INDEX IX_Users_AzureAdB2CId ON Users(AzureAdB2CId) WHERE AzureAdB2CId IS NOT NULL
CREATE INDEX IX_Devices_MacAddress ON Devices(MacAddress) WHERE MacAddress IS NOT NULL
CREATE INDEX IX_Devices_IsOnline ON Devices(IsOnline)
CREATE INDEX IX_Sessions_DeviceId ON Sessions(DeviceId)
CREATE INDEX IX_Sessions_CreatedBy ON Sessions(CreatedBy)
CREATE INDEX IX_Sessions_Status ON Sessions(Status)
CREATE INDEX IX_Sessions_CreatedAt ON Sessions(CreatedAt DESC)
CREATE INDEX IX_SessionParticipants_SessionId ON SessionParticipants(SessionId)
CREATE INDEX IX_SessionParticipants_UserId ON SessionParticipants(UserId)
CREATE INDEX IX_SessionLogs_SessionId ON SessionLogs(SessionId)
CREATE INDEX IX_SessionLogs_Timestamp ON SessionLogs(Timestamp DESC)
CREATE INDEX IX_SessionPins_SessionId ON SessionPins(SessionId)
CREATE INDEX IX_SessionPins_Pin ON SessionPins(Pin) WHERE UsedAt IS NULL
CREATE INDEX IX_FileTransfers_SessionId ON FileTransfers(SessionId)
CREATE INDEX IX_FileTransfers_Status ON FileTransfers(Status)
CREATE INDEX IX_AuditLogs_Timestamp ON AuditLogs(Timestamp DESC)
CREATE INDEX IX_AuditLogs_UserId ON AuditLogs(UserId) WHERE UserId IS NOT NULL
CREATE INDEX IX_AuditLogs_Action ON AuditLogs(Action)
CREATE INDEX IX_AuditLogs_EntityType_EntityId ON AuditLogs(EntityType, EntityId) WHERE EntityType IS NOT NULL
GO

PRINT 'Database schema created successfully!'
GO
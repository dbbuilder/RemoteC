-- Stored Procedures for RemoteC Application

--USE RemoteC2Db
GO

-- =============================================
-- Session Management Procedures
-- =============================================

-- Get user sessions
CREATE OR ALTER PROCEDURE sp_GetUserSessions
    @UserId UNIQUEIDENTIFIER,
    @PageNumber INT = 1,
    @PageSize INT = 25
AS
BEGIN
    SET NOCOUNT ON
    
    -- Calculate offset for pagination
    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize
    
    SELECT 
        s.Id,
        s.Name,
        s.Status,
        s.Type,
        s.CreatedAt,
        s.StartedAt,
        s.EndedAt,
        d.Name AS DeviceName,
        d.HostName,
        u.FirstName + ' ' + u.LastName AS CreatedByName,
        COUNT(*) OVER() AS TotalCount
    FROM Sessions s
    INNER JOIN Devices d ON s.DeviceId = d.Id
    INNER JOIN Users u ON s.CreatedBy = u.Id
    LEFT JOIN SessionParticipants sp ON s.Id = sp.SessionId
    WHERE s.CreatedBy = @UserId 
       OR sp.UserId = @UserId
    ORDER BY s.CreatedAt DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY
END
GO

-- Get session details
CREATE OR ALTER PROCEDURE sp_GetSessionDetails
    @SessionId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON
    
    -- Check if user has access to session
    IF @UserId IS NOT NULL AND NOT EXISTS (
        SELECT 1 FROM Sessions s
        LEFT JOIN SessionParticipants sp ON s.Id = sp.SessionId
        WHERE s.Id = @SessionId 
        AND (s.CreatedBy = @UserId OR sp.UserId = @UserId)
    )
    BEGIN
        RAISERROR('Access denied to session', 16, 1)
        RETURN
    END
    
    -- Return session details
    SELECT 
        s.Id,
        s.Name,
        s.Status,
        s.Type,
        s.CreatedAt,
        s.StartedAt,
        s.EndedAt,
        s.ConnectionInfo,
        s.RequirePin,
        d.Id AS DeviceId,
        d.Name AS DeviceName,
        d.HostName,
        d.IpAddress,
        d.OperatingSystem,
        u.Id AS CreatedById,
        u.FirstName + ' ' + u.LastName AS CreatedByName
    FROM Sessions s
    INNER JOIN Devices d ON s.DeviceId = d.Id
    INNER JOIN Users u ON s.CreatedBy = u.Id
    WHERE s.Id = @SessionId
    
    -- Return participants
    SELECT 
        sp.Id,
        sp.Role,
        sp.IsConnected,
        sp.JoinedAt,
        sp.LeftAt,
        u.Id AS UserId,
        u.FirstName + ' ' + u.LastName AS UserName,
        u.Email
    FROM SessionParticipants sp
    INNER JOIN Users u ON sp.UserId = u.Id
    WHERE sp.SessionId = @SessionId
    ORDER BY sp.JoinedAt
END
GO

-- Create new session
CREATE OR ALTER PROCEDURE sp_CreateSession
    @SessionId UNIQUEIDENTIFIER OUTPUT,
    @Name NVARCHAR(255),
    @DeviceId UNIQUEIDENTIFIER,
    @CreatedBy UNIQUEIDENTIFIER,
    @Type INT = 0,
    @RequirePin BIT = 1
AS
BEGIN
    SET NOCOUNT ON
    BEGIN TRANSACTION
    
    TRY
        -- Generate new session ID
        SET @SessionId = NEWID()
        
        -- Validate device exists
        IF NOT EXISTS (SELECT 1 FROM Devices WHERE Id = @DeviceId)
        BEGIN
            RAISERROR('Device not found', 16, 1)
            RETURN
        END
        
        -- Create session
        INSERT INTO Sessions (Id, Name, DeviceId, CreatedBy, Type, RequirePin, Status)
        VALUES (@SessionId, @Name, @DeviceId, @CreatedBy, @Type, @RequirePin, 0) -- Created status
        
        -- Add creator as owner participant
        INSERT INTO SessionParticipants (SessionId, UserId, Role)
        VALUES (@SessionId, @CreatedBy, 3) -- Owner role
        
        COMMIT TRANSACTION
        
        -- Return session details
        EXEC sp_GetSessionDetails @SessionId, @CreatedBy
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        THROW
    END CATCH
END
GO

-- Update session status
CREATE OR ALTER PROCEDURE sp_UpdateSessionStatus
    @SessionId UNIQUEIDENTIFIER,
    @Status INT,
    @UserId UNIQUEIDENTIFIER = NULL,
    @ConnectionInfo NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON
    
    -- Check permissions if user specified
    IF @UserId IS NOT NULL AND NOT EXISTS (
        SELECT 1 FROM Sessions s
        LEFT JOIN SessionParticipants sp ON s.Id = sp.SessionId
        WHERE s.Id = @SessionId 
        AND (s.CreatedBy = @UserId OR (sp.UserId = @UserId AND sp.Role IN (2, 3))) -- Admin or Owner
    )
    BEGIN
        RAISERROR('Access denied to update session', 16, 1)
        RETURN
    END
    
    UPDATE Sessions 
    SET Status = @Status,
        ConnectionInfo = ISNULL(@ConnectionInfo, ConnectionInfo),
        StartedAt = CASE WHEN @Status = 3 AND StartedAt IS NULL THEN GETUTCDATE() ELSE StartedAt END, -- Connected
        EndedAt = CASE WHEN @Status = 7 THEN GETUTCDATE() ELSE NULL END -- Ended
    WHERE Id = @SessionId
    
    IF @@ROWCOUNT = 0
    BEGIN
        RAISERROR('Session not found', 16, 1)
        RETURN
    END
    
    PRINT 'Session status updated successfully'
END
GO

-- =============================================
-- User Management Procedures
-- =============================================

-- Get user by ID
CREATE OR ALTER PROCEDURE sp_GetUser
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON
    
    SELECT 
        u.Id,
        u.Email,
        u.FirstName,
        u.LastName,
        u.IsActive,
        u.CreatedAt,
        u.LastLoginAt
    FROM Users u
    WHERE u.Id = @UserId
    
    -- Get user roles
    SELECT 
        r.Id,
        r.Name,
        r.Description
    FROM UserRoles ur
    INNER JOIN Roles r ON ur.RoleId = r.Id
    WHERE ur.UserId = @UserId AND r.IsActive = 1
    
    -- Get user permissions
    SELECT DISTINCT
        p.Name,
        p.Description,
        p.Category
    FROM UserRoles ur
    INNER JOIN RolePermissions rp ON ur.RoleId = rp.RoleId
    INNER JOIN Permissions p ON rp.PermissionId = p.Id
    WHERE ur.UserId = @UserId AND p.IsActive = 1
END
GO

-- Create or update user
CREATE OR ALTER PROCEDURE sp_UpsertUser
    @UserId UNIQUEIDENTIFIER OUTPUT,
    @Email NVARCHAR(255),
    @FirstName NVARCHAR(100),
    @LastName NVARCHAR(100),
    @AzureAdB2CId NVARCHAR(255) = NULL,
    @IsActive BIT = 1
AS
BEGIN
    SET NOCOUNT ON
    BEGIN TRANSACTION
    
    TRY
        -- Check if user exists by email or Azure ID
        SELECT @UserId = Id 
        FROM Users 
        WHERE Email = @Email 
           OR (@AzureAdB2CId IS NOT NULL AND AzureAdB2CId = @AzureAdB2CId)
        
        IF @UserId IS NULL
        BEGIN
            -- Create new user
            SET @UserId = NEWID()
            INSERT INTO Users (Id, Email, FirstName, LastName, AzureAdB2CId, IsActive)
            VALUES (@UserId, @Email, @FirstName, @LastName, @AzureAdB2CId, @IsActive)
            
            -- Assign default role (Viewer)
            DECLARE @ViewerRoleId UNIQUEIDENTIFIER
            SELECT @ViewerRoleId = Id FROM Roles WHERE Name = 'Viewer'
            
            IF @ViewerRoleId IS NOT NULL
            BEGIN
                INSERT INTO UserRoles (UserId, RoleId, AssignedBy)
                VALUES (@UserId, @ViewerRoleId, @UserId)
            END
        END
        ELSE
        BEGIN
            -- Update existing user
            UPDATE Users 
            SET FirstName = @FirstName,
                LastName = @LastName,
                AzureAdB2CId = ISNULL(@AzureAdB2CId, AzureAdB2CId),
                IsActive = @IsActive,
                LastLoginAt = CASE WHEN @IsActive = 1 THEN GETUTCDATE() ELSE LastLoginAt END
            WHERE Id = @UserId
        END
        
        COMMIT TRANSACTION
        
        -- Return user details
        EXEC sp_GetUser @UserId
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        THROW
    END CATCH
END
GO

-- =============================================
-- Audit and Logging Procedures
-- =============================================

-- Insert audit log
CREATE OR ALTER PROCEDURE sp_InsertAuditLog
    @Action NVARCHAR(100),
    @EntityType NVARCHAR(100) = NULL,
    @EntityId NVARCHAR(100) = NULL,
    @UserId NVARCHAR(255) = NULL,
    @IpAddress NVARCHAR(45) = NULL,
    @UserAgent NVARCHAR(1000) = NULL,
    @OldValues NVARCHAR(MAX) = NULL,
    @NewValues NVARCHAR(MAX) = NULL,
    @Success BIT = 1,
    @ErrorMessage NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON
    
    INSERT INTO AuditLogs (
        Action, EntityType, EntityId, UserId, IpAddress, UserAgent,
        OldValues, NewValues, Success, ErrorMessage
    )
    VALUES (
        @Action, @EntityType, @EntityId, @UserId, @IpAddress, @UserAgent,
        @OldValues, @NewValues, @Success, @ErrorMessage
    )
    
    PRINT 'Audit log entry created'
END
GO

-- Get audit logs with filtering
CREATE OR ALTER PROCEDURE sp_GetAuditLogs
    @FromDate DATETIME2 = NULL,
    @ToDate DATETIME2 = NULL,
    @UserId NVARCHAR(255) = NULL,
    @Action NVARCHAR(100) = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 50
AS
BEGIN
    SET NOCOUNT ON
    
    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize
    
    SELECT 
        Id,
        Action,
        EntityType,
        EntityId,
        UserId,
        IpAddress,
        Timestamp,
        Success,
        ErrorMessage,
        COUNT(*) OVER() AS TotalCount
    FROM AuditLogs
    WHERE (@FromDate IS NULL OR Timestamp >= @FromDate)
      AND (@ToDate IS NULL OR Timestamp <= @ToDate)
      AND (@UserId IS NULL OR UserId = @UserId)
      AND (@Action IS NULL OR Action LIKE '%' + @Action + '%')
    ORDER BY Timestamp DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY
END
GO

-- =============================================
-- Performance and Maintenance Procedures
-- =============================================

-- Cleanup old audit logs
CREATE OR ALTER PROCEDURE sp_CleanupAuditLogs
    @RetentionDays INT = 90
AS
BEGIN
    SET NOCOUNT ON
    
    DECLARE @CutoffDate DATETIME2 = DATEADD(DAY, -@RetentionDays, GETUTCDATE())
    DECLARE @DeletedCount INT
    
    DELETE FROM AuditLogs 
    WHERE Timestamp < @CutoffDate
    
    SET @DeletedCount = @@ROWCOUNT
    
    PRINT 'Deleted ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' audit log entries older than ' + 
          CAST(@RetentionDays AS NVARCHAR(10)) + ' days'
END
GO

-- Update database statistics
CREATE OR ALTER PROCEDURE sp_UpdateStatistics
AS
BEGIN
    SET NOCOUNT ON
    
    UPDATE STATISTICS Users
    UPDATE STATISTICS Sessions
    UPDATE STATISTICS SessionParticipants
    UPDATE STATISTICS Devices
    UPDATE STATISTICS AuditLogs
    
    PRINT 'Database statistics updated'
END
GO

PRINT 'All stored procedures created successfully!'
GO
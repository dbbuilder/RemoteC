-- Device Management Stored Procedures for RemoteC

USE RemoteCDb
GO

-- =============================================
-- Device Management Procedures
-- =============================================

-- Get user devices
CREATE OR ALTER PROCEDURE sp_GetUserDevices
    @UserId UNIQUEIDENTIFIER,
    @PageNumber INT = 1,
    @PageSize INT = 25,
    @OnlineOnly BIT = 0
AS
BEGIN
    SET NOCOUNT ON
    
    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize
    
    SELECT 
        d.Id,
        d.Name,
        d.HostName,
        d.IpAddress,
        d.MacAddress,
        d.OperatingSystem,
        d.Version,
        d.IsOnline,
        d.LastSeenAt,
        d.CreatedAt,
        u.FirstName + ' ' + u.LastName AS CreatedByName,
        COUNT(*) OVER() AS TotalCount
    FROM Devices d
    INNER JOIN Users u ON d.CreatedBy = u.Id
    WHERE d.CreatedBy = @UserId
      AND (@OnlineOnly = 0 OR d.IsOnline = 1)
    ORDER BY d.Name
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY
END
GO

-- Get device details
CREATE OR ALTER PROCEDURE sp_GetDeviceDetails
    @DeviceId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON
    
    -- Check if user has access to device
    IF @UserId IS NOT NULL AND NOT EXISTS (
        SELECT 1 FROM Devices 
        WHERE Id = @DeviceId AND CreatedBy = @UserId
    )
    BEGIN
        RAISERROR('Access denied to device', 16, 1)
        RETURN
    END
    
    -- Return device details
    SELECT 
        d.Id,
        d.Name,
        d.HostName,
        d.IpAddress,
        d.MacAddress,
        d.OperatingSystem,
        d.Version,
        d.IsOnline,
        d.LastSeenAt,
        d.CreatedAt,
        u.Id AS CreatedById,
        u.FirstName + ' ' + u.LastName AS CreatedByName
    FROM Devices d
    INNER JOIN Users u ON d.CreatedBy = u.Id
    WHERE d.Id = @DeviceId
    
    -- Return device groups
    SELECT 
        dg.Id,
        dg.Name,
        dg.Description,
        dgm.AddedAt
    FROM DeviceGroupMembers dgm
    INNER JOIN DeviceGroups dg ON dgm.DeviceGroupId = dg.Id
    WHERE dgm.DeviceId = @DeviceId
    ORDER BY dg.Name
END
GO

-- Create or update device
CREATE OR ALTER PROCEDURE sp_UpsertDevice
    @DeviceId UNIQUEIDENTIFIER OUTPUT,
    @Name NVARCHAR(255),
    @MacAddress NVARCHAR(17),
    @CreatedBy UNIQUEIDENTIFIER,
    @HostName NVARCHAR(255) = NULL,
    @IpAddress NVARCHAR(45) = NULL,
    @OperatingSystem NVARCHAR(100) = NULL,
    @Version NVARCHAR(50) = NULL,
    @IsOnline BIT = 1
AS
BEGIN
    SET NOCOUNT ON
    BEGIN TRANSACTION
    
    TRY
        -- Check if device exists by MAC address
        SELECT @DeviceId = Id 
        FROM Devices 
        WHERE MacAddress = @MacAddress
        
        IF @DeviceId IS NULL
        BEGIN
            -- Create new device
            SET @DeviceId = NEWID()
            INSERT INTO Devices (
                Id, Name, HostName, IpAddress, MacAddress, 
                OperatingSystem, Version, IsOnline, LastSeenAt, CreatedBy
            )
            VALUES (
                @DeviceId, @Name, @HostName, @IpAddress, @MacAddress,
                @OperatingSystem, @Version, @IsOnline, GETUTCDATE(), @CreatedBy
            )
        END
        ELSE
        BEGIN
            -- Update existing device
            UPDATE Devices 
            SET Name = @Name,
                HostName = ISNULL(@HostName, HostName),
                IpAddress = ISNULL(@IpAddress, IpAddress),
                OperatingSystem = ISNULL(@OperatingSystem, OperatingSystem),
                Version = ISNULL(@Version, Version),
                IsOnline = @IsOnline,
                LastSeenAt = GETUTCDATE()
            WHERE Id = @DeviceId
        END
        
        COMMIT TRANSACTION
        
        -- Return device details
        EXEC sp_GetDeviceDetails @DeviceId, @CreatedBy
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        THROW
    END CATCH
END
GO

-- Update device status
CREATE OR ALTER PROCEDURE sp_UpdateDeviceStatus
    @DeviceId UNIQUEIDENTIFIER,
    @IsOnline BIT,
    @IpAddress NVARCHAR(45) = NULL
AS
BEGIN
    SET NOCOUNT ON
    
    UPDATE Devices 
    SET IsOnline = @IsOnline,
        LastSeenAt = GETUTCDATE(),
        IpAddress = ISNULL(@IpAddress, IpAddress)
    WHERE Id = @DeviceId
    
    IF @@ROWCOUNT = 0
    BEGIN
        RAISERROR('Device not found', 16, 1)
        RETURN
    END
    
    PRINT 'Device status updated successfully'
END
GO

-- =============================================
-- Device Group Procedures
-- =============================================

-- Create device group
CREATE OR ALTER PROCEDURE sp_CreateDeviceGroup
    @GroupId UNIQUEIDENTIFIER OUTPUT,
    @Name NVARCHAR(255),
    @Description NVARCHAR(500) = NULL,
    @CreatedBy UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON
    
    SET @GroupId = NEWID()
    
    INSERT INTO DeviceGroups (Id, Name, Description, CreatedBy)
    VALUES (@GroupId, @Name, @Description, @CreatedBy)
    
    SELECT 
        Id,
        Name,
        Description,
        CreatedAt,
        CreatedBy
    FROM DeviceGroups
    WHERE Id = @GroupId
END
GO

-- Add device to group
CREATE OR ALTER PROCEDURE sp_AddDeviceToGroup
    @DeviceGroupId UNIQUEIDENTIFIER,
    @DeviceId UNIQUEIDENTIFIER,
    @AddedBy UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON
    
    -- Check if already exists
    IF EXISTS (
        SELECT 1 FROM DeviceGroupMembers 
        WHERE DeviceGroupId = @DeviceGroupId AND DeviceId = @DeviceId
    )
    BEGIN
        PRINT 'Device already in group'
        RETURN
    END
    
    INSERT INTO DeviceGroupMembers (DeviceGroupId, DeviceId, AddedBy)
    VALUES (@DeviceGroupId, @DeviceId, @AddedBy)
    
    PRINT 'Device added to group successfully'
END
GO

-- Remove device from group
CREATE OR ALTER PROCEDURE sp_RemoveDeviceFromGroup
    @DeviceGroupId UNIQUEIDENTIFIER,
    @DeviceId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON
    
    DELETE FROM DeviceGroupMembers
    WHERE DeviceGroupId = @DeviceGroupId AND DeviceId = @DeviceId
    
    IF @@ROWCOUNT = 0
    BEGIN
        RAISERROR('Device not found in group', 16, 1)
        RETURN
    END
    
    PRINT 'Device removed from group successfully'
END
GO

-- Get devices in group
CREATE OR ALTER PROCEDURE sp_GetDevicesInGroup
    @DeviceGroupId UNIQUEIDENTIFIER,
    @PageNumber INT = 1,
    @PageSize INT = 25
AS
BEGIN
    SET NOCOUNT ON
    
    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize
    
    SELECT 
        d.Id,
        d.Name,
        d.HostName,
        d.IpAddress,
        d.MacAddress,
        d.OperatingSystem,
        d.IsOnline,
        d.LastSeenAt,
        dgm.AddedAt,
        u.FirstName + ' ' + u.LastName AS AddedByName,
        COUNT(*) OVER() AS TotalCount
    FROM DeviceGroupMembers dgm
    INNER JOIN Devices d ON dgm.DeviceId = d.Id
    INNER JOIN Users u ON dgm.AddedBy = u.Id
    WHERE dgm.DeviceGroupId = @DeviceGroupId
    ORDER BY d.Name
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY
END
GO

PRINT 'Device procedures created successfully!'
GO
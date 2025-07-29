-- PIN Management Stored Procedures for RemoteC

USE RemoteCDb
GO

-- =============================================
-- PIN Management Procedures
-- =============================================

-- Generate session PIN
CREATE OR ALTER PROCEDURE sp_GenerateSessionPin
    @SessionId UNIQUEIDENTIFIER,
    @Pin NVARCHAR(10),
    @ExpirationMinutes INT = 10
AS
BEGIN
    SET NOCOUNT ON
    BEGIN TRANSACTION
    
    TRY
        -- Check if session exists and is valid
        IF NOT EXISTS (
            SELECT 1 FROM Sessions 
            WHERE Id = @SessionId 
            AND Status IN (0, 1, 2) -- Created, Approved, Rejected
        )
        BEGIN
            RAISERROR('Session not found or invalid status', 16, 1)
            RETURN
        END
        
        -- Expire any existing unused PINs for this session
        UPDATE SessionPins
        SET UsedAt = GETUTCDATE()
        WHERE SessionId = @SessionId 
          AND UsedAt IS NULL
          AND ExpiresAt > GETUTCDATE()
        
        -- Generate new PIN
        DECLARE @ExpiresAt DATETIME2 = DATEADD(MINUTE, @ExpirationMinutes, GETUTCDATE())
        
        INSERT INTO SessionPins (SessionId, Pin, ExpiresAt)
        VALUES (@SessionId, @Pin, @ExpiresAt)
        
        -- Update session status to WaitingForPin
        UPDATE Sessions
        SET Status = 1 -- WaitingForPin
        WHERE Id = @SessionId
        
        COMMIT TRANSACTION
        
        -- Return PIN details
        SELECT 
            Pin,
            ExpiresAt,
            @ExpirationMinutes AS ExpirationMinutes
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        THROW
    END CATCH
END
GO

-- Validate session PIN
CREATE OR ALTER PROCEDURE sp_ValidateSessionPin
    @SessionId UNIQUEIDENTIFIER,
    @Pin NVARCHAR(10),
    @IpAddress NVARCHAR(45) = NULL,
    @IsValid BIT OUTPUT,
    @Message NVARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON
    
    SET @IsValid = 0
    SET @Message = 'Invalid PIN'
    
    -- Check if PIN exists and is valid
    DECLARE @PinId UNIQUEIDENTIFIER
    
    SELECT TOP 1 @PinId = Id
    FROM SessionPins
    WHERE SessionId = @SessionId
      AND Pin = @Pin
      AND UsedAt IS NULL
      AND ExpiresAt > GETUTCDATE()
    ORDER BY CreatedAt DESC
    
    IF @PinId IS NOT NULL
    BEGIN
        -- Mark PIN as used
        UPDATE SessionPins
        SET UsedAt = GETUTCDATE(),
            UsedByIp = @IpAddress
        WHERE Id = @PinId
        
        -- Update session status
        UPDATE Sessions
        SET Status = 2 -- Connecting
        WHERE Id = @SessionId
        
        SET @IsValid = 1
        SET @Message = 'PIN validated successfully'
    END
    ELSE
    BEGIN
        -- Check if PIN expired
        IF EXISTS (
            SELECT 1 FROM SessionPins
            WHERE SessionId = @SessionId
              AND Pin = @Pin
              AND ExpiresAt <= GETUTCDATE()
        )
        BEGIN
            SET @Message = 'PIN has expired'
        END
        -- Check if PIN was already used
        ELSE IF EXISTS (
            SELECT 1 FROM SessionPins
            WHERE SessionId = @SessionId
              AND Pin = @Pin
              AND UsedAt IS NOT NULL
        )
        BEGIN
            SET @Message = 'PIN has already been used'
        END
    END
    
    -- Log the attempt
    EXEC sp_InsertAuditLog 
        @Action = 'ValidateSessionPin',
        @EntityType = 'Session',
        @EntityId = @SessionId,
        @IpAddress = @IpAddress,
        @Success = @IsValid,
        @ErrorMessage = @Message
END
GO

-- Get active PINs for session
CREATE OR ALTER PROCEDURE sp_GetSessionPins
    @SessionId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON
    
    SELECT 
        Id,
        Pin,
        ExpiresAt,
        CASE 
            WHEN UsedAt IS NOT NULL THEN 'Used'
            WHEN ExpiresAt <= GETUTCDATE() THEN 'Expired'
            ELSE 'Active'
        END AS Status,
        UsedAt,
        UsedByIp,
        CreatedAt
    FROM SessionPins
    WHERE SessionId = @SessionId
    ORDER BY CreatedAt DESC
END
GO

-- Cleanup expired PINs
CREATE OR ALTER PROCEDURE sp_CleanupExpiredPins
    @RetentionDays INT = 7
AS
BEGIN
    SET NOCOUNT ON
    
    DECLARE @CutoffDate DATETIME2 = DATEADD(DAY, -@RetentionDays, GETUTCDATE())
    DECLARE @DeletedCount INT
    
    DELETE FROM SessionPins 
    WHERE CreatedAt < @CutoffDate
      OR (ExpiresAt < @CutoffDate AND UsedAt IS NULL)
    
    SET @DeletedCount = @@ROWCOUNT
    
    PRINT 'Deleted ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' expired PIN entries'
END
GO

-- Resend PIN notification
CREATE OR ALTER PROCEDURE sp_ResendSessionPin
    @SessionId UNIQUEIDENTIFIER,
    @LastPinId UNIQUEIDENTIFIER OUTPUT,
    @Pin NVARCHAR(10) OUTPUT,
    @ExpiresAt DATETIME2 OUTPUT
AS
BEGIN
    SET NOCOUNT ON
    
    -- Get the latest active PIN
    SELECT TOP 1 
        @LastPinId = Id,
        @Pin = Pin,
        @ExpiresAt = ExpiresAt
    FROM SessionPins
    WHERE SessionId = @SessionId
      AND UsedAt IS NULL
      AND ExpiresAt > GETUTCDATE()
    ORDER BY CreatedAt DESC
    
    IF @LastPinId IS NULL
    BEGIN
        RAISERROR('No active PIN found for session', 16, 1)
        RETURN
    END
    
    -- Log the resend
    EXEC sp_InsertAuditLog 
        @Action = 'ResendSessionPin',
        @EntityType = 'Session',
        @EntityId = @SessionId,
        @Success = 1
END
GO

PRINT 'PIN procedures created successfully!'
GO
-- File Transfer Stored Procedures for RemoteC

USE RemoteCDb
GO

-- =============================================
-- File Transfer Management Procedures
-- =============================================

-- Create file transfer record
CREATE OR ALTER PROCEDURE sp_CreateFileTransfer
    @TransferId UNIQUEIDENTIFIER OUTPUT,
    @SessionId UNIQUEIDENTIFIER,
    @FileName NVARCHAR(500),
    @FileSize BIGINT,
    @TransferDirection INT, -- 0=Upload, 1=Download
    @CreatedBy UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON
    
    -- Validate session exists and is active
    IF NOT EXISTS (
        SELECT 1 FROM Sessions 
        WHERE Id = @SessionId 
        AND Status IN (3, 4) -- Connected, Active
    )
    BEGIN
        RAISERROR('Session not found or not active', 16, 1)
        RETURN
    END
    
    -- Check user has access to session
    IF NOT EXISTS (
        SELECT 1 FROM Sessions s
        LEFT JOIN SessionParticipants sp ON s.Id = sp.SessionId
        WHERE s.Id = @SessionId 
        AND (s.CreatedBy = @CreatedBy OR sp.UserId = @CreatedBy)
    )
    BEGIN
        RAISERROR('Access denied to session', 16, 1)
        RETURN
    END
    
    SET @TransferId = NEWID()
    
    INSERT INTO FileTransfers (
        Id, SessionId, FileName, FileSize, 
        TransferDirection, Status, CreatedBy
    )
    VALUES (
        @TransferId, @SessionId, @FileName, @FileSize,
        @TransferDirection, 0, @CreatedBy -- Status=Pending
    )
    
    -- Return transfer details
    SELECT 
        Id,
        SessionId,
        FileName,
        FileSize,
        TransferDirection,
        Status,
        CreatedAt
    FROM FileTransfers
    WHERE Id = @TransferId
END
GO

-- Update file transfer progress
CREATE OR ALTER PROCEDURE sp_UpdateFileTransferProgress
    @TransferId UNIQUEIDENTIFIER,
    @TransferredBytes BIGINT,
    @Status INT = NULL -- NULL means keep current status
AS
BEGIN
    SET NOCOUNT ON
    
    DECLARE @CurrentStatus INT
    SELECT @CurrentStatus = Status FROM FileTransfers WHERE Id = @TransferId
    
    IF @CurrentStatus IS NULL
    BEGIN
        RAISERROR('File transfer not found', 16, 1)
        RETURN
    END
    
    -- Don't update if already completed or failed
    IF @CurrentStatus IN (2, 3) -- Completed, Failed
    BEGIN
        PRINT 'Transfer already completed or failed'
        RETURN
    END
    
    UPDATE FileTransfers
    SET TransferredBytes = @TransferredBytes,
        Status = ISNULL(@Status, 
            CASE 
                WHEN @CurrentStatus = 0 THEN 1 -- Pending -> InProgress
                ELSE @CurrentStatus 
            END),
        StartedAt = CASE 
            WHEN @CurrentStatus = 0 AND StartedAt IS NULL 
            THEN GETUTCDATE() 
            ELSE StartedAt 
        END
    WHERE Id = @TransferId
    
    -- Return current progress percentage
    SELECT 
        TransferredBytes,
        FileSize,
        CAST(ROUND((CAST(TransferredBytes AS FLOAT) / CAST(FileSize AS FLOAT)) * 100, 2) AS DECIMAL(5,2)) AS ProgressPercent
    FROM FileTransfers
    WHERE Id = @TransferId
END
GO

-- Complete file transfer
CREATE OR ALTER PROCEDURE sp_CompleteFileTransfer
    @TransferId UNIQUEIDENTIFIER,
    @Success BIT,
    @ErrorMessage NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON
    
    UPDATE FileTransfers
    SET Status = CASE WHEN @Success = 1 THEN 2 ELSE 3 END, -- Completed or Failed
        CompletedAt = GETUTCDATE(),
        ErrorMessage = CASE WHEN @Success = 0 THEN @ErrorMessage ELSE NULL END
    WHERE Id = @TransferId
    
    IF @@ROWCOUNT = 0
    BEGIN
        RAISERROR('File transfer not found', 16, 1)
        RETURN
    END
    
    -- Log completion
    DECLARE @SessionId UNIQUEIDENTIFIER, @FileName NVARCHAR(500)
    SELECT @SessionId = SessionId, @FileName = FileName 
    FROM FileTransfers WHERE Id = @TransferId
    
    EXEC sp_InsertAuditLog 
        @Action = 'FileTransferComplete',
        @EntityType = 'FileTransfer',
        @EntityId = @TransferId,
        @Success = @Success,
        @ErrorMessage = @ErrorMessage,
        @NewValues = @FileName
END
GO

-- Get session file transfers
CREATE OR ALTER PROCEDURE sp_GetSessionFileTransfers
    @SessionId UNIQUEIDENTIFIER,
    @PageNumber INT = 1,
    @PageSize INT = 25
AS
BEGIN
    SET NOCOUNT ON
    
    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize
    
    SELECT 
        ft.Id,
        ft.FileName,
        ft.FileSize,
        ft.TransferDirection,
        ft.Status,
        ft.TransferredBytes,
        ft.StartedAt,
        ft.CompletedAt,
        ft.ErrorMessage,
        ft.CreatedAt,
        u.FirstName + ' ' + u.LastName AS CreatedByName,
        CASE 
            WHEN ft.Status = 1 AND ft.FileSize > 0 THEN 
                CAST(ROUND((CAST(ft.TransferredBytes AS FLOAT) / CAST(ft.FileSize AS FLOAT)) * 100, 2) AS DECIMAL(5,2))
            ELSE 0
        END AS ProgressPercent,
        COUNT(*) OVER() AS TotalCount
    FROM FileTransfers ft
    INNER JOIN Users u ON ft.CreatedBy = u.Id
    WHERE ft.SessionId = @SessionId
    ORDER BY ft.CreatedAt DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY
END
GO

-- Get active transfers for monitoring
CREATE OR ALTER PROCEDURE sp_GetActiveTransfers
    @UserId UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON
    
    SELECT 
        ft.Id,
        ft.SessionId,
        ft.FileName,
        ft.FileSize,
        ft.TransferDirection,
        ft.Status,
        ft.TransferredBytes,
        ft.StartedAt,
        s.Name AS SessionName,
        u.FirstName + ' ' + u.LastName AS CreatedByName,
        CASE 
            WHEN ft.FileSize > 0 THEN 
                CAST(ROUND((CAST(ft.TransferredBytes AS FLOAT) / CAST(ft.FileSize AS FLOAT)) * 100, 2) AS DECIMAL(5,2))
            ELSE 0
        END AS ProgressPercent,
        CASE 
            WHEN ft.StartedAt IS NOT NULL AND ft.TransferredBytes > 0 THEN
                CAST(ft.TransferredBytes AS FLOAT) / DATEDIFF(SECOND, ft.StartedAt, GETUTCDATE())
            ELSE 0
        END AS BytesPerSecond
    FROM FileTransfers ft
    INNER JOIN Sessions s ON ft.SessionId = s.Id
    INNER JOIN Users u ON ft.CreatedBy = u.Id
    WHERE ft.Status IN (0, 1) -- Pending, InProgress
      AND (@UserId IS NULL OR ft.CreatedBy = @UserId 
           OR EXISTS (
               SELECT 1 FROM SessionParticipants sp 
               WHERE sp.SessionId = ft.SessionId AND sp.UserId = @UserId
           ))
    ORDER BY ft.StartedAt DESC, ft.CreatedAt DESC
END
GO

-- Cleanup old file transfer records
CREATE OR ALTER PROCEDURE sp_CleanupFileTransfers
    @RetentionDays INT = 30
AS
BEGIN
    SET NOCOUNT ON
    
    DECLARE @CutoffDate DATETIME2 = DATEADD(DAY, -@RetentionDays, GETUTCDATE())
    DECLARE @DeletedCount INT
    
    DELETE FROM FileTransfers 
    WHERE CreatedAt < @CutoffDate
      AND Status IN (2, 3) -- Only delete completed or failed transfers
    
    SET @DeletedCount = @@ROWCOUNT
    
    PRINT 'Deleted ' + CAST(@DeletedCount AS NVARCHAR(10)) + ' old file transfer records'
END
GO

PRINT 'File transfer procedures created successfully!'
GO
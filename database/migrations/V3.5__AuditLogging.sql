-- =============================================
-- Migration: V3.5__AuditLogging.sql
-- Description: Add comprehensive audit logging
-- Author: RemoteC Team
-- Date: 2025-01-29
-- =============================================

-- Create AuditLogs table
CREATE TABLE dbo.AuditLogs (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    Timestamp DATETIME2(7) NOT NULL,
    OrganizationId UNIQUEIDENTIFIER NOT NULL,
    UserId UNIQUEIDENTIFIER NULL,
    UserName NVARCHAR(256) NULL,
    UserEmail NVARCHAR(256) NULL,
    IpAddress NVARCHAR(45) NULL, -- Support IPv6
    UserAgent NVARCHAR(512) NULL,
    Action NVARCHAR(100) NOT NULL,
    ResourceType NVARCHAR(100) NOT NULL,
    ResourceId NVARCHAR(100) NULL,
    ResourceName NVARCHAR(256) NULL,
    Severity INT NOT NULL, -- 0=Debug, 1=Info, 2=Warning, 3=Error, 4=Critical
    Category INT NOT NULL, -- 0=General, 1=Auth, 2=AuthZ, 3=DataAccess, etc.
    Details NVARCHAR(MAX) NULL,
    Metadata NVARCHAR(MAX) NULL, -- JSON data
    CorrelationId NVARCHAR(100) NULL,
    Duration BIGINT NULL, -- In ticks
    Success BIT NOT NULL DEFAULT 1,
    ErrorMessage NVARCHAR(MAX) NULL,
    StackTrace NVARCHAR(MAX) NULL,
    IndexTimestamp AS CAST(Timestamp AS DATE) PERSISTED,
    IndexAction AS Action PERSISTED,
    IndexResource AS ResourceType + ':' + ISNULL(ResourceId, '') PERSISTED,
    CONSTRAINT PK_AuditLogs PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_AuditLogs_Organizations FOREIGN KEY (OrganizationId) 
        REFERENCES dbo.Organizations(Id),
    CONSTRAINT FK_AuditLogs_Users FOREIGN KEY (UserId) 
        REFERENCES dbo.Users(Id)
);

-- Create indexes for performance
CREATE NONCLUSTERED INDEX IX_AuditLogs_Organization_Timestamp 
    ON dbo.AuditLogs(OrganizationId, Timestamp DESC)
    INCLUDE (Action, ResourceType, UserId);

CREATE NONCLUSTERED INDEX IX_AuditLogs_User_Timestamp 
    ON dbo.AuditLogs(UserId, Timestamp DESC)
    WHERE UserId IS NOT NULL;

CREATE NONCLUSTERED INDEX IX_AuditLogs_Resource
    ON dbo.AuditLogs(ResourceType, ResourceId, Timestamp DESC)
    WHERE ResourceId IS NOT NULL;

CREATE NONCLUSTERED INDEX IX_AuditLogs_IndexTimestamp
    ON dbo.AuditLogs(IndexTimestamp, OrganizationId)
    INCLUDE (Severity, Category);

CREATE NONCLUSTERED INDEX IX_AuditLogs_Severity_Category
    ON dbo.AuditLogs(Severity, Category, Timestamp DESC)
    WHERE Severity >= 2; -- Warning and above

CREATE NONCLUSTERED INDEX IX_AuditLogs_CorrelationId
    ON dbo.AuditLogs(CorrelationId, Timestamp)
    WHERE CorrelationId IS NOT NULL;

-- Create partitioned view for better query performance
GO
CREATE VIEW dbo.vw_RecentAuditLogs
AS
SELECT 
    Id,
    Timestamp,
    OrganizationId,
    UserId,
    UserName,
    Action,
    ResourceType,
    ResourceId,
    Severity,
    Category,
    Success,
    Details
FROM dbo.AuditLogs
WHERE Timestamp >= DATEADD(DAY, -7, GETUTCDATE());
GO

-- Create audit summary table for statistics
CREATE TABLE dbo.AuditSummaries (
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    OrganizationId UNIQUEIDENTIFIER NOT NULL,
    SummaryDate DATE NOT NULL,
    TotalEvents INT NOT NULL DEFAULT 0,
    FailedEvents INT NOT NULL DEFAULT 0,
    UniqueUsers INT NOT NULL DEFAULT 0,
    EventsByAction NVARCHAR(MAX) NULL, -- JSON
    EventsByCategory NVARCHAR(MAX) NULL, -- JSON
    EventsBySeverity NVARCHAR(MAX) NULL, -- JSON
    CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_AuditSummaries PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_AuditSummaries_OrgDate UNIQUE (OrganizationId, SummaryDate),
    CONSTRAINT FK_AuditSummaries_Organizations FOREIGN KEY (OrganizationId)
        REFERENCES dbo.Organizations(Id)
);

CREATE NONCLUSTERED INDEX IX_AuditSummaries_OrgDate
    ON dbo.AuditSummaries(OrganizationId, SummaryDate DESC);

-- Stored procedures for audit logging
GO
CREATE PROCEDURE dbo.sp_AuditLog_Insert
    @Id UNIQUEIDENTIFIER,
    @Timestamp DATETIME2(7),
    @OrganizationId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER = NULL,
    @UserName NVARCHAR(256) = NULL,
    @UserEmail NVARCHAR(256) = NULL,
    @IpAddress NVARCHAR(45) = NULL,
    @UserAgent NVARCHAR(512) = NULL,
    @Action NVARCHAR(100),
    @ResourceType NVARCHAR(100),
    @ResourceId NVARCHAR(100) = NULL,
    @ResourceName NVARCHAR(256) = NULL,
    @Severity INT,
    @Category INT,
    @Details NVARCHAR(MAX) = NULL,
    @Metadata NVARCHAR(MAX) = NULL,
    @CorrelationId NVARCHAR(100) = NULL,
    @Duration BIGINT = NULL,
    @Success BIT = 1,
    @ErrorMessage NVARCHAR(MAX) = NULL,
    @StackTrace NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO dbo.AuditLogs (
        Id, Timestamp, OrganizationId, UserId, UserName, UserEmail,
        IpAddress, UserAgent, Action, ResourceType, ResourceId, ResourceName,
        Severity, Category, Details, Metadata, CorrelationId, Duration,
        Success, ErrorMessage, StackTrace
    ) VALUES (
        @Id, @Timestamp, @OrganizationId, @UserId, @UserName, @UserEmail,
        @IpAddress, @UserAgent, @Action, @ResourceType, @ResourceId, @ResourceName,
        @Severity, @Category, @Details, @Metadata, @CorrelationId, @Duration,
        @Success, @ErrorMessage, @StackTrace
    );
END;
GO

-- Batch insert for performance
CREATE PROCEDURE dbo.sp_AuditLog_BatchInsert
    @AuditLogs dbo.AuditLogTableType READONLY
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO dbo.AuditLogs (
        Id, Timestamp, OrganizationId, UserId, UserName, UserEmail,
        IpAddress, UserAgent, Action, ResourceType, ResourceId, ResourceName,
        Severity, Category, Details, Metadata, CorrelationId, Duration,
        Success, ErrorMessage, StackTrace
    )
    SELECT 
        Id, Timestamp, OrganizationId, UserId, UserName, UserEmail,
        IpAddress, UserAgent, Action, ResourceType, ResourceId, ResourceName,
        Severity, Category, Details, Metadata, CorrelationId, Duration,
        Success, ErrorMessage, StackTrace
    FROM @AuditLogs;
END;
GO

-- Create table type for batch insert
CREATE TYPE dbo.AuditLogTableType AS TABLE (
    Id UNIQUEIDENTIFIER NOT NULL,
    Timestamp DATETIME2(7) NOT NULL,
    OrganizationId UNIQUEIDENTIFIER NOT NULL,
    UserId UNIQUEIDENTIFIER NULL,
    UserName NVARCHAR(256) NULL,
    UserEmail NVARCHAR(256) NULL,
    IpAddress NVARCHAR(45) NULL,
    UserAgent NVARCHAR(512) NULL,
    Action NVARCHAR(100) NOT NULL,
    ResourceType NVARCHAR(100) NOT NULL,
    ResourceId NVARCHAR(100) NULL,
    ResourceName NVARCHAR(256) NULL,
    Severity INT NOT NULL,
    Category INT NOT NULL,
    Details NVARCHAR(MAX) NULL,
    Metadata NVARCHAR(MAX) NULL,
    CorrelationId NVARCHAR(100) NULL,
    Duration BIGINT NULL,
    Success BIT NOT NULL,
    ErrorMessage NVARCHAR(MAX) NULL,
    StackTrace NVARCHAR(MAX) NULL
);
GO

-- Query audit logs with filtering
CREATE PROCEDURE dbo.sp_AuditLog_Query
    @OrganizationId UNIQUEIDENTIFIER = NULL,
    @UserId UNIQUEIDENTIFIER = NULL,
    @StartDate DATETIME2(7) = NULL,
    @EndDate DATETIME2(7) = NULL,
    @Action NVARCHAR(100) = NULL,
    @ResourceType NVARCHAR(100) = NULL,
    @ResourceId NVARCHAR(100) = NULL,
    @MinSeverity INT = NULL,
    @Category INT = NULL,
    @SearchText NVARCHAR(256) = NULL,
    @IpAddress NVARCHAR(45) = NULL,
    @SuccessOnly BIT = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 50,
    @SortBy NVARCHAR(50) = 'Timestamp',
    @SortDescending BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Calculate offset
    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;
    
    -- Build dynamic query
    DECLARE @sql NVARCHAR(MAX) = '
    WITH FilteredLogs AS (
        SELECT 
            Id, Timestamp, OrganizationId, UserId, UserName, UserEmail,
            IpAddress, UserAgent, Action, ResourceType, ResourceId, ResourceName,
            Severity, Category, Details, Metadata, CorrelationId, Duration,
            Success, ErrorMessage, StackTrace
        FROM dbo.AuditLogs
        WHERE 1=1';
    
    -- Add filters
    IF @OrganizationId IS NOT NULL
        SET @sql = @sql + ' AND OrganizationId = @OrganizationId';
    
    IF @UserId IS NOT NULL
        SET @sql = @sql + ' AND UserId = @UserId';
    
    IF @StartDate IS NOT NULL
        SET @sql = @sql + ' AND Timestamp >= @StartDate';
    
    IF @EndDate IS NOT NULL
        SET @sql = @sql + ' AND Timestamp <= @EndDate';
    
    IF @Action IS NOT NULL
        SET @sql = @sql + ' AND Action = @Action';
    
    IF @ResourceType IS NOT NULL
        SET @sql = @sql + ' AND ResourceType = @ResourceType';
    
    IF @ResourceId IS NOT NULL
        SET @sql = @sql + ' AND ResourceId = @ResourceId';
    
    IF @MinSeverity IS NOT NULL
        SET @sql = @sql + ' AND Severity >= @MinSeverity';
    
    IF @Category IS NOT NULL
        SET @sql = @sql + ' AND Category = @Category';
    
    IF @SearchText IS NOT NULL
        SET @sql = @sql + ' AND (Details LIKE ''%'' + @SearchText + ''%'' OR 
                                 UserName LIKE ''%'' + @SearchText + ''%'' OR 
                                 UserEmail LIKE ''%'' + @SearchText + ''%'' OR 
                                 ResourceName LIKE ''%'' + @SearchText + ''%'')';
    
    IF @IpAddress IS NOT NULL
        SET @sql = @sql + ' AND IpAddress = @IpAddress';
    
    IF @SuccessOnly IS NOT NULL
        SET @sql = @sql + ' AND Success = @SuccessOnly';
    
    SET @sql = @sql + ')
    SELECT 
        *,
        COUNT(*) OVER() AS TotalCount
    FROM FilteredLogs
    ORDER BY ' + 
    CASE @SortBy
        WHEN 'Timestamp' THEN 'Timestamp'
        WHEN 'User' THEN 'UserName'
        WHEN 'Action' THEN 'Action'
        WHEN 'Severity' THEN 'Severity'
        ELSE 'Timestamp'
    END +
    CASE WHEN @SortDescending = 1 THEN ' DESC' ELSE ' ASC' END + '
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;';
    
    -- Execute query
    EXEC sp_executesql @sql,
        N'@OrganizationId UNIQUEIDENTIFIER, @UserId UNIQUEIDENTIFIER, 
          @StartDate DATETIME2(7), @EndDate DATETIME2(7), 
          @Action NVARCHAR(100), @ResourceType NVARCHAR(100), 
          @ResourceId NVARCHAR(100), @MinSeverity INT, @Category INT,
          @SearchText NVARCHAR(256), @IpAddress NVARCHAR(45), 
          @SuccessOnly BIT, @Offset INT, @PageSize INT',
        @OrganizationId, @UserId, @StartDate, @EndDate, 
        @Action, @ResourceType, @ResourceId, @MinSeverity, @Category,
        @SearchText, @IpAddress, @SuccessOnly, @Offset, @PageSize;
END;
GO

-- Get audit logs by resource
CREATE PROCEDURE dbo.sp_AuditLog_GetByResource
    @ResourceType NVARCHAR(100),
    @ResourceId NVARCHAR(100),
    @Limit INT = 100
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT TOP(@Limit)
        Id, Timestamp, OrganizationId, UserId, UserName, UserEmail,
        IpAddress, UserAgent, Action, ResourceType, ResourceId, ResourceName,
        Severity, Category, Details, Metadata, CorrelationId, Duration,
        Success, ErrorMessage, StackTrace
    FROM dbo.AuditLogs
    WHERE ResourceType = @ResourceType 
      AND ResourceId = @ResourceId
    ORDER BY Timestamp DESC;
END;
GO

-- Get audit logs by user
CREATE PROCEDURE dbo.sp_AuditLog_GetByUser
    @UserId UNIQUEIDENTIFIER,
    @StartDate DATETIME2(7) = NULL,
    @EndDate DATETIME2(7) = NULL,
    @Limit INT = 100
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT TOP(@Limit)
        Id, Timestamp, OrganizationId, UserId, UserName, UserEmail,
        IpAddress, UserAgent, Action, ResourceType, ResourceId, ResourceName,
        Severity, Category, Details, Metadata, CorrelationId, Duration,
        Success, ErrorMessage, StackTrace
    FROM dbo.AuditLogs
    WHERE UserId = @UserId
      AND (@StartDate IS NULL OR Timestamp >= @StartDate)
      AND (@EndDate IS NULL OR Timestamp <= @EndDate)
    ORDER BY Timestamp DESC;
END;
GO

-- Delete old audit logs
CREATE PROCEDURE dbo.sp_AuditLog_DeleteOld
    @CutoffDate DATETIME2(7),
    @BatchSize INT = 1000
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @DeletedCount INT = 0;
    DECLARE @BatchDeleted INT;
    
    WHILE 1 = 1
    BEGIN
        DELETE TOP(@BatchSize) FROM dbo.AuditLogs
        WHERE Timestamp < @CutoffDate;
        
        SET @BatchDeleted = @@ROWCOUNT;
        SET @DeletedCount = @DeletedCount + @BatchDeleted;
        
        IF @BatchDeleted < @BatchSize
            BREAK;
        
        -- Brief pause to avoid blocking
        WAITFOR DELAY '00:00:00.100';
    END;
    
    SELECT @DeletedCount AS DeletedCount;
END;
GO

-- Generate audit statistics
CREATE PROCEDURE dbo.sp_AuditLog_GetStatistics
    @OrganizationId UNIQUEIDENTIFIER,
    @StartDate DATETIME2(7),
    @EndDate DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Total events
    DECLARE @TotalEvents INT;
    SELECT @TotalEvents = COUNT(*)
    FROM dbo.AuditLogs
    WHERE OrganizationId = @OrganizationId
      AND Timestamp BETWEEN @StartDate AND @EndDate;
    
    -- Failed events
    DECLARE @FailedEvents INT;
    SELECT @FailedEvents = COUNT(*)
    FROM dbo.AuditLogs
    WHERE OrganizationId = @OrganizationId
      AND Timestamp BETWEEN @StartDate AND @EndDate
      AND Success = 0;
    
    -- Average response time (in milliseconds)
    DECLARE @AvgResponseTime FLOAT;
    SELECT @AvgResponseTime = AVG(CAST(Duration AS FLOAT) / 10000.0)
    FROM dbo.AuditLogs
    WHERE OrganizationId = @OrganizationId
      AND Timestamp BETWEEN @StartDate AND @EndDate
      AND Duration IS NOT NULL;
    
    -- Events by action
    DECLARE @EventsByAction NVARCHAR(MAX);
    SELECT @EventsByAction = (
        SELECT Action, COUNT(*) AS Count
        FROM dbo.AuditLogs
        WHERE OrganizationId = @OrganizationId
          AND Timestamp BETWEEN @StartDate AND @EndDate
        GROUP BY Action
        ORDER BY COUNT(*) DESC
        FOR JSON AUTO
    );
    
    -- Events by category
    DECLARE @EventsByCategory NVARCHAR(MAX);
    SELECT @EventsByCategory = (
        SELECT Category, COUNT(*) AS Count
        FROM dbo.AuditLogs
        WHERE OrganizationId = @OrganizationId
          AND Timestamp BETWEEN @StartDate AND @EndDate
        GROUP BY Category
        FOR JSON AUTO
    );
    
    -- Events by severity
    DECLARE @EventsBySeverity NVARCHAR(MAX);
    SELECT @EventsBySeverity = (
        SELECT Severity, COUNT(*) AS Count
        FROM dbo.AuditLogs
        WHERE OrganizationId = @OrganizationId
          AND Timestamp BETWEEN @StartDate AND @EndDate
        GROUP BY Severity
        FOR JSON AUTO
    );
    
    -- Top users
    DECLARE @TopUsers NVARCHAR(MAX);
    SELECT @TopUsers = (
        SELECT TOP 5 UserName, COUNT(*) AS Count
        FROM dbo.AuditLogs
        WHERE OrganizationId = @OrganizationId
          AND Timestamp BETWEEN @StartDate AND @EndDate
          AND UserName IS NOT NULL
        GROUP BY UserName
        ORDER BY COUNT(*) DESC
        FOR JSON AUTO
    );
    
    -- Return results
    SELECT 
        @TotalEvents AS TotalEvents,
        @FailedEvents AS FailedEvents,
        @AvgResponseTime AS AvgResponseTime,
        @EventsByAction AS EventsByAction,
        @EventsByCategory AS EventsByCategory,
        @EventsBySeverity AS EventsBySeverity,
        @TopUsers AS TopUsers;
END;
GO

-- Create job for generating daily summaries
CREATE PROCEDURE dbo.sp_AuditLog_GenerateDailySummary
    @Date DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Use yesterday if no date provided
    IF @Date IS NULL
        SET @Date = DATEADD(DAY, -1, CAST(GETUTCDATE() AS DATE));
    
    DECLARE @StartDate DATETIME2(7) = CAST(@Date AS DATETIME2(7));
    DECLARE @EndDate DATETIME2(7) = DATEADD(DAY, 1, @StartDate);
    
    -- Generate summary for each organization
    INSERT INTO dbo.AuditSummaries (
        OrganizationId, SummaryDate, TotalEvents, FailedEvents, 
        UniqueUsers, EventsByAction, EventsByCategory, EventsBySeverity
    )
    SELECT 
        OrganizationId,
        @Date,
        COUNT(*) AS TotalEvents,
        SUM(CASE WHEN Success = 0 THEN 1 ELSE 0 END) AS FailedEvents,
        COUNT(DISTINCT UserId) AS UniqueUsers,
        (
            SELECT Action, COUNT(*) AS Count
            FROM dbo.AuditLogs a2
            WHERE a2.OrganizationId = a1.OrganizationId
              AND a2.Timestamp >= @StartDate
              AND a2.Timestamp < @EndDate
            GROUP BY Action
            FOR JSON AUTO
        ) AS EventsByAction,
        (
            SELECT Category, COUNT(*) AS Count
            FROM dbo.AuditLogs a3
            WHERE a3.OrganizationId = a1.OrganizationId
              AND a3.Timestamp >= @StartDate
              AND a3.Timestamp < @EndDate
            GROUP BY Category
            FOR JSON AUTO
        ) AS EventsByCategory,
        (
            SELECT Severity, COUNT(*) AS Count
            FROM dbo.AuditLogs a4
            WHERE a4.OrganizationId = a1.OrganizationId
              AND a4.Timestamp >= @StartDate
              AND a4.Timestamp < @EndDate
            GROUP BY Severity
            FOR JSON AUTO
        ) AS EventsBySeverity
    FROM dbo.AuditLogs a1
    WHERE Timestamp >= @StartDate AND Timestamp < @EndDate
    GROUP BY OrganizationId;
END;
GO

-- Add row-level security for audit logs
ALTER SECURITY POLICY TenantSecurityPolicy
ADD FILTER PREDICATE rls.fn_TenantAccessPredicate(OrganizationId) ON dbo.AuditLogs;
GO

-- Grant permissions
GRANT SELECT, INSERT ON dbo.AuditLogs TO [RemoteCApp];
GRANT EXECUTE ON dbo.sp_AuditLog_Insert TO [RemoteCApp];
GRANT EXECUTE ON dbo.sp_AuditLog_BatchInsert TO [RemoteCApp];
GRANT EXECUTE ON dbo.sp_AuditLog_Query TO [RemoteCApp];
GRANT EXECUTE ON dbo.sp_AuditLog_GetByResource TO [RemoteCApp];
GRANT EXECUTE ON dbo.sp_AuditLog_GetByUser TO [RemoteCApp];
GRANT EXECUTE ON dbo.sp_AuditLog_DeleteOld TO [RemoteCApp];
GRANT EXECUTE ON dbo.sp_AuditLog_GetStatistics TO [RemoteCApp];
GRANT EXECUTE ON dbo.sp_AuditLog_GenerateDailySummary TO [RemoteCApp];
GRANT SELECT ON dbo.vw_RecentAuditLogs TO [RemoteCApp];
GO
-- RemoteC Database Setup Script
-- This script creates the initial database structure for RemoteC application

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
PRINT 'Run Entity Framework migrations to create tables:'
PRINT 'dotnet ef migrations add InitialCreate --project src/RemoteC.Data --startup-project src/RemoteC.Api'
PRINT 'dotnet ef database update --project src/RemoteC.Data --startup-project src/RemoteC.Api'
GO
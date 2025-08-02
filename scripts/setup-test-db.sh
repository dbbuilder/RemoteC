#!/bin/bash

# Setup script for RemoteC test database
# This script creates and initializes a test database with development data

# Configuration
DB_SERVER="sqltest.schoolvision.net,14333"
DB_USER="sv"
DB_PASSWORD="Gv51076!"
DB_NAME="RemoteC2Db"

echo "Setting up RemoteC test database..."
echo "=================================="

# Change to database directory
cd "$(dirname "$0")/../database" || exit 1

# First, create the database if it doesn't exist
echo "Creating database $DB_NAME if it doesn't exist..."
sqlcmd -S $DB_SERVER -U $DB_USER -P $DB_PASSWORD -C -Q "IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = '$DB_NAME') CREATE DATABASE $DB_NAME"

# Run the initial schema
echo "Creating database schema..."
sqlcmd -S $DB_SERVER -U $DB_USER -P $DB_PASSWORD -C -d $DB_NAME -i initial-schema.sql

# Seed development data
echo "Seeding development data..."
sqlcmd -S $DB_SERVER -U $DB_USER -P $DB_PASSWORD -C -d $DB_NAME -Q "
-- Development User
IF NOT EXISTS (SELECT * FROM Users WHERE Id = '11111111-1111-1111-1111-111111111111')
BEGIN
    INSERT INTO Users (Id, Email, FirstName, LastName, IsActive, CreatedAt)
    VALUES ('11111111-1111-1111-1111-111111111111', 'dev@remotec.test', 'Development', 'User', 1, GETUTCDATE());
    PRINT 'Created development user';
END

-- Test Device
IF NOT EXISTS (SELECT * FROM Devices WHERE Id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa')
BEGIN
    INSERT INTO Devices (Id, Name, CreatedBy, CreatedAt, IsOnline, LastSeenAt)
    VALUES ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Test Device 001', '11111111-1111-1111-1111-111111111111', GETUTCDATE(), 1, GETUTCDATE());
    PRINT 'Created test device';
END

-- Additional test device
IF NOT EXISTS (SELECT * FROM Devices WHERE Id = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb')
BEGIN
    INSERT INTO Devices (Id, Name, CreatedBy, CreatedAt, IsOnline, LastSeenAt, HostName, IpAddress, OperatingSystem)
    VALUES ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'Test Device 002', '11111111-1111-1111-1111-111111111111', GETUTCDATE(), 0, DATEADD(HOUR, -1, GETUTCDATE()), 'TESTPC002', '192.168.1.102', 'Windows 11');
    PRINT 'Created additional test device';
END
"

echo ""
echo "Verifying setup..."
sqlcmd -S $DB_SERVER -U $DB_USER -P $DB_PASSWORD -C -d $DB_NAME -Q "SELECT COUNT(*) as UserCount FROM Users; SELECT COUNT(*) as DeviceCount FROM Devices;"

echo ""
echo "Test database setup completed!"
echo "============================="
echo "Connection string: Server=$DB_SERVER;Database=$DB_NAME;User Id=$DB_USER;Password=$DB_PASSWORD;TrustServerCertificate=true"
echo "Development user: dev@remotec.test (ID: 11111111-1111-1111-1111-111111111111)"
echo "Test device: Test Device 001 (ID: aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa)"
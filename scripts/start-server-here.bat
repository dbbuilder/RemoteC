@echo off
echo Starting RemoteC Server (D:\dev2\remotec)
echo ========================================
echo.

cd /d D:\dev2\remotec

:: Quick check if we're in the right place
if not exist "src\RemoteC.Api\RemoteC.Api.csproj" (
    echo ERROR: Cannot find RemoteC.Api project!
    echo Make sure you're in the correct directory.
    pause
    exit /b 1
)

echo Configuration:
echo - Path: D:\dev2\remotec
echo - Database: SQLite (no SQL Server needed)
echo - Cache: In-Memory (no Redis needed)
echo - URL: http://localhost:17001
echo.

:: Set environment for development
set ASPNETCORE_ENVIRONMENT=Development
set ASPNETCORE_URLS=http://localhost:17001

:: Use SQLite instead of SQL Server
set ConnectionStrings__DefaultConnection=Data Source=remotec.db

:: Use in-memory cache instead of Redis
set Cache__Provider=Memory

:: Disable authentication for testing
set Authentication__Enabled=false

echo Starting API Server...
echo.

dotnet run --project src\RemoteC.Api\RemoteC.Api.csproj

:: If it fails, show common issues
if errorlevel 1 (
    echo.
    echo Server failed to start. Common issues:
    echo 1. Missing .NET 8.0 SDK - Install from https://dotnet.microsoft.com/download/dotnet/8.0
    echo 2. Port 17001 already in use - Check with: netstat -an ^| findstr :17001
    echo 3. Build errors - Try: dotnet build src\RemoteC.Api\RemoteC.Api.csproj
    pause
)
@echo off
REM RemoteC Host - Simple Startup Script
echo Starting RemoteC Host...

REM Set environment
set ASPNETCORE_ENVIRONMENT=Development
set SERVER_URL=http://localhost:17001

REM Navigate to host directory
cd /d "%~dp0src\RemoteC.Host"

REM Check if project exists
if not exist "RemoteC.Host.csproj" (
    echo ERROR: RemoteC.Host project not found!
    echo Make sure you're running this from the RemoteC root directory.
    pause
    exit /b 1
)

REM Run the host
echo Connecting to server at %SERVER_URL%
dotnet run -- --server "%SERVER_URL%"

pause
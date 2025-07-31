@echo off
echo ==============================================
echo       RemoteC Host Startup
echo ==============================================
echo.

REM Check if running on the same machine as server or different machine
echo Where is this host running?
echo 1. Same machine as server (localhost)
echo 2. Different machine on network
echo.
set /p LOCATION="Enter choice (1 or 2): "

if "%LOCATION%"=="1" (
    set SERVER_URL=http://localhost:17001
    echo Using local server at %SERVER_URL%
) else (
    echo Enter the server IP address (e.g., 10.0.0.91):
    set /p SERVER_IP="Server IP: "
    set SERVER_URL=http://%SERVER_IP%:17001
    echo Using remote server at %SERVER_URL%
)

echo.
echo ==============================================
echo Configuration:
echo - Server URL: %SERVER_URL%
echo - Host ID: %COMPUTERNAME%
echo ==============================================
echo.

REM Set environment
set ASPNETCORE_ENVIRONMENT=Development
set REMOTEC_SERVER_URL=%SERVER_URL%
set REMOTEC_HOST_ID=%COMPUTERNAME%
set REMOTEC_HOST_SECRET=dev-secret-001

REM Navigate to host directory
cd /d "%~dp0..\src\RemoteC.Host"

REM Check if project exists
if not exist "RemoteC.Host.csproj" (
    echo ERROR: RemoteC.Host project not found!
    echo Expected location: %CD%
    echo.
    echo Please ensure:
    echo 1. You're running this from the scripts directory
    echo 2. The RemoteC.Host project exists
    pause
    exit /b 1
)

REM Check server connectivity
echo Checking connection to server...
powershell -Command "(Invoke-WebRequest -Uri '%SERVER_URL%/health' -UseBasicParsing -TimeoutSec 5).StatusCode" >nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo WARNING: Cannot reach server at %SERVER_URL%
    echo Make sure the server is running before starting the host.
    echo.
    echo Start server with: scripts\start-server-windows.bat
    echo.
    pause
)

echo.
echo Starting RemoteC Host...
echo.
echo The host will:
echo - Connect to server at %SERVER_URL%
echo - Register as: %COMPUTERNAME%
echo - Enable remote control of this machine
echo.
echo Press Ctrl+C to stop the host
echo.

REM Run the host
dotnet run -- --server "%SERVER_URL%" --id "%COMPUTERNAME%" --secret "dev-secret-001"

if %errorlevel% neq 0 (
    echo.
    echo ERROR: Failed to start host!
    echo.
    echo Possible issues:
    echo 1. .NET SDK not installed
    echo 2. Server not reachable
    echo 3. Build errors in project
    echo.
)

pause
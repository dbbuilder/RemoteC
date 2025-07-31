@echo off
echo ==============================================
echo    RemoteC Host - Remote Machine Setup
echo ==============================================
echo.
echo This script configures a host to connect to a
echo RemoteC server running on another machine.
echo.

REM Get server details
echo Enter the RemoteC server details:
set /p SERVER_IP="Server IP address (e.g., 10.0.0.91): "
set /p HOST_NAME="Name for this host (or press Enter for %COMPUTERNAME%): "

if "%HOST_NAME%"=="" set HOST_NAME=%COMPUTERNAME%

set SERVER_URL=http://%SERVER_IP%:17001

echo.
echo ==============================================
echo Configuration Summary:
echo - Server: %SERVER_URL%
echo - Host Name: %HOST_NAME%
echo - Host Secret: dev-secret-001 (development)
echo ==============================================
echo.

REM Test connection
echo Testing connection to server...
curl -s -f %SERVER_URL%/health >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: Cannot connect to server at %SERVER_URL%
    echo.
    echo Please check:
    echo 1. Server is running on %SERVER_IP%
    echo 2. Firewall allows connection to port 17001
    echo 3. You can ping %SERVER_IP%
    echo.
    echo To test manually:
    echo   ping %SERVER_IP%
    echo   curl %SERVER_URL%/health
    echo.
    pause
    exit /b 1
)

echo Connection successful!
echo.

REM Set environment variables
set ASPNETCORE_ENVIRONMENT=Development
set REMOTEC_SERVER_URL=%SERVER_URL%
set REMOTEC_HOST_ID=%HOST_NAME%
set REMOTEC_HOST_SECRET=dev-secret-001

REM Check if running from compiled host or source
if exist "%~dp0..\src\RemoteC.Host\RemoteC.Host.csproj" (
    echo Running from source code...
    cd /d "%~dp0..\src\RemoteC.Host"
    
    echo Building host application...
    dotnet build
    if %errorlevel% neq 0 (
        echo ERROR: Build failed!
        pause
        exit /b 1
    )
    
    echo.
    echo Starting host...
    dotnet run -- --server "%SERVER_URL%" --id "%HOST_NAME%" --secret "dev-secret-001"
) else if exist "%~dp0RemoteC.Host.exe" (
    echo Running compiled host...
    "%~dp0RemoteC.Host.exe" --server "%SERVER_URL%" --id "%HOST_NAME%" --secret "dev-secret-001"
) else (
    echo ERROR: Cannot find RemoteC.Host!
    echo.
    echo Please either:
    echo 1. Run this from the RemoteC project scripts folder
    echo 2. Place this script next to RemoteC.Host.exe
    echo.
    pause
    exit /b 1
)

pause
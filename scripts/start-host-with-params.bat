@echo off
echo =========================================
echo    Starting RemoteC Host with Parameters
echo =========================================
echo.

REM Set default values
set SERVER_URL=http://localhost:17001
set HOST_ID=dev-host-001
set HOST_SECRET=dev-secret-001

REM Check for command line parameters
if not "%1"=="" set SERVER_URL=%1
if not "%2"=="" set HOST_ID=%2
if not "%3"=="" set HOST_SECRET=%3

echo Configuration:
echo - Server URL: %SERVER_URL%
echo - Host ID: %HOST_ID%
echo - Host Secret: %HOST_SECRET%
echo - Token Endpoint: %SERVER_URL%/api/auth/host/token
echo.

REM Navigate to host directory
cd /d "%~dp0..\src\RemoteC.Host"

REM Run the host with command line parameters
echo Starting host with command line configuration...
dotnet run -- --server %SERVER_URL% --id %HOST_ID% --secret %HOST_SECRET% --token-endpoint %SERVER_URL%/api/auth/host/token

pause
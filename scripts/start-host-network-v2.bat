@echo off
echo ==============================================
echo       RemoteC Host Network Startup
echo ==============================================
echo.

cd /d "%~dp0\..\src\RemoteC.Host"

:: Get server IP
echo The server is running on machine with IP: 10.0.0.91
echo.
set /p SERVER_IP="Enter server IP address [10.0.0.91]: "
if "%SERVER_IP%"=="" set SERVER_IP=10.0.0.91

:: Test connection first
echo.
echo Testing connection to %SERVER_IP%:17001...
powershell -Command "Test-NetConnection -ComputerName %SERVER_IP% -Port 17001 -InformationLevel Quiet" >nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo ERROR: Cannot connect to server at %SERVER_IP%:17001
    echo Please ensure:
    echo   1. Server is running on %SERVER_IP%
    echo   2. Port 17001 is open in Windows Firewall
    echo   3. You can ping %SERVER_IP%
    echo.
    pause
    exit /b 1
)

echo Connection test successful!
echo.

:: Set environment variables for the host
set ASPNETCORE_ENVIRONMENT=Development
set Api__BaseUrl=http://%SERVER_IP%:17001
set Api__TokenEndpoint=http://%SERVER_IP%:17001/api/auth/token
set HostConfiguration__ServerUrl=http://%SERVER_IP%:17001

echo Starting RemoteC Host...
echo Connecting to server at http://%SERVER_IP%:17001
echo.

dotnet run
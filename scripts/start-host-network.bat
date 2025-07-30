@echo off
echo Starting RemoteC Host...
cd /d "%~dp0\..\src\RemoteC.Host"

echo.
set /p SERVER_IP="Enter server IP address (e.g., 192.168.1.100): "

if "%SERVER_IP%"=="" (
    echo No server IP provided, using localhost
    set SERVER_IP=localhost
)

echo.
echo Connecting to server at http://%SERVER_IP%:17001
echo.

set RemoteControl__ServerUrl=http://%SERVER_IP%:17001
dotnet run
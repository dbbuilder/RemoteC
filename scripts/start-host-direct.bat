@echo off
echo Starting RemoteC Host (Direct Mode)...
echo ==================================
echo.

cd /d C:\devlocal\remotec

:: Check if the Host executable exists
if not exist "src\RemoteC.Host\bin\Release\net8.0\RemoteC.Host.exe" (
    echo Building RemoteC Host...
    dotnet build src\RemoteC.Host\RemoteC.Host.csproj -c Release
)

:: Get server IP from user
set /p SERVER_IP=Enter RemoteC Server IP address: 

:: Create temporary config
echo Creating configuration...
(
echo {
echo   "ApiSettings": {
echo     "ApiUrl": "http://%SERVER_IP%:7001",
echo     "SignalRUrl": "http://%SERVER_IP%:7002/hubs/session"
echo   },
echo   "HostSettings": {
echo     "MachineName": "%COMPUTERNAME%",
echo     "AutoStart": true,
echo     "EnablePinAuthentication": true
echo   },
echo   "Logging": {
echo     "LogLevel": {
echo       "Default": "Information"
echo     }
echo   }
echo }
) > src\RemoteC.Host\bin\Release\net8.0\appsettings.json

echo.
echo Starting RemoteC Host...
echo Connecting to server: %SERVER_IP%
echo.

cd src\RemoteC.Host\bin\Release\net8.0
start RemoteC.Host.exe

echo.
echo RemoteC Host is starting...
echo Check the system tray for the RemoteC icon.
echo.
pause
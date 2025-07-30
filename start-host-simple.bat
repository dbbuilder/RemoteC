@echo off
REM RemoteC Host - Simple Direct Startup
echo Starting RemoteC Host...

REM Default server settings
set SERVER_IP=192.168.1.100
set SERVER_PORT=17001

REM Allow user to pass server IP as parameter
if not "%1"=="" set SERVER_IP=%1

echo Connecting to server: %SERVER_IP%:%SERVER_PORT%
echo.

REM Navigate to the built executable directory
cd /d "%~dp0src\RemoteC.Host\bin\Release\net8.0\win-x64"

REM Create a basic appsettings.json if it doesn't exist
if not exist "appsettings.json" (
    echo Creating configuration file...
    echo {> appsettings.json
    echo   "ApiSettings": {>> appsettings.json
    echo     "ApiUrl": "http://%SERVER_IP%:%SERVER_PORT%",>> appsettings.json
    echo     "SignalRUrl": "http://%SERVER_IP%:17002/hubs/session">> appsettings.json
    echo   },>> appsettings.json
    echo   "HostSettings": {>> appsettings.json
    echo     "MachineName": "%COMPUTERNAME%",>> appsettings.json
    echo     "AutoStart": true,>> appsettings.json
    echo     "EnablePinAuthentication": true>> appsettings.json
    echo   },>> appsettings.json
    echo   "Logging": {>> appsettings.json
    echo     "LogLevel": {>> appsettings.json
    echo       "Default": "Information">> appsettings.json
    echo     }>> appsettings.json
    echo   }>> appsettings.json
    echo }>> appsettings.json
)

echo Starting RemoteC Host executable...
echo.

REM Run the host executable
RemoteC.Host.exe

REM Keep the window open if there's an error
if %ERRORLEVEL% neq 0 (
    echo.
    echo Host exited with error code %ERRORLEVEL%
    pause
)
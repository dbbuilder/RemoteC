@echo off
echo ==============================================
echo    Starting RemoteC Server Stack
echo ==============================================
echo.

echo This will start:
echo   1. RemoteC API Server (port 17001)
echo   2. RemoteC Web UI (port 3000)
echo.
pause

:: Start API server in new window
start "RemoteC API Server" cmd /k "cd /d D:\dev2\remotec\src\RemoteC.Api && dotnet run --urls http://0.0.0.0:17001"

:: Wait a bit for API to start
echo Waiting for API server to start...
timeout /t 10 /nobreak >nul

:: Start Web UI in new window
start "RemoteC Web UI" cmd /k "cd /d D:\dev2\remotec\src\RemoteC.Web && npm run dev"

echo.
echo ==============================================
echo Both services are starting in separate windows
echo ==============================================
echo.
echo Access points:
echo   - API:  http://10.0.0.91:17001/swagger
echo   - UI:   http://10.0.0.91:3000
echo.
echo Close this window when done.
pause
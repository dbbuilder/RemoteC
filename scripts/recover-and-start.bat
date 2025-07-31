@echo off
echo =========================================
echo    Complete Recovery and Start Script
echo =========================================
echo.
echo This script will:
echo 1. Stop all running processes
echo 2. Fix all permission issues
echo 3. Rebuild the project
echo 4. Start servers in correct order
echo.
pause

echo.
echo Step 1: Stopping all processes...
echo =========================================

:: Kill specific locked process
taskkill /F /PID 9688 2>nul

:: Kill all RemoteC related processes
taskkill /F /IM RemoteC.Api.exe 2>nul
taskkill /F /IM dotnet.exe 2>nul
taskkill /F /IM node.exe 2>nul

:: Kill processes on specific ports
for /f "tokens=5" %%a in ('netstat -aon ^| findstr :17001') do taskkill /F /PID %%a 2>nul
for /f "tokens=5" %%a in ('netstat -aon ^| findstr :17002') do taskkill /F /PID %%a 2>nul

echo Done!
timeout /t 3 /nobreak >nul

echo.
echo Step 2: Fixing Vite permissions...
echo =========================================
cd /d D:\Dev2\remoteC\src\RemoteC.Web

if exist "node_modules\.vite" (
    echo Removing .vite directory...
    rmdir /s /q "node_modules\.vite" 2>nul
    
    :: If still exists, try with force
    if exist "node_modules\.vite" (
        echo Using force removal...
        rd /s /q "node_modules\.vite" 2>nul
    )
)

echo Done!

echo.
echo Step 3: Rebuilding the project...
echo =========================================
cd /d D:\Dev2\remoteC
dotnet clean
dotnet build

if %errorlevel% neq 0 (
    echo.
    echo Build failed! Please check errors above.
    pause
    exit /b 1
)

echo Build successful!

echo.
echo Step 4: Starting servers...
echo =========================================
echo.
echo Starting API server in new window...
start "RemoteC API" cmd /c "cd /d D:\Dev2\remoteC && scripts\start-server-windows.bat"

echo Waiting for API to start...
timeout /t 5 /nobreak >nul

echo.
echo Starting UI in new window...
start "RemoteC UI" cmd /c "cd /d D:\Dev2\remoteC && scripts\start-dev-ui.bat"

echo.
echo =========================================
echo    Recovery Complete!
echo =========================================
echo.
echo Two new windows should have opened:
echo - API Server: http://localhost:17001
echo - UI: http://localhost:17002
echo.
echo If you still have issues, try:
echo 1. Run scripts\start-ui-elevated.bat as Administrator
echo 2. Restart your computer
echo 3. Disable antivirus temporarily
echo.
pause
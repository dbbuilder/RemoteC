@echo off
echo =========================================
echo    Fixing All Lock Issues
echo =========================================
echo.

echo [1] Killing the API server that's locking files (PID 9688)...
taskkill /F /PID 9688 2>nul
if %errorlevel% equ 0 (
    echo API server stopped successfully.
) else (
    echo API server may have already stopped.
)

echo.
echo [2] Stopping any other RemoteC processes...
taskkill /F /IM RemoteC.Api.exe 2>nul
taskkill /F /IM dotnet.exe /FI "WINDOWTITLE eq RemoteC*" 2>nul

echo.
echo [3] Stopping any Node/Vite processes...
for /f "tokens=5" %%a in ('netstat -aon ^| findstr :17002 ^| findstr LISTENING') do (
    echo Killing Node process on port 17002 (PID %%a)...
    taskkill /F /PID %%a 2>nul
)
taskkill /F /IM node.exe 2>nul

echo.
echo [4] Fixing Vite permissions...
cd /d D:\Dev2\remoteC\src\RemoteC.Web

:: Take ownership and grant permissions
if exist "node_modules\.vite" (
    echo Taking ownership of .vite directory...
    takeown /f "node_modules\.vite" /r /d y >nul 2>&1
    icacls "node_modules\.vite" /grant "%USERNAME%":F /t /q >nul 2>&1
    
    echo Removing .vite directory...
    rmdir /s /q "node_modules\.vite" 2>nul
)

:: Also clean temp vite files
if exist "%TEMP%\vite" (
    rmdir /s /q "%TEMP%\vite" 2>nul
)

echo.
echo [5] Waiting for processes to fully release files...
timeout /t 3 /nobreak >nul

echo.
echo =========================================
echo    All locks cleared!
echo =========================================
echo.
echo You can now:
echo 1. Build the API: dotnet build
echo 2. Start the API: scripts\start-server-windows.bat
echo 3. Start the UI: scripts\start-dev-ui.bat
echo.
pause
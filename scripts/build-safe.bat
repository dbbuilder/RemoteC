@echo off
echo =========================================
echo      Safe Build Script for RemoteC
echo =========================================
echo.
echo This script stops all servers before building
echo to prevent file lock errors.
echo.

echo [1] Stopping all running servers...
echo.

:: Kill API server
for /f "tokens=5" %%a in ('netstat -aon ^| findstr :17001 ^| findstr LISTENING') do (
    echo Stopping API server (PID %%a)...
    taskkill /F /PID %%a 2>nul
)

:: Kill UI server
for /f "tokens=5" %%a in ('netstat -aon ^| findstr :17002 ^| findstr LISTENING') do (
    echo Stopping UI server (PID %%a)...
    taskkill /F /PID %%a 2>nul
)

:: Kill any dotnet processes
taskkill /F /IM RemoteC.Api.exe 2>nul
taskkill /F /IM dotnet.exe /FI "WINDOWTITLE eq RemoteC*" 2>nul

:: Wait a moment for processes to fully stop
timeout /t 2 /nobreak >nul

echo.
echo [2] Cleaning build outputs...
dotnet clean

echo.
echo [3] Building solution...
dotnet build

if %errorlevel% equ 0 (
    echo.
    echo =========================================
    echo    Build completed successfully!
    echo =========================================
    echo.
    echo You can now start the servers:
    echo - API: scripts\start-server-windows.bat
    echo - UI:  scripts\start-dev-ui.bat
) else (
    echo.
    echo =========================================
    echo    Build failed!
    echo =========================================
    echo.
    echo Check the error messages above.
)

echo.
pause
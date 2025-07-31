@echo off
:: Check if running as admin
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo =========================================
    echo    Requesting Administrator Privileges
    echo =========================================
    echo.
    echo This script needs admin rights to fix Vite permissions.
    echo Restarting with elevated privileges...
    echo.
    
    :: Restart script as admin
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

echo =========================================
echo    Starting UI with Admin Privileges
echo =========================================
echo.

cd /d D:\Dev2\remoteC\src\RemoteC.Web

echo [1] Fixing permissions on node_modules...
icacls node_modules /grant "%USERNAME%":F /t /q >nul 2>&1

echo [2] Removing .vite cache with force...
if exist "node_modules\.vite" (
    takeown /f "node_modules\.vite" /r /d y >nul 2>&1
    icacls "node_modules\.vite" /grant "%USERNAME%":F /t /q >nul 2>&1
    rmdir /s /q "node_modules\.vite"
)

echo [3] Setting permissions for Vite to work...
icacls . /grant "%USERNAME%":F /t /q >nul 2>&1

echo.
echo Starting development server with full permissions...
echo.
echo Once started, access the UI at:
echo - http://localhost:17002
echo.

call npm run dev

pause
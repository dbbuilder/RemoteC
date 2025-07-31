@echo off
echo ==============================================
echo     Fixing RemoteC Web UI Dependencies
echo ==============================================
echo.

cd /d D:\dev2\remotec\src\RemoteC.Web

echo Current directory: %cd%
echo.

echo Removing incomplete node_modules...
if exist node_modules (
    rmdir /s /q node_modules
    echo Removed node_modules
)

if exist package-lock.json (
    del package-lock.json
    echo Removed package-lock.json
)

echo.
echo Cleaning npm cache...
call npm cache clean --force

echo.
echo Installing fresh dependencies...
call npm install

if %errorlevel% neq 0 (
    echo.
    echo ERROR: npm install failed!
    echo.
    echo Please check:
    echo 1. Node.js is installed (run: node --version)
    echo 2. You have internet connection
    echo 3. No antivirus is blocking npm
    echo.
    pause
    exit /b 1
)

echo.
echo ===================================
echo Dependencies fixed successfully!
echo ===================================
echo.
echo Starting the UI now...
echo.

timeout /t 3 /nobreak >nul

call npm run dev

pause
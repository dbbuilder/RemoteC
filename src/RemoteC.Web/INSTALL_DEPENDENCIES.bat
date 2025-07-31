@echo off
echo Installing Web UI Dependencies...
echo ================================
echo.
echo This will install all required packages for the RemoteC Web UI.
echo.

cd /d %~dp0

echo Cleaning up old files...
if exist node_modules rmdir /s /q node_modules
if exist package-lock.json del package-lock.json

echo.
echo Installing packages (this may take a few minutes)...
call npm install

if %errorlevel% equ 0 (
    echo.
    echo SUCCESS! Dependencies installed.
    echo.
    echo You can now run the UI with: npm run dev
    echo Or use the start-ui-windows.bat script
) else (
    echo.
    echo ERROR: Installation failed!
    echo Please ensure Node.js is installed from https://nodejs.org/
)

echo.
pause
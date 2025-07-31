@echo off
echo Installing RemoteC Web UI Dependencies...
echo ========================================
echo.

cd /d D:\dev2\remotec\src\RemoteC.Web

echo Current directory: %cd%
echo.

echo Checking Node.js version...
call node --version
if %errorlevel% neq 0 (
    echo ERROR: Node.js is not installed!
    echo Please install Node.js from https://nodejs.org/
    pause
    exit /b 1
)

echo Checking npm version...
call npm --version
if %errorlevel% neq 0 (
    echo ERROR: npm is not installed!
    pause
    exit /b 1
)

echo.
echo Installing dependencies...
call npm install

if %errorlevel% neq 0 (
    echo.
    echo ERROR: Failed to install dependencies
    echo Try running: npm cache clean --force
    pause
    exit /b 1
)

echo.
echo ===================================
echo Dependencies installed successfully!
echo ===================================
echo.
echo You can now run the UI with:
echo   cd D:\dev2\remotec\src\RemoteC.Web
echo   npm run dev
echo.
pause
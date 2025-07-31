@echo off
echo ==============================================
echo   RemoteC Web UI - PRODUCTION MODE
echo ==============================================
echo.
echo IMPORTANT: This mode requires Azure AD B2C configuration!
echo If you don't have Azure AD configured, use start-dev-ui.bat instead.
echo.

cd /d "D:\dev2\remotec\src\RemoteC.Web"

echo Checking if node_modules exists...
if not exist "node_modules\vite" (
    echo Dependencies are missing or incomplete!
    echo Installing dependencies...
    call npm install
    if %errorlevel% neq 0 (
        echo.
        echo ERROR: Failed to install dependencies
        echo Please run install-web-dependencies.bat first
        pause
        exit /b 1
    )
    echo.
)

echo Building for production...
call npm run build
if %errorlevel% neq 0 (
    echo.
    echo ERROR: Build failed!
    pause
    exit /b 1
)

echo.
echo Starting RemoteC Web UI in PRODUCTION mode...
echo.
echo The UI will be accessible at:
echo   - http://localhost:17002 (this machine)
echo   - http://10.0.0.91:17002 (from network)
echo.
echo The UI connects to the API at http://localhost:17001
echo Make sure the API server is running first!
echo.
echo Press Ctrl+C to stop the UI
echo.

call npm run preview

if %errorlevel% neq 0 (
    echo.
    echo ERROR: Failed to start the UI
    echo Make sure all dependencies are installed correctly
    pause
)
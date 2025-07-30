@echo off
echo ==============================================
echo       RemoteC Web UI Startup
echo ==============================================
echo.

cd /d "D:\dev2\remotec\src\RemoteC.Web"

echo Checking if node_modules exists...
if not exist "node_modules\" (
    echo Installing dependencies...
    npm install
    echo.
)

echo Starting RemoteC Web UI...
echo.
echo The UI will be accessible at:
echo   - http://localhost:3000 (this machine)
echo   - http://10.0.0.91:3000 (from network)
echo.
echo The UI connects to the API at http://localhost:17001
echo Make sure the API server is running first!
echo.
echo Press Ctrl+C to stop the UI
echo.

npm run dev
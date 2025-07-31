@echo off
echo =========================================
echo    Fixing Host Connection Issues
echo =========================================
echo.

echo This script will help fix common host connection problems.
echo.

echo [1] Checking if API server is running...
curl -s http://localhost:17001/health >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: API server is not running!
    echo Please start it first: scripts\start-server-windows.bat
    pause
    exit /b 1
)
echo OK - API server is running

echo.
echo [2] Testing host authentication endpoint...
echo.

set HOST_ID=dev-host-001
set HOST_SECRET=dev-secret-001

echo Testing with credentials:
echo - Host ID: %HOST_ID%
echo - Secret: %HOST_SECRET%
echo.

curl -X POST http://localhost:17001/api/auth/host/token ^
  -H "Content-Type: application/json" ^
  -d "{\"hostId\":\"%HOST_ID%\",\"secret\":\"%HOST_SECRET%\"}" ^
  -w "\nHTTP Status: %%{http_code}\n"

echo.
echo [3] Common fixes:
echo.
echo If you see "UnsupportedMediaType" error:
echo - The host code has been fixed to send JSON instead of Basic auth
echo - Rebuild the host project: cd src\RemoteC.Host && dotnet build
echo.
echo If you see "Unauthorized" error:
echo - Check appsettings.Development.json has:
echo   "Host": {
echo     "ValidId": "dev-host-001",
echo     "ValidSecret": "dev-secret-001"
echo   }
echo.
echo [4] To rebuild and run the host:
echo.
echo cd src\RemoteC.Host
echo dotnet build
echo dotnet run -- --server http://localhost:17001 --id %HOST_ID% --secret %HOST_SECRET%
echo.
pause
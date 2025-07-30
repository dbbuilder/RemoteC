@echo off
echo Starting RemoteC Server on Windows...
echo.

cd /d "D:\dev2\remotec\src\RemoteC.Api"

echo Server will be accessible at:
echo   - http://localhost:17001 (this machine)
echo   - http://10.0.0.91:17001 (from network)
echo.
echo Press Ctrl+C to stop the server
echo.

dotnet run --urls "http://0.0.0.0:17001"
pause
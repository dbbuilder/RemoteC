@echo off
echo Starting RemoteC Server on all network interfaces...
cd /d "%~dp0\..\src\RemoteC.Api"

echo.
echo Server will be accessible at:
echo   - http://localhost:17001
for /f "tokens=2 delims=:" %%a in ('ipconfig ^| findstr /C:"IPv4"') do (
    for /f "tokens=1" %%b in ("%%a") do (
        echo   - http://%%b:17001
    )
)
echo.
echo Press Ctrl+C to stop the server
echo.

dotnet run --urls "http://0.0.0.0:17001"
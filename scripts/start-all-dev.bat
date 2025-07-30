@echo off
echo Starting RemoteC Complete Development Environment...
echo ==================================================
echo.

cd /d C:\devlocal\remotec

:: Start API in new window
echo Starting API Server in new window...
start "RemoteC API Server" cmd /k "dotnet run --project src\RemoteC.Api\RemoteC.Api.csproj --urls http://localhost:7001"

:: Wait for API to start
echo Waiting for API to start...
timeout /t 10 /nobreak > nul

:: Check if API is running
powershell -Command "try { Invoke-WebRequest -Uri 'http://localhost:7001/health' -UseBasicParsing | Out-Null; Write-Host 'API is running!' -ForegroundColor Green } catch { Write-Host 'API failed to start!' -ForegroundColor Red }"

echo.
echo Services should be available at:
echo   API:        http://localhost:7001
echo   Swagger UI: http://localhost:7001/swagger
echo   Health:     http://localhost:7001/health
echo.

:: Ask if user wants to start Host
set /p START_HOST=Start RemoteC Host on this machine? (Y/N): 
if /i "%START_HOST%"=="Y" (
    echo.
    echo Starting RemoteC Host...
    start "RemoteC Host" cmd /k "dotnet run --project src\RemoteC.Host\RemoteC.Host.csproj"
)

echo.
echo To stop all services, close the command windows.
echo.
pause
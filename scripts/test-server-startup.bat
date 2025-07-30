@echo off
REM Test RemoteC Server Startup
echo Testing RemoteC Server Startup...
echo ========================================

REM Set development environment
set ASPNETCORE_ENVIRONMENT=Development
set ASPNETCORE_URLS=http://localhost:17001

REM Navigate to API project
cd /d "%~dp0..\src\RemoteC.Api"

REM Start server in background
echo Starting server...
start /B dotnet run --no-launch-profile > server.log 2>&1

REM Wait for server to start
echo Waiting for server to start...
timeout /t 10 /nobreak > nul

REM Test health endpoint
echo Testing health endpoint...
powershell -Command "try { $response = Invoke-WebRequest -Uri 'http://localhost:17001/health' -UseBasicParsing; Write-Host 'Server is running! Health check status:' $response.StatusCode -ForegroundColor Green; Write-Host $response.Content } catch { Write-Host 'Server is not responding!' -ForegroundColor Red; Write-Host $_.Exception.Message }"

REM Kill the server
echo.
echo Stopping server...
taskkill /F /IM dotnet.exe > nul 2>&1

REM Show log file
echo.
echo Server log:
echo ========================================
type server.log

pause
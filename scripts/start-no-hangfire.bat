@echo off
echo Starting RemoteC without Hangfire (Port 17001)
echo =============================================
echo.

cd /d D:\dev2\remotec

:: Kill any process using port 7001
echo Checking for processes on port 17001...
powershell -Command "Get-NetTCPConnection -LocalPort 17001 -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force; Write-Host 'Killed process on port 17001' }"

:: Use a different port to avoid conflicts
set ASPNETCORE_URLS=http://localhost:17001

:: Disable Hangfire and SQL dependencies
set ASPNETCORE_ENVIRONMENT=Development
set ConnectionStrings__DefaultConnection=Data Source=remotec.db
set ConnectionStrings__UseInMemoryDatabase=true
set Hangfire__Enabled=false
set BackgroundJobs__Provider=None
set Cache__Provider=Memory
set Authentication__Enabled=false

echo.
echo Configuration:
echo - Port: 17001
echo - Database: SQLite
echo - Hangfire: DISABLED
echo - Cache: In-Memory
echo.

echo Starting API on http://localhost:17001
echo.

dotnet run --project src\RemoteC.Api\RemoteC.Api.csproj
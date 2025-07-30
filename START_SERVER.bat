@echo off
echo ====================================
echo RemoteC Server - Quick Start
echo ====================================
echo.

cd /d D:\dev2\remotec

:: Use port 17001 to avoid conflicts
set ASPNETCORE_URLS=http://localhost:17001

:: Use special config file that disables Hangfire
set ASPNETCORE_ENVIRONMENT=DevelopmentNoHangfire

echo Starting server on port 17001...
echo.
echo Once started, access:
echo   API:     http://localhost:17001
echo   Swagger: http://localhost:17001/swagger
echo   Health:  http://localhost:17001/health
echo.

dotnet run --project src\RemoteC.Api\RemoteC.Api.csproj
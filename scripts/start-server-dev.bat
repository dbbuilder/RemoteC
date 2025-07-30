@echo off
echo Starting RemoteC Development Server...
echo ====================================
echo.

cd /d C:\devlocal\remotec

:: Check for SQL Server connection
echo Checking prerequisites...
echo.

:: Set development environment
set ASPNETCORE_ENVIRONMENT=Development
set ASPNETCORE_URLS=http://localhost:7001;http://localhost:7002

:: Use in-memory database for quick testing
set ConnectionStrings__DefaultConnection=Data Source=RemoteC.db
set ConnectionStrings__Redis=localhost:6379

echo Starting API Server...
echo.
echo API URL: http://localhost:7001
echo SignalR Hub: http://localhost:7002/hubs/session
echo Swagger UI: http://localhost:7001/swagger
echo.

dotnet run --project src\RemoteC.Api\RemoteC.Api.csproj
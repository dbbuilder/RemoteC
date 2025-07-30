@echo off
echo Starting RemoteC Server on Port 17001
echo =====================================
echo.

cd /d D:\dev2\remotec

:: Configuration for development without external dependencies
set ASPNETCORE_ENVIRONMENT=Development
set ASPNETCORE_URLS=http://localhost:17001;http://localhost:17002

:: Use SQLite instead of SQL Server
set ConnectionStrings__DefaultConnection=Data Source=remotec.db

:: Disable Hangfire to avoid SQL Server dependency
set Hangfire__Enabled=false

:: Use in-memory cache instead of Redis
set Cache__Provider=Memory
set ConnectionStrings__Redis=

:: Disable authentication for testing
set Authentication__Enabled=false

echo Configuration:
echo - API Port: 17001
echo - SignalR Port: 17002  
echo - Database: SQLite (remotec.db)
echo - Background Jobs: Disabled (no Hangfire)
echo - Cache: In-Memory (no Redis)
echo - Authentication: Disabled
echo.

echo Starting API Server...
echo API: http://localhost:17001
echo Swagger: http://localhost:17001/swagger
echo Health: http://localhost:17001/health
echo.

dotnet run --project src\RemoteC.Api\RemoteC.Api.csproj
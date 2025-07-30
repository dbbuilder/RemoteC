@echo off
echo Starting RemoteC in Simple Development Mode...
echo ============================================
echo.

cd /d D:\dev2\remotec

:: Set environment for in-memory database and no Redis
set ASPNETCORE_ENVIRONMENT=Development
set ASPNETCORE_URLS=http://localhost:7001
set ConnectionStrings__DefaultConnection=Data Source=:memory:
set ConnectionStrings__UseInMemoryDatabase=true
set Cache__Provider=Memory
set Authentication__Enabled=false

echo Configuration:
echo - In-Memory Database (no SQL Server required)
echo - In-Memory Cache (no Redis required)
echo - Authentication disabled for testing
echo.

echo Starting API Server...
dotnet run --project src\RemoteC.Api\RemoteC.Api.csproj
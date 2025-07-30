@echo off
echo Quick Fix and Start RemoteC
echo ===========================
echo.

cd /d D:\dev2\remotec

echo Step 1: Adding SQLite support...
dotnet add src\RemoteC.Api\RemoteC.Api.csproj package Microsoft.EntityFrameworkCore.Sqlite --version 8.0.0

echo.
echo Step 2: Fixing database configuration...

:: Create a modified Program.cs that supports SQLite
echo Creating SQLite-compatible startup...
powershell -Command "(Get-Content src\RemoteC.Api\Program.cs) -replace 'options\.UseSqlServer\(builder\.Configuration\.GetConnectionString\(\"DefaultConnection\"\)\);', 'options.UseSqlite(\"Data Source=remotec.db\");' | Set-Content src\RemoteC.Api\Program.cs"

echo.
echo Step 3: Disabling Redis health check...
powershell -Command "(Get-Content src\RemoteC.Api\Program.cs) -replace '\.AddCheck<RedisHealthCheck>\(\"redis\".*?\)', '// Redis disabled for dev' | Set-Content src\RemoteC.Api\Program.cs"

echo.
echo Starting RemoteC API...
echo.
echo API will be available at:
echo   http://localhost:7001
echo   http://localhost:7001/swagger
echo   http://localhost:7001/health
echo.

set ASPNETCORE_ENVIRONMENT=Development
set ASPNETCORE_URLS=http://localhost:7001

dotnet run --project src\RemoteC.Api\RemoteC.Api.csproj
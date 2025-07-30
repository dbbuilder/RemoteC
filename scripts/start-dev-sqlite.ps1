# RemoteC Development Start Script with SQLite
Write-Host "Starting RemoteC with SQLite (No SQL Server Required)" -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan
Write-Host ""

Set-Location "D:\dev2\remotec"

# First, add SQLite package if not already added
Write-Host "Ensuring SQLite package is installed..." -ForegroundColor Yellow
dotnet add src\RemoteC.Api\RemoteC.Api.csproj package Microsoft.EntityFrameworkCore.Sqlite --version 8.0.0

# Set environment variables for SQLite
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:7001"
$env:ConnectionStrings__DefaultConnection = "Data Source=remotec.db"
$env:ConnectionStrings__Redis = ""  # Empty to skip Redis
$env:Cache__Provider = "Memory"     # Use in-memory cache instead of Redis
$env:Authentication__Enabled = "false"  # Disable auth for testing

Write-Host ""
Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Database: SQLite (remotec.db)" -ForegroundColor White
Write-Host "  Cache: In-Memory" -ForegroundColor White
Write-Host "  Authentication: Disabled" -ForegroundColor White
Write-Host "  URL: http://localhost:7001" -ForegroundColor White
Write-Host ""

# Create a temporary Program modification to use SQLite
$programPath = "src\RemoteC.Api\Program.cs"
$programContent = Get-Content $programPath -Raw

# Check if we need to add SQLite support
if ($programContent -notmatch "UseSqlite") {
    Write-Host "Modifying Program.cs to support SQLite..." -ForegroundColor Yellow
    
    # Backup original
    Copy-Item $programPath "$programPath.bak" -Force
    
    # Replace UseSqlServer with conditional logic
    $modifiedContent = $programContent -replace `
        'options\.UseSqlServer\(builder\.Configuration\.GetConnectionString\("DefaultConnection"\)\)', `
        @'
{
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                if (connectionString.Contains(".db"))
                    options.UseSqlite(connectionString);
                else
                    options.UseSqlServer(connectionString);
            }
'@
    
    $modifiedContent | Out-File $programPath -Encoding UTF8
}

Write-Host "Starting API Server..." -ForegroundColor Green
Write-Host ""

# Run the API
dotnet run --project src\RemoteC.Api\RemoteC.Api.csproj
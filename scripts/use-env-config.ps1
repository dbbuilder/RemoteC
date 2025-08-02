#!/usr/bin/env pwsh
# Script to update deployment scripts to use environment variables
# instead of hardcoded connection strings

param(
    [string]$EnvFile = ".env"
)

Write-Host "Loading configuration from environment" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Load environment variables from .env file
if (Test-Path $EnvFile) {
    Get-Content $EnvFile | ForEach-Object {
        if ($_ -match '^([^#][^=]+)=(.*)$') {
            $key = $matches[1].Trim()
            $value = $matches[2].Trim()
            Set-Item -Path "env:$key" -Value $value
        }
    }
    Write-Host "Loaded configuration from $EnvFile" -ForegroundColor Green
} else {
    Write-Host "No .env file found. Using system environment variables." -ForegroundColor Yellow
}

# Build connection string from environment variables
$connectionString = "Server=$env:DB_SERVER,$env:DB_PORT;Database=$env:DB_NAME;User=$env:DB_USER;Password=$env:DB_PASSWORD;TrustServerCertificate=true"

# Export for use in other scripts
$env:REMOTEC_CONNECTION_STRING = $connectionString
$env:REMOTEC_REDIS_CONNECTION = $env:REDIS_CONNECTION

Write-Host ""
Write-Host "Configuration loaded:" -ForegroundColor Green
Write-Host "  Database Server: $env:DB_SERVER" -ForegroundColor Gray
Write-Host "  Database Name: $env:DB_NAME" -ForegroundColor Gray
Write-Host "  Redis: $env:REDIS_CONNECTION" -ForegroundColor Gray
Write-Host ""
Write-Host "Connection string available in: `$env:REMOTEC_CONNECTION_STRING" -ForegroundColor Yellow
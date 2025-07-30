# Start RemoteC API Server in Development Mode
Write-Host "Starting RemoteC API Server in Development Mode..." -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Set development environment
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:17001;https://localhost:17003"

# Navigate to API project
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location "$scriptPath\..\src\RemoteC.Api"

# Add development appsettings if not exists
if (-not (Test-Path "appsettings.Development.json")) {
    Write-Host "Creating appsettings.Development.json..." -ForegroundColor Yellow
    
    $devSettings = @{
        Logging = @{
            LogLevel = @{
                Default = "Information"
                "Microsoft.AspNetCore" = "Warning"
            }
        }
        ConnectionStrings = @{
            DefaultConnection = "Data Source=remotec.db"
        }
        Hangfire = @{
            Enabled = $false
        }
        KeyVault = @{
            VaultName = ""
        }
        AzureAdB2C = @{
            Instance = "https://login.microsoftonline.com/"
            Domain = "yourdomain.onmicrosoft.com"
            TenantId = "00000000-0000-0000-0000-000000000000"
            ClientId = "00000000-0000-0000-0000-000000000000"
            ClientSecret = "dev-secret"
            SignUpSignInPolicyId = "B2C_1_SignUpSignIn"
            ResetPasswordPolicyId = "B2C_1_PasswordReset"
            EditProfilePolicyId = "B2C_1_ProfileEdit"
        }
    }
    
    $devSettings | ConvertTo-Json -Depth 10 | Set-Content "appsettings.Development.json"
}

# Ensure database exists
Write-Host "Checking database..." -ForegroundColor Yellow
if (-not (Test-Path "remotec.db")) {
    Write-Host "Creating SQLite database..." -ForegroundColor Yellow
    dotnet ef database update
}

# Run the server
Write-Host ""
Write-Host "Starting server on http://localhost:17001 and https://localhost:17003" -ForegroundColor Cyan
Write-Host "Press Ctrl+C to stop the server" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Green

dotnet run --no-launch-profile
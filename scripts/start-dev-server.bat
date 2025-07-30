@echo off
REM Start RemoteC API Server in Development Mode
echo Starting RemoteC API Server in Development Mode...
echo ========================================

REM Set development environment
set ASPNETCORE_ENVIRONMENT=Development
set ASPNETCORE_URLS=http://localhost:17001;https://localhost:17003

REM Navigate to API project
cd /d "%~dp0..\src\RemoteC.Api"

REM Add development appsettings if not exists
if not exist appsettings.Development.json (
    echo Creating appsettings.Development.json...
    echo { > appsettings.Development.json
    echo   "Logging": { >> appsettings.Development.json
    echo     "LogLevel": { >> appsettings.Development.json
    echo       "Default": "Information", >> appsettings.Development.json
    echo       "Microsoft.AspNetCore": "Warning" >> appsettings.Development.json
    echo     } >> appsettings.Development.json
    echo   }, >> appsettings.Development.json
    echo   "ConnectionStrings": { >> appsettings.Development.json
    echo     "DefaultConnection": "Data Source=remotec.db" >> appsettings.Development.json
    echo   }, >> appsettings.Development.json
    echo   "Hangfire": { >> appsettings.Development.json
    echo     "Enabled": false >> appsettings.Development.json
    echo   }, >> appsettings.Development.json
    echo   "KeyVault": { >> appsettings.Development.json
    echo     "VaultName": "" >> appsettings.Development.json
    echo   }, >> appsettings.Development.json
    echo   "AzureAdB2C": { >> appsettings.Development.json
    echo     "Instance": "https://login.microsoftonline.com/", >> appsettings.Development.json
    echo     "Domain": "yourdomain.onmicrosoft.com", >> appsettings.Development.json
    echo     "TenantId": "00000000-0000-0000-0000-000000000000", >> appsettings.Development.json
    echo     "ClientId": "00000000-0000-0000-0000-000000000000", >> appsettings.Development.json
    echo     "ClientSecret": "dev-secret", >> appsettings.Development.json
    echo     "SignUpSignInPolicyId": "B2C_1_SignUpSignIn", >> appsettings.Development.json
    echo     "ResetPasswordPolicyId": "B2C_1_PasswordReset", >> appsettings.Development.json
    echo     "EditProfilePolicyId": "B2C_1_ProfileEdit" >> appsettings.Development.json
    echo   } >> appsettings.Development.json
    echo } >> appsettings.Development.json
)

REM Ensure database exists
echo Checking database...
if not exist remotec.db (
    echo Creating SQLite database...
    dotnet ef database update
)

REM Run the server
echo.
echo Starting server on http://localhost:17001 and https://localhost:17003
echo Press Ctrl+C to stop the server
echo ========================================
dotnet run --no-launch-profile

pause
# Fix build issues including cross-project references
$ErrorActionPreference = "Stop"

Write-Host "Fixing RemoteC Build Issues" -ForegroundColor Cyan
Write-Host "==========================" -ForegroundColor Cyan
Write-Host ""

# Change to solution directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionPath = Split-Path -Parent $scriptPath
Set-Location $solutionPath

# Clean all obj and bin directories to remove stale references
Write-Host "Cleaning all build artifacts..." -ForegroundColor Yellow
$dirsToClean = Get-ChildItem -Path . -Include bin,obj -Directory -Recurse
foreach ($dir in $dirsToClean) {
    Write-Host "  Removing $($dir.FullName)" -ForegroundColor Gray
    Remove-Item -Path $dir.FullName -Recurse -Force -ErrorAction SilentlyContinue
}

# Remove any global.json that might be causing issues
if (Test-Path "global.json") {
    Write-Host "Removing global.json..." -ForegroundColor Yellow
    Remove-Item "global.json" -Force
}

# Check for Directory.Build.props in parent directories
Write-Host "`nChecking for Directory.Build.props in parent directories..." -ForegroundColor Yellow
$parentPath = Split-Path -Parent $solutionPath
$grandParentPath = Split-Path -Parent $parentPath

$pathsToCheck = @($parentPath, $grandParentPath, "D:\Dev2", "D:\")
foreach ($path in $pathsToCheck) {
    $buildProps = Join-Path $path "Directory.Build.props"
    if (Test-Path $buildProps) {
        Write-Host "  Found: $buildProps" -ForegroundColor Red
        Write-Host "  This may be causing cross-project references!" -ForegroundColor Red
        
        # Rename it to disable it
        $backupName = "$buildProps.backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
        Write-Host "  Renaming to: $backupName" -ForegroundColor Yellow
        Rename-Item -Path $buildProps -NewName $backupName
    }
}

# Add missing packages to RemoteC.Data
Write-Host "`nFixing RemoteC.Data packages..." -ForegroundColor Yellow
Set-Location "src/RemoteC.Data"
& dotnet add package Microsoft.EntityFrameworkCore --version 8.0.0
& dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.0
& dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.0
& dotnet add package Microsoft.Data.SqlClient --version 5.1.2

# Return to solution root
Set-Location $solutionPath

# Clear NuGet cache
Write-Host "`nClearing NuGet cache..." -ForegroundColor Yellow
& dotnet nuget locals all --clear

# Restore solution
Write-Host "`nRestoring solution..." -ForegroundColor Yellow
& dotnet restore RemoteC.sln --force

# Try building again
Write-Host "`nBuilding solution..." -ForegroundColor Yellow
& dotnet build RemoteC.sln -c Release

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nBUILD SUCCEEDED!" -ForegroundColor Green
    Write-Host ""
    Write-Host "You can now run the ScreenCapture tests:" -ForegroundColor Green
    Write-Host "  dotnet test tests\RemoteC.Tests.Unit\RemoteC.Tests.Unit.csproj --filter FullyQualifiedName~ScreenCaptureServiceTests -c Release" -ForegroundColor Gray
} else {
    Write-Host "`nBuild still failing. Manual intervention may be required." -ForegroundColor Red
    Write-Host "Check if there's a Directory.Build.props file in a parent directory causing issues." -ForegroundColor Yellow
}

Write-Host ""
# Isolate and build test project
Write-Host "Isolated Test Build" -ForegroundColor Cyan
Write-Host "==================" -ForegroundColor Cyan

# First, check if there's a parent Directory.Build.props causing issues
$parentBuildProps = "D:\Dev2\Directory.Build.props"
if (Test-Path $parentBuildProps) {
    Write-Host "`nWARNING: Found parent Directory.Build.props at D:\Dev2" -ForegroundColor Yellow
    Write-Host "This is likely causing the FaithVision cross-project reference!" -ForegroundColor Yellow
    
    # Temporarily rename it
    $backupName = "D:\Dev2\Directory.Build.props.backup"
    Write-Host "Temporarily renaming it to: $backupName" -ForegroundColor Yellow
    Move-Item $parentBuildProps $backupName -Force
}

Set-Location "D:\dev2\remotec"

# Clean everything
Write-Host "`nCleaning all build artifacts..." -ForegroundColor Yellow
Get-ChildItem -Path . -Include bin,obj -Recurse -Force | Remove-Item -Recurse -Force

# Now try to build
Write-Host "`nBuilding test project..." -ForegroundColor Yellow
Set-Location "tests\RemoteC.Tests.Unit"

# Restore first
dotnet restore

# Build
dotnet build -c Release

# Run tests
if ($LASTEXITCODE -eq 0) {
    Write-Host "`nRunning ScreenCapture tests..." -ForegroundColor Cyan
    dotnet test --filter "FullyQualifiedName~ScreenCaptureServiceTests" -c Release --no-build
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n✓ SUCCESS! ScreenCapture tests pass on Windows!" -ForegroundColor Green -BackgroundColor DarkGreen
    }
}

# Restore parent build props if we moved it
if (Test-Path $backupName) {
    Write-Host "`nRestoring parent Directory.Build.props..." -ForegroundColor Yellow
    Move-Item $backupName $parentBuildProps -Force
}

Set-Location "D:\dev2\remotec"
Write-Host "`nDone!" -ForegroundColor Cyan
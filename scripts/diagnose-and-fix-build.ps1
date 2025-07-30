# Diagnose and fix build issues for ScreenCapture tests
$ErrorActionPreference = "Stop"

Write-Host "Diagnosing and Fixing Build Issues" -ForegroundColor Cyan
Write-Host "===================================" -ForegroundColor Cyan
Write-Host ""

Set-Location "D:\dev2\remotec"

# Step 1: Check current directory structure
Write-Host "Step 1: Checking directory structure..." -ForegroundColor Yellow
if (Test-Path "tests\RemoteC.Tests.Unit\RemoteC.Tests.Unit.csproj") {
    Write-Host "  ✓ Test project file exists" -ForegroundColor Green
} else {
    Write-Host "  ✗ Test project file not found!" -ForegroundColor Red
    exit 1
}

# Step 2: Clean ALL build artifacts
Write-Host "`nStep 2: Cleaning ALL build artifacts..." -ForegroundColor Yellow
Get-ChildItem -Path . -Include bin,obj -Recurse -Force | ForEach-Object {
    Write-Host "  Removing: $($_.FullName)" -ForegroundColor Gray
    Remove-Item $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Host "  ✓ Clean complete" -ForegroundColor Green

# Step 3: Check for parent Directory.Build.props
Write-Host "`nStep 3: Checking for interfering parent build files..." -ForegroundColor Yellow
$parentPath = "D:\Dev2"
if (Test-Path "$parentPath\Directory.Build.props") {
    Write-Host "  ! Found parent Directory.Build.props at $parentPath" -ForegroundColor Yellow
    Write-Host "  This may be causing cross-project references" -ForegroundColor Yellow
}

# Step 4: Create local Directory.Build.props
Write-Host "`nStep 4: Creating local Directory.Build.props..." -ForegroundColor Yellow
@'
<Project>
  <PropertyGroup>
    <!-- Disable inheritance from parent directories -->
    <ImportDirectoryBuildProps>false</ImportDirectoryBuildProps>
    <ImportDirectoryBuildTargets>false</ImportDirectoryBuildTargets>
    
    <!-- Set explicit paths -->
    <BaseIntermediateOutputPath>$(MSBuildProjectDirectory)\obj\</BaseIntermediateOutputPath>
    <BaseOutputPath>$(MSBuildProjectDirectory)\bin\</BaseOutputPath>
    
    <!-- Common settings -->
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    
    <!-- Disable warnings -->
    <NoWarn>$(NoWarn);CS1591;NU1605</NoWarn>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
'@ | Out-File "Directory.Build.props" -Encoding UTF8 -NoNewline
Write-Host "  ✓ Created Directory.Build.props" -ForegroundColor Green

# Step 5: Restore solution
Write-Host "`nStep 5: Restoring NuGet packages..." -ForegroundColor Yellow
$restoreOutput = & dotnet restore RemoteC.sln 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ! Restore had issues, but continuing..." -ForegroundColor Yellow
    Write-Host $restoreOutput -ForegroundColor Gray
} else {
    Write-Host "  ✓ Restore successful" -ForegroundColor Green
}

# Step 6: Build test project dependencies first
Write-Host "`nStep 6: Building dependencies..." -ForegroundColor Yellow

# Build RemoteC.Shared
Write-Host "  Building RemoteC.Shared..." -ForegroundColor Gray
$buildOutput = & dotnet build src\RemoteC.Shared\RemoteC.Shared.csproj -c Release --no-restore 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "    Failed to build RemoteC.Shared" -ForegroundColor Red
    Write-Host $buildOutput -ForegroundColor Gray
} else {
    Write-Host "    ✓ Success" -ForegroundColor Green
}

# Build RemoteC.Host (contains ScreenCaptureService)
Write-Host "  Building RemoteC.Host..." -ForegroundColor Gray
$buildOutput = & dotnet build src\RemoteC.Host\RemoteC.Host.csproj -c Release --no-restore 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "    Failed to build RemoteC.Host" -ForegroundColor Red
    Write-Host $buildOutput -ForegroundColor Gray
} else {
    Write-Host "    ✓ Success" -ForegroundColor Green
}

# Step 7: Build test project
Write-Host "`nStep 7: Building test project..." -ForegroundColor Yellow
Set-Location "tests\RemoteC.Tests.Unit"

$buildOutput = & dotnet build -c Release --no-restore 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ✗ Build failed" -ForegroundColor Red
    Write-Host $buildOutput -ForegroundColor Gray
    
    # Try to fix common issues
    Write-Host "`n  Attempting to fix common issues..." -ForegroundColor Yellow
    
    # Add packages if missing
    Write-Host "  Adding test packages..." -ForegroundColor Gray
    & dotnet add package xunit --version 2.6.6
    & dotnet add package xunit.runner.visualstudio --version 2.5.6
    & dotnet add package Moq --version 4.20.70
    & dotnet add package FluentAssertions --version 6.12.0
    & dotnet add package Microsoft.NET.Test.Sdk --version 17.8.0
    
    # Try build again
    Write-Host "`n  Retrying build..." -ForegroundColor Yellow
    $buildOutput = & dotnet build -c Release 2>&1
}

# Step 8: Check if DLL was created
Write-Host "`nStep 8: Verifying build output..." -ForegroundColor Yellow
$dllPath = "bin\Release\net8.0\RemoteC.Tests.Unit.dll"
if (Test-Path $dllPath) {
    Write-Host "  ✓ Test DLL created at: $dllPath" -ForegroundColor Green
    $fileInfo = Get-Item $dllPath
    Write-Host "    Size: $($fileInfo.Length) bytes" -ForegroundColor Gray
    Write-Host "    Modified: $($fileInfo.LastWriteTime)" -ForegroundColor Gray
} else {
    Write-Host "  ✗ Test DLL not found at expected location" -ForegroundColor Red
    Write-Host "  Checking for alternate locations..." -ForegroundColor Yellow
    
    $foundDlls = Get-ChildItem -Path . -Filter "RemoteC.Tests.Unit.dll" -Recurse -ErrorAction SilentlyContinue
    if ($foundDlls) {
        Write-Host "  Found DLLs at:" -ForegroundColor Yellow
        $foundDlls | ForEach-Object {
            Write-Host "    $($_.FullName)" -ForegroundColor Gray
        }
    }
}

# Step 9: Try running tests
if ($LASTEXITCODE -eq 0) {
    Write-Host "`nStep 9: Running ScreenCapture tests..." -ForegroundColor Cyan
    Write-Host "======================================" -ForegroundColor Cyan
    
    # Use explicit DLL path if needed
    $testOutput = & dotnet test --filter "FullyQualifiedName~ScreenCaptureServiceTests" -c Release --no-build --logger "console;verbosity=normal" 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n✓ TESTS PASSED!" -ForegroundColor Green -BackgroundColor DarkGreen
        Write-Host "The ScreenCapture tests work correctly on Windows." -ForegroundColor Green
    } else {
        Write-Host "`n✗ Tests failed or couldn't run" -ForegroundColor Red
        Write-Host $testOutput -ForegroundColor Gray
        
        # Try alternative test command
        Write-Host "`nTrying alternative test command..." -ForegroundColor Yellow
        & dotnet test bin\Release\net8.0\RemoteC.Tests.Unit.dll --filter "FullyQualifiedName~ScreenCaptureServiceTests"
    }
} else {
    Write-Host "`nBuild failed. Cannot run tests." -ForegroundColor Red
}

# Return to root
Set-Location "D:\dev2\remotec"

Write-Host "`nDiagnostics complete." -ForegroundColor Cyan
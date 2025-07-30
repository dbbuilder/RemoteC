# Fix cross-project reference issue
$ErrorActionPreference = "Stop"

Write-Host "Fixing Cross-Project Reference Issue" -ForegroundColor Cyan
Write-Host "===================================" -ForegroundColor Cyan
Write-Host ""

# Kill any MSBuild or dotnet processes that might be locking files
Write-Host "Stopping any running build processes..." -ForegroundColor Yellow
Get-Process -Name "dotnet", "MSBuild", "VBCSCompiler" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# Change to solution directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionPath = Split-Path -Parent $scriptPath
Set-Location $solutionPath

# Create a Directory.Build.props to override any parent settings
Write-Host "Creating local Directory.Build.props to override parent settings..." -ForegroundColor Yellow

$directoryBuildProps = @'
<Project>
  <PropertyGroup>
    <!-- Override any parent Directory.Build.props -->
    <BaseIntermediateOutputPath>$(MSBuildProjectDirectory)\obj\</BaseIntermediateOutputPath>
    <BaseOutputPath>$(MSBuildProjectDirectory)\bin\</BaseOutputPath>
    <OutputPath>$(BaseOutputPath)$(Configuration)\</OutputPath>
    <IntermediateOutputPath>$(BaseIntermediateOutputPath)$(Configuration)\</IntermediateOutputPath>
    <MSBuildProjectExtensionsPath>$(BaseIntermediateOutputPath)</MSBuildProjectExtensionsPath>
    
    <!-- Ensure we're not inheriting from parent directories -->
    <ImportDirectoryBuildProps>false</ImportDirectoryBuildProps>
    <ImportDirectoryBuildTargets>false</ImportDirectoryBuildTargets>
    
    <!-- Set other properties -->
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <WarningsNotAsErrors>NU1605</WarningsNotAsErrors>
  </PropertyGroup>
</Project>
'@

$directoryBuildProps | Out-File -FilePath "Directory.Build.props" -Encoding UTF8
Write-Host "  Created Directory.Build.props" -ForegroundColor Green

# Also check each project file
Write-Host "`nChecking project files for BaseIntermediateOutputPath..." -ForegroundColor Yellow
$projectFiles = Get-ChildItem -Path . -Include *.csproj -Recurse

foreach ($project in $projectFiles) {
    $content = Get-Content $project -Raw
    if ($content -match "BaseIntermediateOutputPath" -or $content -match "FaithVision") {
        Write-Host "  Found reference in: $($project.FullName)" -ForegroundColor Red
        
        # Remove any BaseIntermediateOutputPath settings
        $newContent = $content -replace '<BaseIntermediateOutputPath>.*?</BaseIntermediateOutputPath>', ''
        $newContent = $newContent -replace '<MSBuildProjectExtensionsPath>.*?</MSBuildProjectExtensionsPath>', ''
        
        if ($newContent -ne $content) {
            $newContent | Out-File -FilePath $project.FullName -Encoding UTF8
            Write-Host "    Fixed!" -ForegroundColor Green
        }
    }
}

# Fix the specific package version conflicts in RemoteC.Data
Write-Host "`nFixing RemoteC.Data package versions..." -ForegroundColor Yellow
$dataProject = "src/RemoteC.Data/RemoteC.Data.csproj"
if (Test-Path $dataProject) {
    $content = Get-Content $dataProject -Raw
    
    # Update to consistent versions
    $content = $content -replace 'Version="8.0.0"', 'Version="8.0.7"'
    $content = $content -replace 'Version="5.1.2"', 'Version="5.2.0"'  # Update SqlClient to fix vulnerability
    
    $content | Out-File -FilePath $dataProject -Encoding UTF8
    Write-Host "  Updated package versions" -ForegroundColor Green
}

# Clean everything again
Write-Host "`nCleaning all build artifacts..." -ForegroundColor Yellow
$dirsToClean = Get-ChildItem -Path . -Include bin,obj -Directory -Recurse
foreach ($dir in $dirsToClean) {
    Remove-Item -Path $dir.FullName -Recurse -Force -ErrorAction SilentlyContinue
}

# Now restore and build
Write-Host "`nRestoring solution..." -ForegroundColor Yellow
& dotnet restore RemoteC.sln --force --no-cache

if ($LASTEXITCODE -ne 0) {
    Write-Host "Restore failed. Trying individual projects..." -ForegroundColor Yellow
    
    # Try restoring projects in order
    $projects = @(
        "src/RemoteC.Shared/RemoteC.Shared.csproj",
        "src/RemoteC.Data/RemoteC.Data.csproj",
        "src/RemoteC.Core.Interop/RemoteC.Core.Interop.csproj",
        "src/RemoteC.Api/RemoteC.Api.csproj",
        "src/RemoteC.Host/RemoteC.Host.csproj",
        "src/RemoteC.Client/RemoteC.Client.csproj",
        "tests/RemoteC.Tests.Unit/RemoteC.Tests.Unit.csproj"
    )
    
    foreach ($proj in $projects) {
        if (Test-Path $proj) {
            Write-Host "  Restoring $proj..." -ForegroundColor Gray
            & dotnet restore $proj --force --no-cache
        }
    }
}

Write-Host "`nBuilding solution..." -ForegroundColor Yellow
& dotnet build RemoteC.sln -c Release --no-restore

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nBUILD SUCCEEDED!" -ForegroundColor Green
    Write-Host ""
    Write-Host "You can now run the ScreenCapture tests:" -ForegroundColor Green
    Write-Host "  dotnet test tests\RemoteC.Tests.Unit\RemoteC.Tests.Unit.csproj --filter FullyQualifiedName~ScreenCaptureServiceTests -c Release --no-build" -ForegroundColor Gray
} else {
    Write-Host "`nTrying to build just the test project..." -ForegroundColor Yellow
    & dotnet build tests\RemoteC.Tests.Unit\RemoteC.Tests.Unit.csproj -c Release
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`nTest project built successfully!" -ForegroundColor Green
        Write-Host "Run the ScreenCapture tests:" -ForegroundColor Green
        Write-Host "  dotnet test tests\RemoteC.Tests.Unit\RemoteC.Tests.Unit.csproj --filter FullyQualifiedName~ScreenCaptureServiceTests -c Release --no-build" -ForegroundColor Gray
    }
}

Write-Host ""
# Check for cross-project references
Write-Host "Checking for cross-project references..." -ForegroundColor Cyan

# Search for FaithVision references
$files = Get-ChildItem -Path . -Include *.csproj,*.props,*.targets,Directory.Build.props,Directory.Build.targets -Recurse

foreach ($file in $files) {
    $content = Get-Content $file -Raw
    if ($content -match "FaithVision") {
        Write-Host "Found reference in: $($file.FullName)" -ForegroundColor Red
        $matches = [regex]::Matches($content, ".*FaithVision.*")
        foreach ($match in $matches) {
            Write-Host "  Line: $($match.Value.Trim())" -ForegroundColor Yellow
        }
    }
}

# Check global MSBuild settings
Write-Host "`nChecking MSBuild settings..." -ForegroundColor Cyan
$userProfile = $env:USERPROFILE
$msbuildDirs = @(
    "$userProfile\.nuget\packages",
    "$userProfile\AppData\Local\Microsoft\MSBuild",
    "$userProfile\.dotnet"
)

foreach ($dir in $msbuildDirs) {
    if (Test-Path $dir) {
        Write-Host "Checking $dir..." -ForegroundColor Gray
        $configFiles = Get-ChildItem -Path $dir -Include *.props,*.targets -Recurse -ErrorAction SilentlyContinue | Select-Object -First 10
        foreach ($file in $configFiles) {
            $content = Get-Content $file -Raw -ErrorAction SilentlyContinue
            if ($content -match "FaithVision" -or $content -match "BaseIntermediateOutputPath") {
                Write-Host "  Found in: $($file.FullName)" -ForegroundColor Yellow
            }
        }
    }
}
# Direct build and test with explicit commands
Write-Host "Direct Build and Test Approach" -ForegroundColor Cyan
Write-Host "==============================" -ForegroundColor Cyan

Set-Location "D:\dev2\remotec"

# Clean the specific test project
Write-Host "`nCleaning test project..." -ForegroundColor Yellow
if (Test-Path "tests\RemoteC.Tests.Unit\bin") {
    Remove-Item -Path "tests\RemoteC.Tests.Unit\bin" -Recurse -Force
}
if (Test-Path "tests\RemoteC.Tests.Unit\obj") {
    Remove-Item -Path "tests\RemoteC.Tests.Unit\obj" -Recurse -Force
}

# Try building with MSBuild directly
Write-Host "`nBuilding with MSBuild..." -ForegroundColor Yellow
$msbuildPath = "${env:ProgramFiles}\Microsoft Visual Studio\2022\*\MSBuild\Current\Bin\MSBuild.exe"
$msbuild = Get-Item $msbuildPath -ErrorAction SilentlyContinue | Select-Object -First 1

if ($msbuild) {
    Write-Host "  Using MSBuild at: $($msbuild.FullName)" -ForegroundColor Gray
    & $msbuild.FullName "tests\RemoteC.Tests.Unit\RemoteC.Tests.Unit.csproj" /p:Configuration=Release /p:Platform="Any CPU" /t:Rebuild
} else {
    Write-Host "  MSBuild not found, using dotnet build" -ForegroundColor Yellow
    Set-Location "tests\RemoteC.Tests.Unit"
    dotnet build -c Release --force
}

# Check what was built
Write-Host "`nChecking build output..." -ForegroundColor Yellow
$testDll = Get-ChildItem -Path "tests\RemoteC.Tests.Unit" -Filter "RemoteC.Tests.Unit.dll" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1

if ($testDll) {
    Write-Host "  Found test DLL at: $($testDll.FullName)" -ForegroundColor Green
    
    # Run tests using vstest.console directly
    Write-Host "`nRunning tests with vstest.console..." -ForegroundColor Yellow
    $vstest = "${env:ProgramFiles}\Microsoft Visual Studio\2022\*\Common7\IDE\Extensions\TestPlatform\vstest.console.exe"
    $vstestExe = Get-Item $vstest -ErrorAction SilentlyContinue | Select-Object -First 1
    
    if ($vstestExe) {
        Write-Host "  Using vstest at: $($vstestExe.FullName)" -ForegroundColor Gray
        & $vstestExe.FullName $testDll.FullName /TestCaseFilter:"FullyQualifiedName~ScreenCaptureServiceTests"
    } else {
        Write-Host "  vstest.console not found, using dotnet test" -ForegroundColor Yellow
        dotnet test $testDll.FullName --filter "FullyQualifiedName~ScreenCaptureServiceTests"
    }
} else {
    Write-Host "  Test DLL not found!" -ForegroundColor Red
    Write-Host "  Contents of tests\RemoteC.Tests.Unit:" -ForegroundColor Yellow
    Get-ChildItem -Path "tests\RemoteC.Tests.Unit" -Recurse | Where-Object { !$_.PSIsContainer } | Select-Object FullName
}

Write-Host "`nDone!" -ForegroundColor Cyan
# Test after fixing CA1822 warning
Write-Host "Testing ScreenCapture after CA1822 fix" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan

Set-Location "D:\dev2\remotec"

# Build the specific projects needed
Write-Host "`nBuilding RemoteC.Core.Interop..." -ForegroundColor Yellow
dotnet build src\RemoteC.Core.Interop\RemoteC.Core.Interop.csproj -c Release

if ($LASTEXITCODE -eq 0) {
    Write-Host "  Success - RemoteC.Core.Interop built successfully" -ForegroundColor Green
    
    # Now build the test project
    Write-Host "`nBuilding test project..." -ForegroundColor Yellow
    Set-Location "tests\RemoteC.Tests.Unit"
    dotnet build -c Release
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  Success - Test project built successfully" -ForegroundColor Green
        
        # Run the ScreenCapture tests
        Write-Host "`nRunning ScreenCapture tests..." -ForegroundColor Cyan
        $output = dotnet test --filter "FullyQualifiedName~ScreenCaptureServiceTests" -c Release --no-build --logger "console;verbosity=normal" | Out-String
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "`nSUCCESS! ScreenCapture tests pass on Windows!" -ForegroundColor Green -BackgroundColor DarkGreen
            Write-Host "`nThis confirms that System.Drawing.Common works correctly on Windows." -ForegroundColor Green
            Write-Host "The 2 tests that were failing on Linux/WSL now pass on Windows platform." -ForegroundColor Green
            
            # Parse test results
            if ($output -match "Total tests: (\d+)") {
                $total = $matches[1]
            }
            if ($output -match "Passed: (\d+)") {
                $passed = $matches[1]
            }
            if ($output -match "Failed: (\d+)") {
                $failed = $matches[1]
            }
            
            Write-Host "`nTest Results:" -ForegroundColor Cyan
            Write-Host "  Total:  $total" -ForegroundColor White
            Write-Host "  Passed: $passed" -ForegroundColor Green
            if ($failed -eq '0') {
                Write-Host "  Failed: $failed" -ForegroundColor Green
            } else {
                Write-Host "  Failed: $failed" -ForegroundColor Red
            }
        } else {
            Write-Host "`nTests failed" -ForegroundColor Red
            Write-Host $output
        }
    }
}

Set-Location "D:\dev2\remotec"
Write-Host "`nDone!" -ForegroundColor Cyan
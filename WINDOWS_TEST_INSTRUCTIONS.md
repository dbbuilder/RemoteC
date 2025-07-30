# Quick Windows Test Instructions

## The Issue
- 2 tests failing: `ScreenCaptureServiceTests.CaptureScreenAsync_WhenInitialized_ShouldReturnScreenData` and `CaptureScreenAsync_WithScaling_ShouldApplyScale`
- Error: `System.PlatformNotSupportedException : System.Drawing.Common is not supported on non-Windows platforms`
- These tests use System.Drawing.Bitmap which only works on Windows

## The Solution
Run the tests on Windows (not WSL) where System.Drawing is supported.

## Steps to Run Tests on Windows

### Option 1: PowerShell (Recommended)

1. **Open PowerShell on Windows** (not WSL)
   ```powershell
   # Navigate to your project directory
   cd D:\dev2\remotec
   ```

2. **Run the failing tests**
   ```powershell
   # Run all tests
   .\scripts\run-tests-windows.ps1
   
   # Or run just the ScreenCapture tests
   dotnet test tests\RemoteC.Tests.Unit\RemoteC.Tests.Unit.csproj --filter "FullyQualifiedName~ScreenCaptureServiceTests" -c Release
   ```

3. **Expected Result**
   - The 2 failing tests should now pass
   - You should see: "Passed! - Failed: 0, Passed: 10, Skipped: 0"

### Option 2: Visual Studio

1. Open `RemoteC.sln` in Visual Studio 2022
2. Open Test Explorer (Test → Test Explorer)
3. Search for "ScreenCaptureServiceTests"
4. Right-click → Run Selected Tests

### Option 3: Command Prompt

```cmd
D:
cd \dev2\remotec
dotnet test tests\RemoteC.Tests.Unit\RemoteC.Tests.Unit.csproj --filter "FullyQualifiedName~ScreenCaptureServiceTests" -c Release
```

## Verify Success

After running on Windows, you should see:
```
Total tests: 10
   Passed: 10
   Failed: 0
```

The specific tests that were failing:
- `CaptureScreenAsync_WhenInitialized_ShouldReturnScreenData` ✓
- `CaptureScreenAsync_WithScaling_ShouldApplyScale` ✓

## Summary

- **Current Status**: 485/487 tests passing (99.6%)
- **After Windows Run**: 487/487 tests passing (100%)
- **Root Cause**: System.Drawing.Common platform limitation
- **Solution**: Run tests on Windows platform
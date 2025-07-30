# Windows Test Execution Guide

This guide explains how to run RemoteC tests on Windows to resolve System.Drawing.Common platform issues.

## Background

The ScreenCaptureService uses `System.Drawing.Common` for image processing, which is only supported on Windows. When running tests on Linux/WSL, you'll encounter:

```
System.PlatformNotSupportedException : System.Drawing.Common is not supported on non-Windows platforms.
```

## Prerequisites

1. **Windows OS** (not WSL)
2. **.NET 8.0 SDK** installed
3. **PowerShell** (5.1 or Core 7+)
4. **Visual Studio 2022** (optional, for debugging)

## Quick Start

### 1. Open PowerShell on Windows

```powershell
# Open PowerShell as Administrator (recommended)
# Navigate to the project directory
cd D:\dev2\remotec
```

### 2. Run All Tests

```powershell
# Run all tests
.\scripts\run-tests-windows.ps1

# Run specific test categories
.\scripts\run-tests-windows.ps1 -Project unit
.\scripts\run-tests-windows.ps1 -Project api
.\scripts\run-tests-windows.ps1 -Project integration
.\scripts\run-tests-windows.ps1 -Project performance

# Run with verbose output
.\scripts\run-tests-windows.ps1 -Verbose

# Run with code coverage
.\scripts\run-tests-windows.ps1 -CollectCoverage
```

### 3. Run Specific Tests

```powershell
# Run the failing ScreenCaptureService tests
.\scripts\run-specific-test-windows.ps1 -TestName ScreenCaptureServiceTests

# Run with debugging
.\scripts\run-specific-test-windows.ps1 -TestName ScreenCaptureServiceTests -Debug

# Run a specific test method
.\scripts\run-specific-test-windows.ps1 -TestName "CaptureScreenAsync_WhenInitialized_ShouldReturnScreenData"
```

### 4. Fix ScreenCapture Tests

```powershell
# Check and fix ScreenCapture test issues
.\scripts\fix-screen-capture-tests.ps1

# Dry run (check without making changes)
.\scripts\fix-screen-capture-tests.ps1 -DryRun
```

## Expected Results

### Before (on Linux/WSL)
```
Failed RemoteC.Tests.Unit.Host.Services.ScreenCaptureServiceTests.CaptureScreenAsync_WhenInitialized_ShouldReturnScreenData
Error Message:
 System.PlatformNotSupportedException : System.Drawing.Common is not supported on non-Windows platforms.
```

### After (on Windows)
```
✓ RemoteC.Tests.Unit.Host.Services.ScreenCaptureServiceTests.CaptureScreenAsync_WhenInitialized_ShouldReturnScreenData
✓ RemoteC.Tests.Unit.Host.Services.ScreenCaptureServiceTests.CaptureScreenAsync_WithScaling_ShouldApplyScale

All tests passed!
```

## Troubleshooting

### 1. System.Drawing.Common Still Not Working

```powershell
# Check installed runtimes
dotnet --list-runtimes

# Install Windows Desktop Runtime if missing
winget install Microsoft.DotNet.DesktopRuntime.8

# Explicitly add System.Drawing.Common package
cd tests\RemoteC.Tests.Unit
dotnet add package System.Drawing.Common --version 8.0.0
```

### 2. Build Errors

```powershell
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build -c Release
```

### 3. Permission Issues

```powershell
# Run PowerShell as Administrator
# Or set execution policy
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

## Visual Studio Testing

If you prefer Visual Studio:

1. Open `RemoteC.sln` in Visual Studio 2022
2. Open Test Explorer (Test → Test Explorer)
3. Filter for "ScreenCaptureServiceTests"
4. Right-click → Run Selected Tests
5. For debugging: Right-click → Debug Selected Tests

## CI/CD Considerations

For GitHub Actions or other CI/CD pipelines:

```yaml
# Use Windows runner for tests that require System.Drawing
jobs:
  test-windows:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      - name: Run Windows-specific tests
        run: |
          dotnet test tests/RemoteC.Tests.Unit/RemoteC.Tests.Unit.csproj \
            --filter "FullyQualifiedName~ScreenCaptureServiceTests" \
            -c Release
```

## Alternative Solutions

### 1. Conditional Compilation

```csharp
#if Windows
    // Use System.Drawing
#else
    // Use SkiaSharp or ImageSharp
#endif
```

### 2. Mock System.Drawing in Tests

```csharp
// Create abstraction layer
public interface IImageProcessor
{
    byte[] ProcessImage(byte[] data, int width, int height);
}

// Mock in tests instead of using real System.Drawing
```

### 3. Use Cross-Platform Libraries

- **SkiaSharp**: Cross-platform 2D graphics
- **ImageSharp**: Fully managed image processing
- **Magick.NET**: ImageMagick for .NET

## Summary

Running tests on Windows resolves the System.Drawing.Common platform issues. Use the provided PowerShell scripts for easy test execution and debugging. For long-term cross-platform support, consider migrating to platform-agnostic image processing libraries.
@echo off
REM =====================================================
REM RemoteC Server - Quick Start Script
REM =====================================================
echo.
echo RemoteC Server - Quick Start
echo ============================
echo.

REM Check if .NET is installed
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK is not installed!
    echo Please install .NET 8.0 SDK from https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

REM Set environment to development
set ASPNETCORE_ENVIRONMENT=Development
set ASPNETCORE_URLS=http://localhost:17001

echo Starting RemoteC Server in Development Mode...
echo.
echo Server will be available at:
echo   - http://localhost:17001 (API)
echo   - http://localhost:17001/swagger (API Documentation)
echo   - http://localhost:17001/health (Health Check)
echo.
echo Press Ctrl+C to stop the server
echo ============================
echo.

REM Navigate to API directory and run
cd /d "%~dp0src\RemoteC.Api"
dotnet run --no-launch-profile
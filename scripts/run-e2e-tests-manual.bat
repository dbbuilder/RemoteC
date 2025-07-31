@echo off
echo =========================================
echo   Running E2E Tests (Manual Server Start)
echo =========================================
echo.
echo This script runs E2E tests with manually started servers
echo to avoid permission issues with auto-start.
echo.

cd /d D:\dev2\remotec\src\RemoteC.Web

:: Clean Vite deps
if exist "node_modules\.vite" (
    echo Cleaning Vite cache...
    rmdir /s /q "node_modules\.vite" 2>nul
)

echo.
echo [1] Please ensure the API server is running:
echo     Run in another terminal: scripts\start-server-windows.bat
echo.
echo [2] Please ensure the UI is running:
echo     Run in another terminal: scripts\start-dev-ui.bat
echo.
echo Press any key when both servers are running...
pause >nul

echo.
echo [3] Running E2E tests against running servers...
echo.

:: Run tests without webServer config
call npx playwright test --config=playwright-manual.config.ts

if %errorlevel% neq 0 (
    echo.
    echo =========================================
    echo      E2E Tests Failed!
    echo =========================================
    echo.
    echo View the HTML report:
    call npx playwright show-report
) else (
    echo.
    echo =========================================
    echo    All E2E Tests Passed!
    echo =========================================
    echo.
    echo View the HTML report:
    call npx playwright show-report
)

pause
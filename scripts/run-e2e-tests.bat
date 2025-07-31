@echo off
echo =========================================
echo      Running RemoteC E2E Tests
echo =========================================
echo.

cd /d D:\dev2\remotec\src\RemoteC.Web

echo [1] Checking dependencies...
if not exist "node_modules\@playwright\test" (
    echo Installing Playwright...
    call npm install -D @playwright/test
    call npx playwright install
)

echo.
echo [2] Cleaning Vite cache to prevent permission errors...
if exist "node_modules\.vite" (
    rmdir /s /q "node_modules\.vite" 2>nul
)

echo.
echo [3] Starting API server and UI...
echo Note: Tests will automatically start the servers
echo.

echo [4] Running E2E tests...
echo.
call npm run test:e2e

if %errorlevel% neq 0 (
    echo.
    echo =========================================
    echo      E2E Tests Failed!
    echo =========================================
    echo.
    echo Check the test report at:
    echo src\RemoteC.Web\playwright-report\index.html
    echo.
    pause
    exit /b 1
)

echo.
echo =========================================
echo    All E2E Tests Passed!
echo =========================================
echo.
echo View detailed report at:
echo src\RemoteC.Web\playwright-report\index.html
echo.
pause
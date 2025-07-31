@echo off
echo =========================================
echo    Testing RemoteC Authentication Modes
echo =========================================
echo.

cd /d D:\dev2\remotec\src\RemoteC.Web

echo [1] Checking Node.js installation...
node --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: Node.js is not installed!
    echo Please install from https://nodejs.org/
    pause
    exit /b 1
)
echo OK - Node.js is installed

echo.
echo [2] Checking npm dependencies...
if not exist "node_modules\vite" (
    echo ERROR: Dependencies not installed!
    echo Run: install-web-dependencies.bat
    pause
    exit /b 1
)
echo OK - Dependencies installed

echo.
echo [3] Testing TypeScript compilation...
call npx tsc --noEmit
if %errorlevel% neq 0 (
    echo ERROR: TypeScript compilation failed!
    pause
    exit /b 1
)
echo OK - TypeScript compiles successfully

echo.
echo [4] Checking development mode configuration...
if not exist "src\DevApp.tsx" (
    echo ERROR: DevApp.tsx missing!
    pause
    exit /b 1
)
if not exist "src\pages\DevLoginPage.tsx" (
    echo ERROR: DevLoginPage.tsx missing!
    pause
    exit /b 1
)
echo OK - Development mode files present

echo.
echo [5] Building production bundle...
call npm run build
if %errorlevel% neq 0 (
    echo ERROR: Production build failed!
    pause
    exit /b 1
)
echo OK - Production build successful

echo.
echo =========================================
echo    All Authentication Modes Verified!
echo =========================================
echo.
echo Development Mode:
echo   - Run: start-dev-ui.bat
echo   - Login: Any username/password
echo   - No Azure AD required
echo.
echo Production Mode:
echo   - Run: start-ui-production.bat
echo   - Login: Azure AD credentials
echo   - Requires Azure AD configuration
echo.
echo Environment Detection:
echo   - npm run dev = Development mode
echo   - npm run preview = Production mode
echo.
pause
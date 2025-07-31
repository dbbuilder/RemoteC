@echo off
echo =========================================
echo    Fixing Vite Dependencies Permissions
echo =========================================
echo.

cd /d D:\dev2\remotec\src\RemoteC.Web

echo Cleaning Vite cache and dependencies...
echo.

:: Remove .vite directory with force
if exist "node_modules\.vite" (
    echo Removing .vite directory...
    rmdir /s /q "node_modules\.vite" 2>nul
    
    :: If rmdir fails, try with admin privileges
    if exist "node_modules\.vite" (
        echo Trying with elevated permissions...
        takeown /f "node_modules\.vite" /r /d y >nul 2>&1
        icacls "node_modules\.vite" /grant "%USERNAME%":F /t /q >nul 2>&1
        rmdir /s /q "node_modules\.vite"
    )
)

:: Remove vite cache from temp
if exist "%TEMP%\vite" (
    echo Cleaning Vite temp cache...
    rmdir /s /q "%TEMP%\vite" 2>nul
)

:: Clear npm cache for vite
echo Clearing npm cache...
call npm cache clean --force 2>nul

echo.
echo =========================================
echo    Vite cache cleaned successfully!
echo =========================================
echo.
echo You can now run the E2E tests.
echo.
pause
@echo off
echo =========================================
echo    Starting RemoteC Production UI
echo    (Simple Auth Mode)
echo =========================================
echo.

REM Check if API is running
echo [1] Checking API Server...
curl -s http://localhost:17001/health > nul 2>&1
if %errorlevel% neq 0 (
    echo WARNING: API server not detected on port 17001
    echo Please start the API server first using start-full-stack-dev.bat
    echo.
    echo Continue anyway? (Y/N)
    choice /c YN
    if errorlevel 2 exit /b 1
) else (
    echo API server is running on port 17001
)

REM Test SQL Server connection
echo.
echo [2] Testing SQL Server connection...
sqlcmd -S 172.31.208.1,14333 -U sv -P Gv51076! -C -Q "SELECT 'Database connection successful' as Status" -d master
if %errorlevel% neq 0 (
    echo ERROR: Cannot connect to SQL Server!
    echo Please ensure SQL Server is running and accessible.
    pause
    exit /b 1
)

REM Create env file for simple auth
echo.
echo [3] Configuring Simple Auth Mode...
cd /d %~dp0..\src\RemoteC.Web
echo VITE_USE_SIMPLE_AUTH=true > .env.local
echo VITE_API_URL=http://localhost:17001 >> .env.local
echo Simple auth configuration created

REM Start the UI in production mode
echo.
echo [4] Starting React UI in Production Mode on http://localhost:17002
start "RemoteC Production UI" cmd /k "echo Starting RemoteC UI in Production Mode... && echo. && echo Login Credentials: && echo   admin / admin123 (Full access) && echo   operator / operator123 (Operator access) && echo   viewer / viewer123 (Read-only access) && echo. && npm run dev"

echo.
echo =========================================
echo    Production UI Starting...
echo =========================================
echo.
echo UI:         http://localhost:17002
echo API Server: http://localhost:17001
echo.
echo Login Credentials:
echo   Username: admin     Password: admin123     (Full access)
echo   Username: operator  Password: operator123  (Operator access)
echo   Username: viewer    Password: viewer123    (Read-only access)
echo.
echo The UI will open in your browser automatically.
echo.
echo To stop the UI, close the UI window or press Ctrl+C
echo.
pause
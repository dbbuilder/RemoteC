@echo off
echo =========================================
echo    Starting RemoteC Full Stack (Dev Mode)
echo =========================================
echo.
echo This script will start:
echo   1. SQL Server connection test
echo   2. Backend API server
echo   3. React UI in development mode
echo.
echo Press Ctrl+C in any window to stop that component
echo.

REM Test SQL Server connection first
echo [1] Testing SQL Server connection...
sqlcmd -S 172.31.208.1,14333 -U sv -P Gv51076! -C -Q "SELECT 'Database connection successful' as Status" -d master
if %errorlevel% neq 0 (
    echo ERROR: Cannot connect to SQL Server!
    echo Please ensure SQL Server is running and accessible.
    pause
    exit /b 1
)

REM Start the API server in a new window
echo.
echo [2] Starting API Server on http://localhost:17001
start "RemoteC API Server" cmd /k "cd /d %~dp0..\src\RemoteC.Api && echo Starting RemoteC API Server... && dotnet run"

REM Wait for API to start
echo Waiting for API server to start...
timeout /t 5 /nobreak > nul

REM Check if API is running
curl -s http://localhost:17001/health > nul 2>&1
if %errorlevel% neq 0 (
    echo WARNING: API server may not be ready yet.
    echo Continue anyway? (Y/N)
    choice /c YN
    if errorlevel 2 exit /b 1
)

REM Start the React UI in a new window
echo.
echo [3] Starting React UI on http://localhost:17002
start "RemoteC UI" cmd /k "cd /d %~dp0..\src\RemoteC.Web && echo Starting RemoteC UI... && npm run dev"

echo.
echo =========================================
echo    All services starting...
echo =========================================
echo.
echo API Server: http://localhost:17001
echo UI:         http://localhost:17002
echo.
echo The UI will open in your browser automatically.
echo.
echo To stop all services, close each window or press Ctrl+C
echo.
pause
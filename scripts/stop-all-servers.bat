@echo off
echo =========================================
echo      Stopping All RemoteC Servers
echo =========================================
echo.

echo Stopping processes on RemoteC ports...
echo.

echo [1] Stopping API server on port 17001...
for /f "tokens=5" %%a in ('netstat -aon ^| findstr :17001 ^| findstr LISTENING') do (
    echo Killing process %%a
    taskkill /F /PID %%a 2>nul
)

echo [2] Stopping UI server on port 17002...
for /f "tokens=5" %%a in ('netstat -aon ^| findstr :17002 ^| findstr LISTENING') do (
    echo Killing process %%a
    taskkill /F /PID %%a 2>nul
)

echo [3] Killing any RemoteC.Api processes...
taskkill /F /IM RemoteC.Api.exe 2>nul
taskkill /F /IM dotnet.exe /FI "WINDOWTITLE eq RemoteC.Api" 2>nul

echo [4] Killing any node processes (UI servers)...
taskkill /F /IM node.exe 2>nul

echo.
echo =========================================
echo    All servers stopped!
echo =========================================
echo.
echo You can now rebuild the solution.
echo.
pause
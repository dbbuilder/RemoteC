@echo off
echo =========================================
echo    Killing Process Locking DLL Files
echo =========================================
echo.

echo The API server (PID 9688) is locking the DLL files.
echo.

echo Attempting to kill process 9688...
taskkill /F /PID 9688

if %errorlevel% equ 0 (
    echo.
    echo Process killed successfully!
    echo You can now rebuild the project.
) else (
    echo.
    echo Failed to kill process. Trying with admin privileges...
    echo.
    echo Please run this script as Administrator if the process won't stop.
    echo.
    echo Alternative: Open Task Manager and end the following:
    echo - RemoteC.Api.exe
    echo - dotnet.exe processes related to RemoteC
)

echo.
pause
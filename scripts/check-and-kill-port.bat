@echo off
echo Checking what's using port 7001...
echo ==================================
echo.

netstat -ano | findstr :7001

echo.
echo To kill the process using port 7001:
echo 1. Note the PID (last column) from above
echo 2. Run: taskkill /PID [PID_NUMBER] /F
echo.
echo Or use this PowerShell command to auto-kill:
echo.

powershell -Command "Get-NetTCPConnection -LocalPort 7001 -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force; Write-Host 'Killed process' $_.OwningProcess }"

echo.
pause
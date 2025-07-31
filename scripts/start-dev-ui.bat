@echo off
echo ================================================
echo     RemoteC Development UI (No Azure AD)
echo ================================================
echo.
echo This will start the UI in DEVELOPMENT MODE:
echo - No Azure AD authentication required
echo - Use any username/password to login
echo - Default: admin/admin
echo.
echo ================================================
echo.

cd /d D:\dev2\remotec\src\RemoteC.Web

echo Starting development server...
echo.
echo Once started, access the UI at:
echo - http://localhost:3000
echo.

call npm run dev

pause
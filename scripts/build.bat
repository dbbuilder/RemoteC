@echo off
REM RemoteC Build Script for Windows
REM This script builds all components of the RemoteC solution

echo Building RemoteC Solution...

REM Navigate to solution root
cd /d "%~dp0\.."

REM Build .NET solution
echo Building .NET solution...
dotnet restore
if %errorlevel% neq 0 exit /b %errorlevel%

dotnet build --configuration Release --no-restore
if %errorlevel% neq 0 exit /b %errorlevel%

REM Run tests
echo Running unit tests...
dotnet test --no-build --configuration Release --verbosity normal
if %errorlevel% neq 0 exit /b %errorlevel%

REM Build React frontend
echo Building React frontend...
cd src\RemoteC.Web
call npm ci
if %errorlevel% neq 0 exit /b %errorlevel%

call npm run build
if %errorlevel% neq 0 exit /b %errorlevel%

cd ..\..

REM Create deployment package
echo Creating deployment package...
if not exist deployment\build mkdir deployment\build
dotnet publish src\RemoteC.Api -c Release -o deployment\build\api --no-build
if %errorlevel% neq 0 exit /b %errorlevel%

xcopy /E /I src\RemoteC.Web\build deployment\build\web
if %errorlevel% neq 0 exit /b %errorlevel%

echo Build completed successfully!
echo Deployment files are in: deployment\build\
@echo off
REM Build script for RemoteC Rust Core library (Windows)

setlocal enabledelayedexpansion

set SCRIPT_DIR=%~dp0
set PROJECT_ROOT=%SCRIPT_DIR%..
set RUST_PROJECT=%PROJECT_ROOT%\src\RemoteC.Core
set OUTPUT_DIR=%PROJECT_ROOT%\src\RemoteC.Core.Interop\runtimes

echo Building RemoteC Rust Core...
echo Project root: %PROJECT_ROOT%
echo Rust project: %RUST_PROJECT%

REM Check if Rust is installed
where cargo >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo Error: Rust is not installed. Please install from https://rustup.rs/
    exit /b 1
)

REM Create output directories
if not exist "%OUTPUT_DIR%\win-x64\native" mkdir "%OUTPUT_DIR%\win-x64\native"
if not exist "%OUTPUT_DIR%\win-x86\native" mkdir "%OUTPUT_DIR%\win-x86\native"
if not exist "%OUTPUT_DIR%\linux-x64\native" mkdir "%OUTPUT_DIR%\linux-x64\native"
if not exist "%OUTPUT_DIR%\osx-x64\native" mkdir "%OUTPUT_DIR%\osx-x64\native"

REM Build for Windows x64
cd /d "%RUST_PROJECT%"
echo Building for Windows x64...
cargo build --release

REM Copy Windows x64 library
echo Copying Windows x64 library...
copy /Y "target\release\remotec_core.dll" "%OUTPUT_DIR%\win-x64\native\"
if exist "target\release\remotec_core.dll.lib" (
    copy /Y "target\release\remotec_core.dll.lib" "%OUTPUT_DIR%\win-x64\native\remotec_core.lib"
)

REM Build for Windows x86 if target is installed
rustup target list | findstr /C:"i686-pc-windows-msvc (installed)" >nul
if %ERRORLEVEL% EQU 0 (
    echo Building for Windows x86...
    cargo build --release --target i686-pc-windows-msvc
    copy /Y "target\i686-pc-windows-msvc\release\remotec_core.dll" "%OUTPUT_DIR%\win-x86\native\"
) else (
    echo Note: Install x86 target for 32-bit support:
    echo   rustup target add i686-pc-windows-msvc
)

REM Cross-compile for other platforms if cross is installed
where cross >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    echo Cross compilation available. Building for other platforms...
    
    echo Cross-compiling for Linux x64...
    cross build --release --target x86_64-unknown-linux-gnu
    copy /Y "target\x86_64-unknown-linux-gnu\release\libremotec_core.so" "%OUTPUT_DIR%\linux-x64\native\" 2>nul
    
    echo Cross-compiling for macOS x64...
    cross build --release --target x86_64-apple-darwin
    copy /Y "target\x86_64-apple-darwin\release\libremotec_core.dylib" "%OUTPUT_DIR%\osx-x64\native\" 2>nul
) else (
    echo Note: Install 'cross' for cross-platform compilation:
    echo   cargo install cross
)

echo Build complete. Libraries are in: %OUTPUT_DIR%

REM Generate documentation
echo Generating FFI documentation...
cd /d "%RUST_PROJECT%"
cargo doc --no-deps

echo Done!
endlocal
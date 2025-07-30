@echo off
REM Performance test runner script for Windows

echo ======================================
echo RemoteC Performance Test Suite
echo ======================================
echo.

REM Change to the performance test directory
cd /d "%~dp0"

REM Build the project
echo Building performance tests...
dotnet build --configuration Release

if %ERRORLEVEL% neq 0 (
    echo Build failed. Exiting.
    exit /b 1
)

REM Create results directory
if not exist results mkdir results

REM Set test date
for /f "tokens=2-4 delims=/ " %%a in ('date /t') do set DATE=%%c%%a%%b
for /f "tokens=1-2 delims=: " %%a in ('time /t') do set TIME=%%a%%b
set TEST_DATE=%DATE%_%TIME: =%
set RESULTS_DIR=results\%TEST_DATE%
if not exist "%RESULTS_DIR%" mkdir "%RESULTS_DIR%"

echo.
echo Running performance tests...
echo Results will be saved to: %RESULTS_DIR%
echo.

REM Run the performance tests
dotnet run --configuration Release -- --benchmarks > "%RESULTS_DIR%\benchmark_results.txt" 2>&1
type "%RESULTS_DIR%\benchmark_results.txt"

echo.
echo ======================================
echo Performance Test Summary
echo ======================================
echo.

REM Generate summary report
(
echo # RemoteC Performance Test Report
echo Date: %date% %time%
echo.
echo ## Test Environment
echo - OS: Windows
echo - .NET Version: 
dotnet --version
echo.
echo ## Results Summary
echo.
echo ### API Performance
echo - Device List API: Check benchmark_results.txt for details
echo - Session Creation API: Check benchmark_results.txt for details
echo - Concurrent API Requests: Check benchmark_results.txt for details
echo.
echo ### SignalR Performance
echo - Message Broadcasting: Check benchmark_results.txt for details
echo - Concurrent Message Processing: Check benchmark_results.txt for details
echo - High-Frequency Updates: Check benchmark_results.txt for details
echo.
echo ### Database Performance
echo - User Query: Check benchmark_results.txt for details
echo - Device Query with Joins: Check benchmark_results.txt for details
echo - Complex Session Query: Check benchmark_results.txt for details
echo - Bulk Insert: Check benchmark_results.txt for details
echo - Concurrent Operations: Check benchmark_results.txt for details
echo.
echo ### Remote Control Performance
echo - Screen Capture: Check benchmark_results.txt for details
echo - Input Processing: Check benchmark_results.txt for details
echo - Frame Compression: Check benchmark_results.txt for details
echo.
echo ## Performance Targets
echo.
echo ### Phase 1 ^(Current^)
echo - [ ] Screen capture latency: ^<100ms
echo - [ ] Network latency: ^<100ms on LAN
echo - [ ] API response time: ^<300ms
echo - [ ] SignalR connection time: ^<500ms
echo.
echo ### Phase 2 ^(Rust Engine^)
echo - [ ] Screen capture latency: ^<50ms
echo - [ ] Network latency: ^<50ms ^(QUIC^)
echo - [ ] 60 FPS sustained capture
echo - [ ] Hardware encoding support
echo.
echo ## Recommendations
echo.
echo 1. **Database Optimization**
echo    - Add covering indexes for frequent queries
echo    - Enable query store for monitoring
echo    - Consider read replicas for reporting
echo.
echo 2. **API Optimization**
echo    - Implement response caching
echo    - Use output caching for static content
echo    - Enable response compression
echo.
echo 3. **SignalR Optimization**
echo    - Use MessagePack for serialization
echo    - Implement backpressure handling
echo    - Consider Azure SignalR Service for scale
echo.
echo 4. **Infrastructure**
echo    - Use CDN for static assets
echo    - Enable HTTP/2 and HTTP/3
echo    - Implement connection pooling
) > "%RESULTS_DIR%\summary_report.md"

echo Summary report saved to: %RESULTS_DIR%\summary_report.md
echo.
echo Performance tests completed!
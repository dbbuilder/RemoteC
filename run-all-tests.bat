@echo off
REM Comprehensive test runner and remediation plan generator for Windows

echo ================================================
echo RemoteC Comprehensive Test Suite
echo ================================================
echo.

REM Create results directory
for /f "tokens=2-4 delims=/ " %%a in ('date /t') do set DATE=%%c%%a%%b
for /f "tokens=1-2 delims=: " %%a in ('time /t') do set TIME=%%a%%b
set TEST_DATE=%DATE%_%TIME: =%
set RESULTS_DIR=test-results\%TEST_DATE%
if not exist test-results mkdir test-results
if not exist "%RESULTS_DIR%" mkdir "%RESULTS_DIR%"

REM Summary variables
set TOTAL_TESTS=0
set PASSED_TESTS=0
set FAILED_TESTS=0
set WARNINGS=0

echo Test run started at: %date% %time%
echo Results will be saved to: %RESULTS_DIR%
echo.

REM 1. Unit Tests
echo === UNIT TESTS ===
echo Running RemoteC.Tests.Unit...
dotnet test tests\RemoteC.Tests.Unit\RemoteC.Tests.Unit.csproj --no-build --logger:trx > "%RESULTS_DIR%\RemoteC.Tests.Unit_results.txt" 2>&1
if %ERRORLEVEL% == 0 (
    echo [PASSED] RemoteC.Tests.Unit
    set /a PASSED_TESTS+=1
) else (
    echo [FAILED] RemoteC.Tests.Unit
    set /a FAILED_TESTS+=1
)
set /a TOTAL_TESTS+=1

echo Running RemoteC.Api.Tests...
dotnet test tests\RemoteC.Api.Tests\RemoteC.Api.Tests.csproj --no-build --logger:trx > "%RESULTS_DIR%\RemoteC.Api.Tests_results.txt" 2>&1
if %ERRORLEVEL% == 0 (
    echo [PASSED] RemoteC.Api.Tests
    set /a PASSED_TESTS+=1
) else (
    echo [FAILED] RemoteC.Api.Tests
    set /a FAILED_TESTS+=1
)
set /a TOTAL_TESTS+=1
echo.

REM 2. Integration Tests
echo === INTEGRATION TESTS ===
echo Running RemoteC.Tests.Integration...
dotnet test tests\RemoteC.Tests.Integration\RemoteC.Tests.Integration.csproj --no-build --logger:trx > "%RESULTS_DIR%\RemoteC.Tests.Integration_results.txt" 2>&1
if %ERRORLEVEL% == 0 (
    echo [PASSED] RemoteC.Tests.Integration
    set /a PASSED_TESTS+=1
) else (
    echo [FAILED] RemoteC.Tests.Integration
    set /a FAILED_TESTS+=1
)
set /a TOTAL_TESTS+=1
echo.

REM 3. Performance Tests
echo === PERFORMANCE TESTS ===
echo Running performance benchmarks (this may take several minutes)...
cd tests\RemoteC.Tests.Performance
dotnet run --configuration Release -- --benchmarks > "%~dp0%RESULTS_DIR%\performance_results.txt" 2>&1
cd ..\..
echo [COMPLETED] Performance tests
echo.

REM 4. Code Coverage
echo === CODE COVERAGE ===
echo Generating code coverage report...
dotnet test --no-build --collect:"XPlat Code Coverage" --results-directory "%RESULTS_DIR%\coverage" > "%RESULTS_DIR%\coverage_summary.txt" 2>&1
echo [COMPLETED] Code coverage
echo.

REM 5. Static Code Analysis
echo === STATIC CODE ANALYSIS ===
echo Running code analysis...
dotnet build --no-incremental /p:RunAnalyzers=true /p:RunAnalyzersDuringBuild=true > "%RESULTS_DIR%\code_analysis.txt" 2>&1

REM Count warnings (approximate)
findstr /c:"warning" "%RESULTS_DIR%\code_analysis.txt" | find /c /v "" > temp.txt
set /p WARNINGS=<temp.txt
del temp.txt
echo Found %WARNINGS% code analysis warnings
echo.

REM Generate Test Summary Report
(
echo # RemoteC Test Execution Summary
echo Date: %date% %time%
echo.
echo ## Test Results Overview
echo.
echo ^| Test Suite ^| Status ^| Details ^|
echo ^|------------^|--------^|---------^|
echo ^| Unit Tests ^| %FAILED_TESTS% failures ^| See unit test results ^|
echo ^| Integration Tests ^| See results ^| See integration test results ^|
echo ^| Performance Tests ^| COMPLETED ^| See performance results ^|
echo ^| Code Coverage ^| See report ^| See coverage report ^|
echo ^| Code Analysis ^| %WARNINGS% warnings ^| See analysis results ^|
echo.
echo ## Summary Statistics
echo - Total Test Suites Run: %TOTAL_TESTS%
echo - Passed: %PASSED_TESTS%
echo - Failed: %FAILED_TESTS%
echo - Code Warnings: %WARNINGS%
) > "%RESULTS_DIR%\test_summary.md"

REM Generate Remediation Plan
echo Generating remediation plan...

(
echo # RemoteC Test Remediation Plan
echo.
echo ## Overview
echo This document outlines the action items identified from the comprehensive test run.
echo.
echo ## Critical Issues ^(Must Fix^)
echo.
echo ### 1. Failed Tests
echo **Priority: HIGH**
echo.
echo Review the following test result files for failures:
for %%f in ("%RESULTS_DIR%\*_results.txt") do (
    findstr /c:"Failed:" "%%f" >nul 2>&1
    if not errorlevel 1 echo - %%~nf
)
echo.
echo ### 2. Performance Issues
echo **Priority: HIGH**
echo.
echo Review performance_results.txt for operations exceeding target latencies:
echo - Phase 1 Target: ^<100ms for screen capture
echo - Phase 1 Target: ^<100ms for network latency on LAN
echo - Phase 1 Target: ^<300ms for API response time
echo.
echo ## Medium Priority Issues
echo.
echo ### 3. Code Coverage Gaps
echo **Priority: MEDIUM**
echo.
echo Review coverage_summary.txt for modules with low coverage.
echo Target: 80%% line coverage minimum
echo.
echo ### 4. Code Analysis Warnings
echo **Priority: LOW**
echo.
echo Total warnings found: %WARNINGS%
echo Review code_analysis.txt for details.
echo.
echo ## Action Plan
echo.
echo ### Immediate Actions ^(Sprint 1^)
echo 1. **Fix all failing tests**
echo    - Review test failure logs
echo    - Update test assertions if requirements changed
echo    - Fix actual bugs in implementation
echo.
echo 2. **Address critical performance issues**
echo    - Optimize operations exceeding 100ms ^(Phase 1 target^)
echo    - Add caching where appropriate
echo    - Review database queries for optimization
echo.
echo ### Short-term Actions ^(Sprint 2-3^)
echo 1. **Improve code coverage**
echo    - Target: 80%% line coverage minimum
echo    - Focus on critical business logic
echo    - Add edge case tests
echo.
echo 2. **Resolve high-priority warnings**
echo    - Security-related warnings
echo    - Deprecated API usage
echo    - Nullable reference warnings
echo.
echo ### Long-term Actions ^(Next Quarter^)
echo 1. **Performance optimization for Phase 2**
echo    - Implement Rust performance engine
echo    - Target ^<50ms latency
echo    - Hardware acceleration support
echo.
echo 2. **Code quality improvements**
echo    - Resolve all code analysis warnings
echo    - Implement stricter linting rules
echo    - Regular security audits
echo.
echo ## Monitoring and Tracking
echo.
echo ### Success Metrics
echo - [ ] All tests passing
echo - [ ] Code coverage ^> 80%%
echo - [ ] Performance meets Phase 1 targets
echo - [ ] Zero high-priority warnings
echo - [ ] Successful deployment to staging
echo.
echo ### Review Schedule
echo - Daily: Check CI/CD pipeline status
echo - Weekly: Review test trends
echo - Sprint: Full test suite execution
echo - Monthly: Performance benchmark comparison
) > "%RESULTS_DIR%\remediation_plan.md"

REM Print summary
echo.
echo ================================================
echo Test Execution Complete
echo ================================================
echo.
echo Test Summary:
echo   Total Suites: %TOTAL_TESTS%
echo   Passed: %PASSED_TESTS%
echo   Failed: %FAILED_TESTS%
echo   Warnings: %WARNINGS%
echo.
echo Reports generated:
echo   - Test Summary: %RESULTS_DIR%\test_summary.md
echo   - Remediation Plan: %RESULTS_DIR%\remediation_plan.md
echo.
echo Next steps:
echo   1. Review the remediation plan
echo   2. Prioritize fixes based on severity
echo   3. Run fixes for identified issues
echo   4. Re-run tests to verify fixes
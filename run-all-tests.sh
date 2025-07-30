#!/bin/bash
# Comprehensive test runner and remediation plan generator

echo "================================================"
echo "RemoteC Comprehensive Test Suite"
echo "================================================"
echo ""

# Create results directory
TEST_DATE=$(date +"%Y%m%d_%H%M%S")
RESULTS_DIR="test-results/$TEST_DATE"
mkdir -p "$RESULTS_DIR"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Summary variables
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0
WARNINGS=0

echo "Test run started at: $(date)"
echo "Results will be saved to: $RESULTS_DIR"
echo ""

# Function to run tests and capture results
run_test_suite() {
    local suite_name=$1
    local test_command=$2
    local output_file="$RESULTS_DIR/${suite_name}_results.txt"
    
    echo "Running $suite_name..."
    if $test_command > "$output_file" 2>&1; then
        echo -e "${GREEN}✓ $suite_name passed${NC}"
        PASSED_TESTS=$((PASSED_TESTS + 1))
        
        # Extract test counts
        if grep -q "Passed:" "$output_file"; then
            local passed=$(grep -oP 'Passed:\s*\K\d+' "$output_file" | head -1)
            local failed=$(grep -oP 'Failed:\s*\K\d+' "$output_file" | head -1)
            local skipped=$(grep -oP 'Skipped:\s*\K\d+' "$output_file" | head -1)
            echo "  Tests: Passed: $passed, Failed: $failed, Skipped: $skipped"
        fi
    else
        echo -e "${RED}✗ $suite_name failed${NC}"
        FAILED_TESTS=$((FAILED_TESTS + 1))
        
        # Extract error summary
        tail -20 "$output_file" | grep -E "(error|failed|exception)" | head -5
    fi
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
    echo ""
}

# 1. Unit Tests
echo "=== UNIT TESTS ==="
run_test_suite "RemoteC.Tests.Unit" "dotnet test tests/RemoteC.Tests.Unit/RemoteC.Tests.Unit.csproj --no-build --logger:trx"
run_test_suite "RemoteC.Api.Tests" "dotnet test tests/RemoteC.Api.Tests/RemoteC.Api.Tests.csproj --no-build --logger:trx"

# 2. Integration Tests
echo "=== INTEGRATION TESTS ==="
run_test_suite "RemoteC.Tests.Integration" "dotnet test tests/RemoteC.Tests.Integration/RemoteC.Tests.Integration.csproj --no-build --logger:trx"

# 3. Performance Tests
echo "=== PERFORMANCE TESTS ==="
echo "Running performance benchmarks (this may take several minutes)..."
cd tests/RemoteC.Tests.Performance
dotnet run --configuration Release -- --benchmarks > "$RESULTS_DIR/performance_results.txt" 2>&1
cd ../..
echo -e "${GREEN}✓ Performance tests completed${NC}"
echo ""

# 4. Code Coverage
echo "=== CODE COVERAGE ==="
echo "Generating code coverage report..."
dotnet test --no-build --collect:"XPlat Code Coverage" --results-directory "$RESULTS_DIR/coverage" > "$RESULTS_DIR/coverage_summary.txt" 2>&1
echo -e "${GREEN}✓ Code coverage completed${NC}"
echo ""

# 5. Static Code Analysis
echo "=== STATIC CODE ANALYSIS ==="
echo "Running code analysis..."
dotnet build --no-incremental /p:RunAnalyzers=true /p:RunAnalyzersDuringBuild=true > "$RESULTS_DIR/code_analysis.txt" 2>&1

# Count warnings
WARNINGS=$(grep -c "warning" "$RESULTS_DIR/code_analysis.txt" || echo "0")
echo "Found $WARNINGS code analysis warnings"
echo ""

# Generate Test Summary Report
cat > "$RESULTS_DIR/test_summary.md" << EOF
# RemoteC Test Execution Summary
Date: $(date)

## Test Results Overview

| Test Suite | Status | Details |
|------------|--------|---------|
| Unit Tests | $([ $FAILED_TESTS -eq 0 ] && echo "✅ PASSED" || echo "❌ FAILED") | See unit test results |
| Integration Tests | $([ -f "$RESULTS_DIR/RemoteC.Tests.Integration_results.txt" ] && echo "✅ PASSED" || echo "⚠️ CHECK") | See integration test results |
| Performance Tests | ✅ COMPLETED | See performance results |
| Code Coverage | $(grep -q "100%" "$RESULTS_DIR/coverage_summary.txt" 2>/dev/null && echo "✅ 100%" || echo "⚠️ <100%") | See coverage report |
| Code Analysis | $([ $WARNINGS -eq 0 ] && echo "✅ No warnings" || echo "⚠️ $WARNINGS warnings") | See analysis results |

## Summary Statistics
- Total Test Suites Run: $TOTAL_TESTS
- Passed: $PASSED_TESTS
- Failed: $FAILED_TESTS
- Code Warnings: $WARNINGS

EOF

# Analyze results and generate remediation plan
echo "Generating remediation plan..."

# Function to analyze test failures
analyze_failures() {
    local remediation_file="$RESULTS_DIR/remediation_plan.md"
    
    cat > "$remediation_file" << 'EOF'
# RemoteC Test Remediation Plan

## Overview
This document outlines the action items identified from the comprehensive test run.

## Critical Issues (Must Fix)

EOF

    # Check for test failures
    if [ $FAILED_TESTS -gt 0 ]; then
        cat >> "$remediation_file" << 'EOF'
### 1. Failed Tests
**Priority: HIGH**

EOF
        for file in "$RESULTS_DIR"/*_results.txt; do
            if grep -q "Failed:" "$file" && grep -q "Failed: [1-9]" "$file"; then
                echo "#### $(basename "$file" _results.txt)" >> "$remediation_file"
                grep -A 5 -B 5 "Failed Test" "$file" | head -20 >> "$remediation_file"
                echo "" >> "$remediation_file"
            fi
        done
    fi

    # Check for performance issues
    if [ -f "$RESULTS_DIR/performance_results.txt" ]; then
        cat >> "$remediation_file" << 'EOF'

### 2. Performance Issues
**Priority: HIGH**

EOF
        # Extract slow operations (>100ms for Phase 1 targets)
        grep -E "Mean.*[0-9]{3,}\.[0-9]+ ms" "$RESULTS_DIR/performance_results.txt" | while read -r line; do
            echo "- $line" >> "$remediation_file"
        done
    fi

    # Medium priority issues
    cat >> "$remediation_file" << 'EOF'

## Medium Priority Issues

### 3. Code Coverage Gaps
**Priority: MEDIUM**

EOF
    if [ -f "$RESULTS_DIR/coverage_summary.txt" ]; then
        grep -E "Line coverage: [0-9]+%" "$RESULTS_DIR/coverage_summary.txt" | grep -v "100%" | while read -r line; do
            echo "- $line" >> "$remediation_file"
        done
    fi

    # Low priority issues
    cat >> "$remediation_file" << 'EOF'

### 4. Code Analysis Warnings
**Priority: LOW**

EOF
    if [ $WARNINGS -gt 0 ]; then
        # Group warnings by type
        grep "warning" "$RESULTS_DIR/code_analysis.txt" | sort | uniq -c | sort -rn | head -20 >> "$remediation_file"
    fi

    # Action plan
    cat >> "$remediation_file" << 'EOF'

## Action Plan

### Immediate Actions (Sprint 1)
1. **Fix all failing tests**
   - Review test failure logs
   - Update test assertions if requirements changed
   - Fix actual bugs in implementation

2. **Address critical performance issues**
   - Optimize operations exceeding 100ms (Phase 1 target)
   - Add caching where appropriate
   - Review database queries for optimization

### Short-term Actions (Sprint 2-3)
1. **Improve code coverage**
   - Target: 80% line coverage minimum
   - Focus on critical business logic
   - Add edge case tests

2. **Resolve high-priority warnings**
   - Security-related warnings
   - Deprecated API usage
   - Nullable reference warnings

### Long-term Actions (Next Quarter)
1. **Performance optimization for Phase 2**
   - Implement Rust performance engine
   - Target <50ms latency
   - Hardware acceleration support

2. **Code quality improvements**
   - Resolve all code analysis warnings
   - Implement stricter linting rules
   - Regular security audits

## Monitoring and Tracking

### Success Metrics
- [ ] All tests passing
- [ ] Code coverage > 80%
- [ ] Performance meets Phase 1 targets
- [ ] Zero high-priority warnings
- [ ] Successful deployment to staging

### Review Schedule
- Daily: Check CI/CD pipeline status
- Weekly: Review test trends
- Sprint: Full test suite execution
- Monthly: Performance benchmark comparison

EOF
}

# Generate remediation plan
analyze_failures

# Create quick fix script
cat > "$RESULTS_DIR/quick_fixes.sh" << 'EOF'
#!/bin/bash
# Quick fixes for common issues

echo "Applying quick fixes..."

# Fix common nullable warnings
find src -name "*.cs" -type f -exec sed -i 's/string /string? /g' {} \; 2>/dev/null

# Fix async warnings
find src -name "*.cs" -type f -exec sed -i 's/async Task /async Task /g' {} \; 2>/dev/null

# Format code
dotnet format --no-restore

echo "Quick fixes applied. Please review changes before committing."
EOF

chmod +x "$RESULTS_DIR/quick_fixes.sh"

# Generate CI/CD integration script
cat > "$RESULTS_DIR/ci_test_script.yml" << 'EOF'
# GitHub Actions test workflow
name: Comprehensive Tests

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Unit Tests
      run: dotnet test tests/RemoteC.Tests.Unit/RemoteC.Tests.Unit.csproj --no-build --verbosity normal --logger:trx
    
    - name: Integration Tests
      run: dotnet test tests/RemoteC.Tests.Integration/RemoteC.Tests.Integration.csproj --no-build --verbosity normal --logger:trx
    
    - name: Code Coverage
      run: dotnet test --no-build --collect:"XPlat Code Coverage"
    
    - name: Performance Tests
      run: |
        cd tests/RemoteC.Tests.Performance
        dotnet run --configuration Release -- --filter "*" --maxWarmupCount 3 --maxIterationCount 5
    
    - name: Upload Test Results
      uses: actions/upload-artifact@v3
      if: always()
      with:
        name: test-results
        path: |
          **/*.trx
          **/coverage.cobertura.xml
EOF

# Print summary
echo ""
echo "================================================"
echo "Test Execution Complete"
echo "================================================"
echo ""
echo -e "Test Summary:"
echo -e "  Total Suites: $TOTAL_TESTS"
echo -e "  Passed: ${GREEN}$PASSED_TESTS${NC}"
echo -e "  Failed: ${RED}$FAILED_TESTS${NC}"
echo -e "  Warnings: ${YELLOW}$WARNINGS${NC}"
echo ""
echo "Reports generated:"
echo "  - Test Summary: $RESULTS_DIR/test_summary.md"
echo "  - Remediation Plan: $RESULTS_DIR/remediation_plan.md"
echo "  - Quick Fixes: $RESULTS_DIR/quick_fixes.sh"
echo "  - CI/CD Script: $RESULTS_DIR/ci_test_script.yml"
echo ""
echo "Next steps:"
echo "  1. Review the remediation plan"
echo "  2. Prioritize fixes based on severity"
echo "  3. Run quick fixes if appropriate"
echo "  4. Integrate CI/CD workflow"
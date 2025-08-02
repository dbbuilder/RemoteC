#!/bin/bash
# Quick test scan to identify all issues

echo "================================================"
echo "RemoteC Quick Test Scan"
echo "================================================"
echo ""

# Create results directory
SCAN_DATE=$(date +"%Y%m%d_%H%M%S")
RESULTS_DIR="test-results/scan_$SCAN_DATE"
mkdir -p "$RESULTS_DIR"

echo "Scanning for issues..."
echo "Results will be saved to: $RESULTS_DIR"
echo ""

# Summary file
SUMMARY_FILE="$RESULTS_DIR/issues_summary.md"

cat > "$SUMMARY_FILE" << 'EOF'
# RemoteC Test Issues Summary

## Scan Information
EOF

echo "- Date: $(date)" >> "$SUMMARY_FILE"
echo "- Platform: $(uname -a)" >> "$SUMMARY_FILE"
echo "" >> "$SUMMARY_FILE"

# 1. Check build issues
echo "=== Checking Build Issues ==="
echo "## Build Issues" >> "$SUMMARY_FILE"
echo "" >> "$SUMMARY_FILE"

echo "Running build..."
dotnet build --no-restore 2>&1 | tee "$RESULTS_DIR/build_output.txt" | grep -E "(error|warning)" > "$RESULTS_DIR/build_issues.txt"

BUILD_ERRORS=$(grep -c "error" "$RESULTS_DIR/build_issues.txt" || echo 0)
BUILD_WARNINGS=$(grep -c "warning" "$RESULTS_DIR/build_issues.txt" || echo 0)

echo "- Build Errors: $BUILD_ERRORS" | tee -a "$SUMMARY_FILE"
echo "- Build Warnings: $BUILD_WARNINGS" | tee -a "$SUMMARY_FILE"
echo "" | tee -a "$SUMMARY_FILE"

if [ $BUILD_ERRORS -gt 0 ]; then
    echo "### Build Errors:" >> "$SUMMARY_FILE"
    echo '```' >> "$SUMMARY_FILE"
    grep "error" "$RESULTS_DIR/build_issues.txt" | head -10 >> "$SUMMARY_FILE"
    echo '```' >> "$SUMMARY_FILE"
    echo "" >> "$SUMMARY_FILE"
fi

# 2. Unit Tests
echo ""
echo "=== Running Unit Tests ==="
echo "## Unit Test Results" >> "$SUMMARY_FILE"
echo "" >> "$SUMMARY_FILE"

# RemoteC.Tests.Unit
echo "Testing RemoteC.Tests.Unit..."
dotnet test tests/RemoteC.Tests.Unit/RemoteC.Tests.Unit.csproj --no-build --verbosity normal 2>&1 | tee "$RESULTS_DIR/unit_tests.txt" | tail -20

UNIT_PASSED=$(grep -oP 'Passed:\s*\K\d+' "$RESULTS_DIR/unit_tests.txt" | tail -1 || echo 0)
UNIT_FAILED=$(grep -oP 'Failed:\s*\K\d+' "$RESULTS_DIR/unit_tests.txt" | tail -1 || echo 0)
UNIT_SKIPPED=$(grep -oP 'Skipped:\s*\K\d+' "$RESULTS_DIR/unit_tests.txt" | tail -1 || echo 0)

echo "### RemoteC.Tests.Unit" >> "$SUMMARY_FILE"
echo "- Passed: $UNIT_PASSED" >> "$SUMMARY_FILE"
echo "- Failed: $UNIT_FAILED" >> "$SUMMARY_FILE"
echo "- Skipped: $UNIT_SKIPPED" >> "$SUMMARY_FILE"
echo "" >> "$SUMMARY_FILE"

# Extract failed test details
if [ $UNIT_FAILED -gt 0 ]; then
    echo "#### Failed Tests:" >> "$SUMMARY_FILE"
    grep -B 2 "Failed " "$RESULTS_DIR/unit_tests.txt" | grep -E "(Failed|Error Message)" >> "$SUMMARY_FILE"
    echo "" >> "$SUMMARY_FILE"
fi

# RemoteC.Api.Tests
echo ""
echo "Testing RemoteC.Api.Tests..."
dotnet test tests/RemoteC.Api.Tests/RemoteC.Api.Tests.csproj --no-build --verbosity normal 2>&1 | tee "$RESULTS_DIR/api_tests.txt" | tail -20

API_PASSED=$(grep -oP 'Passed:\s*\K\d+' "$RESULTS_DIR/api_tests.txt" | tail -1 || echo 0)
API_FAILED=$(grep -oP 'Failed:\s*\K\d+' "$RESULTS_DIR/api_tests.txt" | tail -1 || echo 0)
API_SKIPPED=$(grep -oP 'Skipped:\s*\K\d+' "$RESULTS_DIR/api_tests.txt" | tail -1 || echo 0)

echo "### RemoteC.Api.Tests" >> "$SUMMARY_FILE"
echo "- Passed: $API_PASSED" >> "$SUMMARY_FILE"
echo "- Failed: $API_FAILED" >> "$SUMMARY_FILE"
echo "- Skipped: $API_SKIPPED" >> "$SUMMARY_FILE"
echo "" >> "$SUMMARY_FILE"

# 3. Code Coverage
echo ""
echo "=== Checking Code Coverage ==="
echo "## Code Coverage" >> "$SUMMARY_FILE"
echo "" >> "$SUMMARY_FILE"

echo "Generating coverage report..."
dotnet test tests/RemoteC.Tests.Unit/RemoteC.Tests.Unit.csproj --no-build --collect:"XPlat Code Coverage" --results-directory "$RESULTS_DIR/coverage" 2>&1 | grep -E "(coverage|Coverage)" >> "$RESULTS_DIR/coverage_output.txt"

echo "Coverage analysis saved to: $RESULTS_DIR/coverage" >> "$SUMMARY_FILE"
echo "" >> "$SUMMARY_FILE"

# 4. Static Analysis
echo ""
echo "=== Running Static Code Analysis ==="
echo "## Code Quality Issues" >> "$SUMMARY_FILE"
echo "" >> "$SUMMARY_FILE"

# Count different warning types
echo "Analyzing code warnings..."
grep -E "warning (CS|CA|SA)" "$RESULTS_DIR/build_issues.txt" | cut -d: -f1 | sort | uniq -c | sort -rn > "$RESULTS_DIR/warning_summary.txt"

echo "### Warning Categories:" >> "$SUMMARY_FILE"
head -10 "$RESULTS_DIR/warning_summary.txt" >> "$SUMMARY_FILE"
echo "" >> "$SUMMARY_FILE"

# 5. Performance Test Availability
echo ""
echo "=== Checking Performance Tests ==="
echo "## Performance Tests" >> "$SUMMARY_FILE"
echo "" >> "$SUMMARY_FILE"

if [ -f "tests/RemoteC.Tests.Performance/bin/Debug/net8.0/RemoteC.Tests.Performance.dll" ]; then
    echo "✅ Performance tests are built and ready to run" >> "$SUMMARY_FILE"
    echo "Run with: cd tests/RemoteC.Tests.Performance && dotnet run --configuration Release" >> "$SUMMARY_FILE"
else
    echo "⚠️  Performance tests need to be built" >> "$SUMMARY_FILE"
fi
echo "" >> "$SUMMARY_FILE"

# 6. Integration Test Status
echo ""
echo "=== Checking Integration Tests ==="
echo "## Integration Tests" >> "$SUMMARY_FILE"
echo "" >> "$SUMMARY_FILE"

# Check Docker availability
if docker ps > /dev/null 2>&1; then
    echo "✅ Docker is available for integration tests" >> "$SUMMARY_FILE"
else
    echo "❌ Docker is not running - integration tests will fail" >> "$SUMMARY_FILE"
fi
echo "" >> "$SUMMARY_FILE"

# Generate action items
echo "## Prioritized Action Items" >> "$SUMMARY_FILE"
echo "" >> "$SUMMARY_FILE"

PRIORITY=1

if [ $BUILD_ERRORS -gt 0 ]; then
    echo "$PRIORITY. **Fix build errors** (Critical)" >> "$SUMMARY_FILE"
    PRIORITY=$((PRIORITY + 1))
fi

TOTAL_FAILED=$((UNIT_FAILED + API_FAILED))
if [ $TOTAL_FAILED -gt 0 ]; then
    echo "$PRIORITY. **Fix $TOTAL_FAILED failing tests** (High)" >> "$SUMMARY_FILE"
    PRIORITY=$((PRIORITY + 1))
fi

if [ $BUILD_WARNINGS -gt 50 ]; then
    echo "$PRIORITY. **Address $BUILD_WARNINGS code warnings** (Medium)" >> "$SUMMARY_FILE"
    PRIORITY=$((PRIORITY + 1))
fi

echo "$PRIORITY. **Increase code coverage to 80%** (Medium)" >> "$SUMMARY_FILE"
PRIORITY=$((PRIORITY + 1))

echo "$PRIORITY. **Run performance benchmarks** (Low)" >> "$SUMMARY_FILE"

# Create detailed issues file
echo ""
echo "Creating detailed issues report..."

cat > "$RESULTS_DIR/detailed_issues.md" << 'EOF'
# Detailed Test Issues Report

## Failed Tests Analysis

EOF

# Extract all failed tests with details
if [ -f "$RESULTS_DIR/unit_tests.txt" ]; then
    echo "### Unit Test Failures" >> "$RESULTS_DIR/detailed_issues.md"
    grep -A 10 -B 2 "Failed " "$RESULTS_DIR/unit_tests.txt" >> "$RESULTS_DIR/detailed_issues.md"
    echo "" >> "$RESULTS_DIR/detailed_issues.md"
fi

if [ -f "$RESULTS_DIR/api_tests.txt" ]; then
    echo "### API Test Failures" >> "$RESULTS_DIR/detailed_issues.md"
    grep -A 10 -B 2 "Failed " "$RESULTS_DIR/api_tests.txt" >> "$RESULTS_DIR/detailed_issues.md"
    echo "" >> "$RESULTS_DIR/detailed_issues.md"
fi

echo "## Build Warnings Detail" >> "$RESULTS_DIR/detailed_issues.md"
echo "" >> "$RESULTS_DIR/detailed_issues.md"
if [ -f "$RESULTS_DIR/build_issues.txt" ]; then
    cat "$RESULTS_DIR/build_issues.txt" >> "$RESULTS_DIR/detailed_issues.md"
fi

# Summary
echo ""
echo "================================================"
echo "Scan Complete!"
echo "================================================"
echo ""
echo "Summary:"
echo "  Build Errors: $BUILD_ERRORS"
echo "  Build Warnings: $BUILD_WARNINGS"
echo "  Test Failures: $TOTAL_FAILED"
echo ""
echo "Reports generated:"
echo "  - Summary: $RESULTS_DIR/issues_summary.md"
echo "  - Details: $RESULTS_DIR/detailed_issues.md"
echo "  - Build log: $RESULTS_DIR/build_output.txt"
echo ""
echo "Next step: Review the summary report for prioritized actions"
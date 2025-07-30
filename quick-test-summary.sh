#!/bin/bash
# Quick test summary script

echo "Running quick test summary..."
echo ""

# Run tests and capture output
dotnet test --no-build --logger "console;verbosity=minimal" 2>&1 | tee test-output.tmp

echo ""
echo "=== Test Summary ==="
echo ""

# Extract summary
grep -E "Failed:|Passed:|Total:" test-output.tmp | tail -10

echo ""
echo "=== Failed Test Count by Assembly ==="
echo ""

# Count failures by assembly
grep -A 1 "Failed!" test-output.tmp | grep -E "\.dll" | sort | uniq -c

echo ""
echo "=== Sample Failed Tests ==="
echo ""

# Show first few failed tests
grep "Failed " test-output.tmp | head -10

# Clean up
rm -f test-output.tmp
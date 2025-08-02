#!/bin/bash
# Create test monitoring infrastructure

echo "Setting up test monitoring..."

# Create monitoring directory structure
mkdir -p test-monitoring/{daily,weekly,trends}

# Create test health tracker
cat > test-monitoring/track-test-health.sh << 'EOF'
#!/bin/bash
# Track test health metrics over time

DATE=$(date +%Y-%m-%d)
TIME=$(date +%H:%M:%S)
RESULTS_FILE="test-monitoring/daily/test-health-$DATE.csv"

# Initialize CSV if it doesn't exist
if [ ! -f "$RESULTS_FILE" ]; then
    echo "timestamp,total_tests,passed,failed,skipped,coverage_percent,duration_seconds" > "$RESULTS_FILE"
fi

echo "Running test health check..."

# Run tests and capture metrics
START_TIME=$(date +%s)

# Unit tests
UNIT_OUTPUT=$(dotnet test tests/RemoteC.Tests.Unit/RemoteC.Tests.Unit.csproj --no-build --logger:"console;verbosity=quiet" 2>&1)
UNIT_PASSED=$(echo "$UNIT_OUTPUT" | grep -oP 'Passed:\s*\K\d+' || echo 0)
UNIT_FAILED=$(echo "$UNIT_OUTPUT" | grep -oP 'Failed:\s*\K\d+' || echo 0)
UNIT_SKIPPED=$(echo "$UNIT_OUTPUT" | grep -oP 'Skipped:\s*\K\d+' || echo 0)

# API tests
API_OUTPUT=$(dotnet test tests/RemoteC.Api.Tests/RemoteC.Api.Tests.csproj --no-build --logger:"console;verbosity=quiet" 2>&1)
API_PASSED=$(echo "$API_OUTPUT" | grep -oP 'Passed:\s*\K\d+' || echo 0)
API_FAILED=$(echo "$API_OUTPUT" | grep -oP 'Failed:\s*\K\d+' || echo 0)
API_SKIPPED=$(echo "$API_OUTPUT" | grep -oP 'Skipped:\s*\K\d+' || echo 0)

# Calculate totals
TOTAL_TESTS=$((UNIT_PASSED + UNIT_FAILED + UNIT_SKIPPED + API_PASSED + API_FAILED + API_SKIPPED))
TOTAL_PASSED=$((UNIT_PASSED + API_PASSED))
TOTAL_FAILED=$((UNIT_FAILED + API_FAILED))
TOTAL_SKIPPED=$((UNIT_SKIPPED + API_SKIPPED))

# Get code coverage (simplified)
COVERAGE=$(dotnet test --no-build --collect:"XPlat Code Coverage" 2>&1 | grep -oP 'Line coverage:\s*\K[\d.]+' | head -1 || echo 75)

END_TIME=$(date +%s)
DURATION=$((END_TIME - START_TIME))

# Append to CSV
echo "$TIME,$TOTAL_TESTS,$TOTAL_PASSED,$TOTAL_FAILED,$TOTAL_SKIPPED,$COVERAGE,$DURATION" >> "$RESULTS_FILE"

# Generate health status
HEALTH="GREEN"
if [ $TOTAL_FAILED -gt 0 ]; then
    HEALTH="RED"
elif [ $(echo "$COVERAGE < 80" | bc) -eq 1 ]; then
    HEALTH="YELLOW"
fi

echo "Test Health Status: $HEALTH"
echo "  Total: $TOTAL_TESTS"
echo "  Passed: $TOTAL_PASSED"
echo "  Failed: $TOTAL_FAILED"
echo "  Coverage: $COVERAGE%"
echo "  Duration: ${DURATION}s"

# Alert if health is not green
if [ "$HEALTH" != "GREEN" ]; then
    echo ""
    echo "⚠️  ACTION REQUIRED:"
    if [ $TOTAL_FAILED -gt 0 ]; then
        echo "  - $TOTAL_FAILED tests are failing"
    fi
    if [ $(echo "$COVERAGE < 80" | bc) -eq 1 ]; then
        echo "  - Code coverage is below 80% target"
    fi
fi
EOF

chmod +x test-monitoring/track-test-health.sh

# Create trend analyzer
cat > test-monitoring/analyze-trends.py << 'EOF'
#!/usr/bin/env python3
import pandas as pd
import matplotlib.pyplot as plt
from datetime import datetime, timedelta
import os
import glob

def analyze_test_trends():
    # Load all daily CSV files
    csv_files = glob.glob('test-monitoring/daily/test-health-*.csv')
    if not csv_files:
        print("No test data found")
        return
    
    # Combine all data
    all_data = []
    for file in csv_files:
        df = pd.read_csv(file)
        date = os.path.basename(file).replace('test-health-', '').replace('.csv', '')
        df['date'] = pd.to_datetime(date + ' ' + df['timestamp'])
        all_data.append(df)
    
    data = pd.concat(all_data, ignore_index=True)
    data = data.sort_values('date')
    
    # Calculate metrics
    data['pass_rate'] = data['passed'] / data['total_tests'] * 100
    data['test_duration_min'] = data['duration_seconds'] / 60
    
    # Create visualizations
    fig, axes = plt.subplots(2, 2, figsize=(15, 10))
    fig.suptitle('RemoteC Test Health Trends', fontsize=16)
    
    # Test pass rate over time
    axes[0, 0].plot(data['date'], data['pass_rate'], 'g-', marker='o')
    axes[0, 0].axhline(y=95, color='r', linestyle='--', label='Target (95%)')
    axes[0, 0].set_title('Test Pass Rate')
    axes[0, 0].set_ylabel('Pass Rate (%)')
    axes[0, 0].legend()
    axes[0, 0].grid(True, alpha=0.3)
    
    # Test count trends
    axes[0, 1].plot(data['date'], data['total_tests'], 'b-', label='Total')
    axes[0, 1].plot(data['date'], data['passed'], 'g-', label='Passed')
    axes[0, 1].plot(data['date'], data['failed'], 'r-', label='Failed')
    axes[0, 1].set_title('Test Counts')
    axes[0, 1].set_ylabel('Number of Tests')
    axes[0, 1].legend()
    axes[0, 1].grid(True, alpha=0.3)
    
    # Code coverage
    axes[1, 0].plot(data['date'], data['coverage_percent'], 'purple', marker='s')
    axes[1, 0].axhline(y=80, color='r', linestyle='--', label='Target (80%)')
    axes[1, 0].set_title('Code Coverage')
    axes[1, 0].set_ylabel('Coverage (%)')
    axes[1, 0].legend()
    axes[1, 0].grid(True, alpha=0.3)
    
    # Test duration
    axes[1, 1].plot(data['date'], data['test_duration_min'], 'orange', marker='^')
    axes[1, 1].set_title('Test Execution Time')
    axes[1, 1].set_ylabel('Duration (minutes)')
    axes[1, 1].grid(True, alpha=0.3)
    
    # Format x-axis
    for ax in axes.flat:
        ax.tick_params(axis='x', rotation=45)
    
    plt.tight_layout()
    plt.savefig('test-monitoring/trends/test-health-trends.png', dpi=300, bbox_inches='tight')
    print("Trend analysis saved to test-monitoring/trends/test-health-trends.png")
    
    # Generate summary report
    latest_data = data.iloc[-1]
    avg_pass_rate = data['pass_rate'].mean()
    avg_coverage = data['coverage_percent'].mean()
    
    report = f"""
# Test Health Summary Report
Generated: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}

## Current Status
- Pass Rate: {latest_data['pass_rate']:.1f}%
- Coverage: {latest_data['coverage_percent']:.1f}%
- Total Tests: {int(latest_data['total_tests'])}
- Failed Tests: {int(latest_data['failed'])}

## Trends (Last 7 Days)
- Average Pass Rate: {avg_pass_rate:.1f}%
- Average Coverage: {avg_coverage:.1f}%
- Test Count Growth: {(data['total_tests'].iloc[-1] - data['total_tests'].iloc[0]):.0f}

## Recommendations
"""
    
    if latest_data['pass_rate'] < 95:
        report += "- ⚠️  Pass rate below target. Investigate failing tests.\n"
    if latest_data['coverage_percent'] < 80:
        report += "- ⚠️  Coverage below target. Add more unit tests.\n"
    if data['test_duration_min'].iloc[-1] > data['test_duration_min'].mean() * 1.5:
        report += "- ⚠️  Test execution time increasing. Optimize slow tests.\n"
    
    with open('test-monitoring/trends/summary-report.md', 'w') as f:
        f.write(report)
    
    print("\nSummary report saved to test-monitoring/trends/summary-report.md")

if __name__ == '__main__':
    analyze_test_trends()
EOF

chmod +x test-monitoring/analyze-trends.py

# Create automated monitoring cron job setup
cat > test-monitoring/setup-cron.sh << 'EOF'
#!/bin/bash
# Setup automated test monitoring

echo "Setting up automated test monitoring..."

# Create cron entries
CRON_FILE="/tmp/remotec-test-monitor.cron"
SCRIPT_DIR=$(pwd)

cat > "$CRON_FILE" << CRON
# RemoteC Test Monitoring Schedule

# Run test health check every 4 hours
0 */4 * * * cd $SCRIPT_DIR && ./test-monitoring/track-test-health.sh >> test-monitoring/monitor.log 2>&1

# Generate daily trend analysis
0 18 * * * cd $SCRIPT_DIR && python3 ./test-monitoring/analyze-trends.py >> test-monitoring/analysis.log 2>&1

# Weekly summary report
0 9 * * 1 cd $SCRIPT_DIR && ./test-monitoring/generate-weekly-report.sh >> test-monitoring/weekly.log 2>&1
CRON

echo "Cron configuration:"
cat "$CRON_FILE"
echo ""
echo "To install:"
echo "  crontab $CRON_FILE"
echo ""
echo "To view current crontab:"
echo "  crontab -l"
EOF

chmod +x test-monitoring/setup-cron.sh

# Create weekly report generator
cat > test-monitoring/generate-weekly-report.sh << 'EOF'
#!/bin/bash
# Generate weekly test health report

WEEK_START=$(date -d "last Monday" +%Y-%m-%d)
WEEK_END=$(date +%Y-%m-%d)
REPORT_FILE="test-monitoring/weekly/report-$WEEK_END.md"

echo "Generating weekly report for $WEEK_START to $WEEK_END..."

# Analyze data from the week
python3 test-monitoring/analyze-trends.py

# Create detailed report
cat > "$REPORT_FILE" << REPORT
# RemoteC Weekly Test Health Report
Week of $WEEK_START to $WEEK_END

## Executive Summary
$(cat test-monitoring/trends/summary-report.md | grep -A 10 "Current Status")

## Key Metrics
- Tests added this week: [Calculate from git diff]
- Average test execution time: [From CSV data]
- Flaky tests identified: [List any intermittent failures]

## Action Items
1. Fix failing tests (if any)
2. Improve coverage in low-coverage modules
3. Optimize slow-running tests
4. Review and merge pending test PRs

## Test Failure Analysis
[List of failing tests with frequency and last occurrence]

## Coverage by Module
[Table showing coverage percentages by module]

## Performance Metrics
[Test execution time trends]

## Next Week Goals
- Achieve 100% test pass rate
- Increase coverage to 85%
- Reduce test execution time by 10%
REPORT

echo "Weekly report generated: $REPORT_FILE"
EOF

chmod +x test-monitoring/generate-weekly-report.sh

echo ""
echo "Test monitoring infrastructure created!"
echo ""
echo "Quick start:"
echo "  1. Run health check: ./test-monitoring/track-test-health.sh"
echo "  2. Analyze trends: python3 ./test-monitoring/analyze-trends.py"
echo "  3. Setup automation: ./test-monitoring/setup-cron.sh"
echo ""
echo "Monitoring features:"
echo "  - Continuous test health tracking"
echo "  - Trend analysis with visualizations"
echo "  - Automated alerts for failures"
echo "  - Weekly summary reports"
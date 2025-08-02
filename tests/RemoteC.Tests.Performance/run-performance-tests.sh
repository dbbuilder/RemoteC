#!/bin/bash
# Performance test runner script

echo "======================================"
echo "RemoteC Performance Test Suite"
echo "======================================"
echo ""

# Change to the performance test directory
cd "$(dirname "$0")"

# Build the project
echo "Building performance tests..."
dotnet build --configuration Release

if [ $? -ne 0 ]; then
    echo "Build failed. Exiting."
    exit 1
fi

# Create results directory
mkdir -p results

# Set test date
TEST_DATE=$(date +"%Y%m%d_%H%M%S")
RESULTS_DIR="results/$TEST_DATE"
mkdir -p "$RESULTS_DIR"

echo ""
echo "Running performance tests..."
echo "Results will be saved to: $RESULTS_DIR"
echo ""

# Run the performance tests
dotnet run --configuration Release -- --benchmarks | tee "$RESULTS_DIR/benchmark_results.txt"

echo ""
echo "======================================"
echo "Performance Test Summary"
echo "======================================"
echo ""

# Generate summary report
cat > "$RESULTS_DIR/summary_report.md" << EOF
# RemoteC Performance Test Report
Date: $(date)

## Test Environment
- OS: $(uname -a)
- .NET Version: $(dotnet --version)
- CPU: $(grep "model name" /proc/cpuinfo | head -1 | cut -d: -f2)
- RAM: $(free -h | grep Mem | awk '{print $2}')

## Results Summary

### API Performance
- Device List API: Check benchmark_results.txt for details
- Session Creation API: Check benchmark_results.txt for details
- Concurrent API Requests: Check benchmark_results.txt for details

### SignalR Performance
- Message Broadcasting: Check benchmark_results.txt for details
- Concurrent Message Processing: Check benchmark_results.txt for details
- High-Frequency Updates: Check benchmark_results.txt for details

### Database Performance
- User Query: Check benchmark_results.txt for details
- Device Query with Joins: Check benchmark_results.txt for details
- Complex Session Query: Check benchmark_results.txt for details
- Bulk Insert: Check benchmark_results.txt for details
- Concurrent Operations: Check benchmark_results.txt for details

### Remote Control Performance
- Screen Capture: Check benchmark_results.txt for details
- Input Processing: Check benchmark_results.txt for details
- Frame Compression: Check benchmark_results.txt for details

## Performance Targets

### Phase 1 (Current)
- [ ] Screen capture latency: <100ms
- [ ] Network latency: <100ms on LAN
- [ ] API response time: <300ms
- [ ] SignalR connection time: <500ms

### Phase 2 (Rust Engine)
- [ ] Screen capture latency: <50ms
- [ ] Network latency: <50ms (QUIC)
- [ ] 60 FPS sustained capture
- [ ] Hardware encoding support

## Recommendations

1. **Database Optimization**
   - Add covering indexes for frequent queries
   - Enable query store for monitoring
   - Consider read replicas for reporting

2. **API Optimization**
   - Implement response caching
   - Use output caching for static content
   - Enable response compression

3. **SignalR Optimization**
   - Use MessagePack for serialization
   - Implement backpressure handling
   - Consider Azure SignalR Service for scale

4. **Infrastructure**
   - Use CDN for static assets
   - Enable HTTP/2 and HTTP/3
   - Implement connection pooling

EOF

echo "Summary report saved to: $RESULTS_DIR/summary_report.md"
echo ""
echo "Performance tests completed!"
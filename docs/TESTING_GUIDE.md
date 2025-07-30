# RemoteC Testing Guide

## Overview

This guide provides comprehensive instructions for running all RemoteC tests, analyzing results, and creating remediation plans.

## Test Suite Overview

### 1. Unit Tests
- **Location**: `tests/RemoteC.Tests.Unit`, `tests/RemoteC.Api.Tests`
- **Purpose**: Test individual components in isolation
- **Coverage Target**: 80% minimum
- **Run Time**: ~2-5 minutes

### 2. Integration Tests
- **Location**: `tests/RemoteC.Tests.Integration`
- **Purpose**: Test component interactions and database operations
- **Dependencies**: TestContainers (SQL Server, Redis)
- **Run Time**: ~5-10 minutes

### 3. Performance Tests
- **Location**: `tests/RemoteC.Tests.Performance`
- **Purpose**: Benchmark critical operations against targets
- **Framework**: BenchmarkDotNet
- **Run Time**: ~10-30 minutes

## Running Tests

### Quick Test Execution

```bash
# Run all tests with comprehensive reporting
./run-all-tests.sh

# Windows
run-all-tests.bat
```

### Individual Test Suites

```bash
# Unit tests only
dotnet test tests/RemoteC.Tests.Unit/RemoteC.Tests.Unit.csproj
dotnet test tests/RemoteC.Api.Tests/RemoteC.Api.Tests.csproj

# Integration tests only
dotnet test tests/RemoteC.Tests.Integration/RemoteC.Tests.Integration.csproj

# Performance tests only
cd tests/RemoteC.Tests.Performance
dotnet run --configuration Release -- --benchmarks
```

### Performance Test Options

```bash
# Run specific benchmarks
dotnet run -- --filter "*ApiPerformance*"

# Quick run (fewer iterations)
dotnet run -- --filter "*" --maxWarmupCount 1 --maxIterationCount 3

# Export results
dotnet run -- --filter "*" --exporters json --exporters html
```

## Test Results Analysis

### Generated Reports

After running `./run-all-tests.sh`, you'll find:

```
test-results/
└── YYYYMMDD_HHMM/
    ├── test_summary.md          # Overall test summary
    ├── remediation_plan.md      # Action items and fixes
    ├── RemoteC.Tests.Unit_results.txt
    ├── RemoteC.Api.Tests_results.txt
    ├── RemoteC.Tests.Integration_results.txt
    ├── performance_results.txt
    ├── coverage_summary.txt
    ├── code_analysis.txt
    └── dashboard.html           # Visual test dashboard
```

### Viewing the Dashboard

```bash
# Generate HTML dashboard
./generate-test-dashboard.sh

# Open in browser
xdg-open test-results/latest/dashboard.html  # Linux
open test-results/latest/dashboard.html      # macOS
start test-results/latest/dashboard.html     # Windows
```

## Performance Targets

### Phase 1 (Current)
| Metric | Target | Measurement Method |
|--------|--------|-------------------|
| Screen Capture Latency | <100ms | `BenchmarkScreenCapture` |
| Network Latency (LAN) | <100ms | `BenchmarkNetworkLatency` |
| API Response Time | <300ms | `BenchmarkDeviceListApi` |
| SignalR Connection | <500ms | `BenchmarkSignalRConnection` |

### Phase 2 (Rust Engine)
| Metric | Target | Notes |
|--------|--------|-------|
| Screen Capture Latency | <50ms | Hardware accelerated |
| Network Latency | <50ms | QUIC protocol |
| Frame Rate | 60 FPS | Sustained capture |
| Compression | H.264/H.265 | Hardware encoding |

## Remediation Process

### 1. Analyze Test Failures

```bash
# Find all test failures
grep -r "Failed:" test-results/latest/*_results.txt

# Extract error details
grep -A 10 -B 5 "Exception" test-results/latest/*_results.txt
```

### 2. Priority Classification

**Critical (Fix immediately)**
- Test failures in main branch
- Performance regressions >20%
- Security vulnerabilities
- Build breaks

**High (Fix in current sprint)**
- Test failures in feature branches
- Performance near threshold (90-100% of target)
- Code coverage <60% for critical components

**Medium (Fix in next sprint)**
- Code analysis warnings
- Code coverage 60-80%
- Non-critical test flakiness

**Low (Backlog)**
- Style violations
- Documentation gaps
- Nice-to-have optimizations

### 3. Common Fixes

#### Test Failures
```csharp
// Async test timeout
[Fact(Timeout = 30000)] // 30 seconds
public async Task LongRunningTest()
{
    // Test implementation
}

// Flaky test retry
[Retry(3)]
public async Task FlakyNetworkTest()
{
    // Test implementation
}
```

#### Performance Issues
```csharp
// Add caching
services.AddMemoryCache();
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});

// Optimize queries
_dbContext.Users
    .AsNoTracking() // Read-only queries
    .Where(u => u.IsActive)
    .Select(u => new UserDto { /* projection */ })
    .ToListAsync();
```

#### Coverage Improvements
```csharp
// Add edge case tests
[Theory]
[InlineData(null)]
[InlineData("")]
[InlineData(" ")]
public void ValidateInput_EdgeCases(string input)
{
    // Test null, empty, whitespace
}
```

## CI/CD Integration

### GitHub Actions Workflow

```yaml
name: Test Suite
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    - name: Run all tests
      run: ./run-all-tests.sh
    - name: Upload results
      uses: actions/upload-artifact@v3
      with:
        name: test-results
        path: test-results/
    - name: Comment PR
      if: github.event_name == 'pull_request'
      uses: actions/github-script@v6
      with:
        script: |
          const fs = require('fs');
          const summary = fs.readFileSync('test-results/latest/test_summary.md', 'utf8');
          github.rest.issues.createComment({
            issue_number: context.issue.number,
            owner: context.repo.owner,
            repo: context.repo.repo,
            body: summary
          });
```

### Quality Gates

Configure branch protection rules:
- All tests must pass
- Code coverage >80%
- No high-severity warnings
- Performance within targets

## Monitoring Test Health

### Test Metrics to Track

1. **Test Execution Time**
   - Track trends over time
   - Alert on significant increases
   - Optimize slow tests

2. **Test Flakiness**
   - Track intermittent failures
   - Identify environmental issues
   - Add retry logic where appropriate

3. **Coverage Trends**
   - Monitor coverage changes
   - Enforce minimum thresholds
   - Focus on critical paths

### Test Maintenance

**Weekly**
- Review flaky tests
- Update test data
- Remove obsolete tests

**Monthly**
- Performance baseline update
- Coverage analysis
- Test suite optimization

**Quarterly**
- Full test audit
- Update test strategies
- Performance target review

## Troubleshooting

### Common Issues

**TestContainers fails to start**
```bash
# Ensure Docker is running
docker ps

# Check available resources
docker system df

# Clean up
docker system prune -a
```

**Performance tests inconsistent**
```bash
# Close other applications
# Run in Release mode
# Use consistent hardware
# Disable CPU throttling
```

**Coverage not generating**
```bash
# Install coverage tools
dotnet tool install -g dotnet-coverage
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coveragereport
```

## Best Practices

### Writing Good Tests

1. **Arrange-Act-Assert Pattern**
```csharp
[Fact]
public async Task CreateSession_ValidInput_ReturnsSuccess()
{
    // Arrange
    var request = new CreateSessionRequest { /* ... */ };
    
    // Act
    var result = await _service.CreateSessionAsync(request);
    
    // Assert
    Assert.NotNull(result);
    Assert.True(result.Success);
}
```

2. **Test Data Builders**
```csharp
public class UserBuilder
{
    private User _user = new();
    
    public UserBuilder WithEmail(string email)
    {
        _user.Email = email;
        return this;
    }
    
    public User Build() => _user;
}
```

3. **Async Test Helpers**
```csharp
public static class AsyncTestHelpers
{
    public static async Task<T> WithTimeout<T>(
        Task<T> task, 
        int timeoutMs = 5000)
    {
        var timeoutTask = Task.Delay(timeoutMs);
        var completedTask = await Task.WhenAny(task, timeoutTask);
        
        if (completedTask == timeoutTask)
            throw new TimeoutException();
            
        return await task;
    }
}
```

## Appendix

### Useful Commands

```bash
# Find slowest tests
dotnet test --logger "console;verbosity=detailed" | grep "Passed.*ms"

# Run tests in parallel
dotnet test --parallel

# Generate test report
dotnet test --logger html

# Debug specific test
dotnet test --filter "FullyQualifiedName~CreateSession"

# Code coverage with filters
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:Exclude="[*.Tests]*"
```

### References

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [xUnit Documentation](https://xunit.net/)
- [TestContainers Documentation](https://dotnet.testcontainers.org/)
- [Code Coverage Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage)
#!/bin/bash
# Script to fix all Moq extension method issues in tests

echo "Fixing Moq extension method issues..."

# Find all test files that use IDistributedCache extension methods
echo "Finding files with IDistributedCache mocks..."

# Get list of test files using GetStringAsync, SetStringAsync, or RemoveAsync
files=$(grep -r "GetStringAsync\|SetStringAsync\|RemoveAsync" tests --include="*Tests.cs" -l | sort -u)

echo "Found ${#files[@]} files to check"

# For each file, check if it's using Moq and IDistributedCache
for file in $files; do
    if grep -q "Mock<IDistributedCache>" "$file"; then
        echo "  - $file needs fixing"
        
        # Create a marker to indicate this file needs manual review
        echo "$file" >> moq-fixes-needed.txt
    fi
done

echo ""
echo "Creating sample fix for IDistributedCache mocking..."

cat > fix-idistributedcache-sample.cs << 'EOF'
// Instead of mocking extension methods like:
_cacheMock.Setup(c => c.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync("value");

// Mock the underlying methods:
byte[] cacheValue = Encoding.UTF8.GetBytes("value");
_cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(cacheValue);

// For SetStringAsync:
_cacheMock.Setup(c => c.SetAsync(
    It.IsAny<string>(), 
    It.IsAny<byte[]>(), 
    It.IsAny<DistributedCacheEntryOptions>(), 
    It.IsAny<CancellationToken>()))
    .Returns(Task.CompletedTask);

// For RemoveAsync - this is not an extension method, so it works:
_cacheMock.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
    .Returns(Task.CompletedTask);
EOF

echo ""
echo "Files that need fixing have been listed in moq-fixes-needed.txt"
echo "Review fix-idistributedcache-sample.cs for the pattern to apply"
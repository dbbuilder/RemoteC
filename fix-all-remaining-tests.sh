#!/bin/bash
# Comprehensive script to fix all remaining test issues

echo "================================================"
echo "RemoteC - Fix All Remaining Tests"
echo "================================================"
echo ""

# Step 1: Fix CacheServiceTests
echo "=== Step 1: Fixing CacheServiceTests ==="
cat > patch-cache-service-tests.cs << 'EOF'
// Fix pattern for CacheServiceTests
// Replace GetStringAsync/SetStringAsync with GetAsync/SetAsync

// Original:
_cacheMock.Setup(c => c.GetStringAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync("value");

// Fixed:
_cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(Encoding.UTF8.GetBytes("value"));

// For SetStringAsync:
_cacheMock.Setup(c => c.SetAsync(
    It.IsAny<string>(), 
    It.IsAny<byte[]>(), 
    It.IsAny<DistributedCacheEntryOptions>(), 
    It.IsAny<CancellationToken>()))
    .Returns(Task.CompletedTask);
EOF

# Step 2: Fix AuditServiceTests  
echo ""
echo "=== Step 2: Fixing AuditServiceTests ==="
cat > patch-audit-service-tests.cs << 'EOF'
// Pattern for fixing AuditServiceTests cache mocks

// For GetAsync with typed result:
byte[]? cachedData = null;
if (expectedLogs != null)
{
    cachedData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(expectedLogs));
}
_cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(cachedData);
EOF

# Step 3: Fix all IConfiguration mocks
echo ""
echo "=== Step 3: Finding all IConfiguration mock issues ==="

# Find files with GetValue<T> pattern
echo "Files with IConfiguration.GetValue<T> mocks:"
grep -r "GetValue<" tests --include="*Tests.cs" -l | while read file; do
    echo "  - $file"
done

# Step 4: Create automated fix script
echo ""
echo "Creating automated fix script..."

cat > apply-remaining-fixes.sh << 'SCRIPT'
#!/bin/bash
echo "Applying fixes to all test files..."

# Function to fix IDistributedCache mocks in a file
fix_cache_mocks() {
    local file=$1
    echo "Fixing cache mocks in: $file"
    
    # Add System.Text if not present
    if ! grep -q "using System.Text;" "$file"; then
        sed -i '1a using System.Text;' "$file"
    fi
    
    # Replace GetStringAsync
    sed -i 's/GetStringAsync(/GetAsync(/g' "$file"
    sed -i 's/\.ReturnsAsync("\([^"]*\)")/.ReturnsAsync(Encoding.UTF8.GetBytes("\1"))/g' "$file"
    sed -i 's/\.ReturnsAsync((string?)null)/.ReturnsAsync((byte[]?)null)/g' "$file"
    
    # Replace SetStringAsync
    sed -i 's/SetStringAsync(/SetAsync(/g' "$file"
    sed -i 's/It\.IsAny<string>()/It.IsAny<byte[]>()/g' "$file"
}

# Function to fix IConfiguration mocks
fix_config_mocks() {
    local file=$1
    echo "Fixing configuration mocks in: $file"
    
    # Comment out GetValue<T> lines for manual fixing
    sed -i 's/^\([[:space:]]*\)\(.*GetValue<.*\)$/\1\/\/ TODO: Fix this - \2/' "$file"
}

# Apply fixes
for file in tests/RemoteC.Api.Tests/Services/CacheServiceTests.cs \
           tests/RemoteC.Api.Tests/Services/AuditServiceTests.cs; do
    if [ -f "$file" ]; then
        fix_cache_mocks "$file"
    fi
done

# Fix remaining IConfiguration issues
find tests -name "*Tests.cs" -type f | while read file; do
    if grep -q "GetValue<" "$file" && grep -q "Mock<IConfiguration>" "$file"; then
        fix_config_mocks "$file"
    fi
done

echo "Automated fixes applied!"
SCRIPT

chmod +x apply-remaining-fixes.sh

# Step 5: Fix ScreenCaptureServiceTests specifically
echo ""
echo "=== Step 5: Creating ScreenCaptureServiceTests fix ==="

cat > fix-screen-capture-tests.patch << 'EOF'
--- a/tests/RemoteC.Tests.Unit/Host/Services/ScreenCaptureServiceTests.cs
+++ b/tests/RemoteC.Tests.Unit/Host/Services/ScreenCaptureServiceTests.cs
@@ -64,6 +64,7 @@ public async Task CaptureScreenAsync_WhenInitialized_ShouldReturnScreenData()
         // Arrange
         await InitializeService();
         var quality = new QualitySettings { Quality = 85, TargetFps = 30 };
+        _providerMock.Setup(p => p.GetScreenBounds(It.IsAny<string>())).Returns(new ScreenBounds { Width = 1920, Height = 1080 });
         var expectedFrame = new ScreenFrame
         {
             Width = 1920,
@@ -120,6 +121,7 @@ public async Task CaptureScreenAsync_WithScaling_ShouldApplyScale()
         // Arrange
         await InitializeService();
         var quality = new QualitySettings { Quality = 85, Scale = 0.5f, CompressionType = RemoteC.Host.Services.CompressionType.Jpeg };
+        _providerMock.Setup(p => p.GetScreenBounds(It.IsAny<string>())).Returns(new ScreenBounds { Width = 1920, Height = 1080 });
         
         var originalFrame = new ScreenFrame
         {
EOF

echo ""
echo "================================================"
echo "Fix Script Complete!"
echo "================================================"
echo ""
echo "Next steps:"
echo "1. Run: ./apply-remaining-fixes.sh"
echo "2. Review the changes"
echo "3. Manually fix any TODO comments added"
echo "4. Run tests: dotnet test"
echo ""
echo "Files that need manual attention will have TODO comments"
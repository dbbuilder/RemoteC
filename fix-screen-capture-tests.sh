#!/bin/bash
# Script to fix ScreenCaptureService test failures

echo "Fixing ScreenCaptureService test failures..."

# Create a patch file for the fix
cat > screen-capture-test-fix.patch << 'EOF'
--- a/tests/RemoteC.Tests.Unit/Host/Services/ScreenCaptureServiceTests.cs
+++ b/tests/RemoteC.Tests.Unit/Host/Services/ScreenCaptureServiceTests.cs
@@ -73,8 +73,12 @@ public async Task CaptureScreenAsync_WhenInitialized_ShouldReturnScreenData()
         };
         var quality = new QualitySettings { Quality = 85, CompressionType = "JPEG" };
         
-        _providerMock.Setup(p => p.CaptureScreenAsync(It.IsAny<string>()))
-            .ReturnsAsync(expectedFrame);
+        // Fix: Ensure the mock returns a non-null ScreenFrame
+        _providerMock.Setup(p => p.CaptureScreenAsync(It.IsAny<string>()))
+            .ReturnsAsync(() => expectedFrame);
+        
+        _providerMock.Setup(p => p.GetScreenBounds(It.IsAny<string>()))
+            .Returns(new ScreenBounds { Width = 1920, Height = 1080 });
 
         // Act
         var result = await _service.CaptureScreenAsync(0, quality, CancellationToken.None);
@@ -141,8 +145,12 @@ public async Task CaptureScreenAsync_WithScaling_ShouldApplyScale()
             Timestamp = DateTime.UtcNow
         };
         
-        _providerMock.Setup(p => p.CaptureScreenAsync(It.IsAny<string>()))
-            .ReturnsAsync(originalFrame);
+        // Fix: Ensure the mock returns a non-null ScreenFrame
+        _providerMock.Setup(p => p.CaptureScreenAsync(It.IsAny<string>()))
+            .ReturnsAsync(() => originalFrame);
+            
+        _providerMock.Setup(p => p.GetScreenBounds(It.IsAny<string>()))
+            .Returns(new ScreenBounds { Width = 1920, Height = 1080 });
 
         // Act
         var result = await _service.CaptureScreenAsync(0, quality, CancellationToken.None);
EOF

# Check if patch can be applied
if patch --dry-run -p1 < screen-capture-test-fix.patch > /dev/null 2>&1; then
    echo "Applying patch..."
    patch -p1 < screen-capture-test-fix.patch
    echo "Patch applied successfully!"
else
    echo "Cannot apply patch automatically. Manual intervention required."
    echo "Please review the following changes:"
    cat screen-capture-test-fix.patch
fi

# Run the specific tests to verify the fix
echo ""
echo "Running ScreenCaptureService tests to verify fix..."
dotnet test tests/RemoteC.Tests.Unit/RemoteC.Tests.Unit.csproj \
    --filter "FullyQualifiedName~ScreenCaptureServiceTests" \
    --no-build \
    --verbosity normal

# Check test results
if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Tests are now passing!"
    echo ""
    echo "Next steps:"
    echo "1. Review the changes in ScreenCaptureServiceTests.cs"
    echo "2. Commit the fix with message: 'fix: Fix ScreenCaptureService test mock setup'"
    echo "3. Run the full test suite to ensure no regressions"
else
    echo ""
    echo "❌ Tests are still failing. Manual investigation required."
    echo ""
    echo "Debugging tips:"
    echo "1. Check if IRemoteControlProvider interface has changed"
    echo "2. Verify ScreenFrame constructor requirements"
    echo "3. Review the actual service implementation for expected behavior"
fi

# Clean up
rm -f screen-capture-test-fix.patch
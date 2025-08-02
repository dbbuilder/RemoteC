#!/bin/bash

echo "Building Rust library and searching for errors..."

# Build and capture output (both stdout and stderr)
cargo build --release --target x86_64-pc-windows-gnu --features "windows,openh264" > build_output_full.txt 2>&1

# Check build status
if [ $? -eq 0 ]; then
    echo "Build succeeded!"
    exit 0
else
    echo "Build failed. Searching for errors..."
fi

# Extract actual errors (not warnings)
echo "=== Compilation Errors ==="
grep -E "^error\[E[0-9]+\]:" build_output_full.txt

echo ""
echo "=== Error Summary ==="
grep "error: could not compile" build_output_full.txt

echo ""
echo "=== Searching for other error patterns ==="
grep -i "error:" build_output_full.txt | grep -v "warning:" | grep -v "note:" | head -20

echo ""
echo "=== Last 50 lines of output ==="
tail -50 build_output_full.txt | grep -v "warning:" | grep -v "note:"
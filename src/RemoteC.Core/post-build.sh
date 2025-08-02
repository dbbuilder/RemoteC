#!/bin/bash

# Post-build script to copy Rust DLL to .NET output directories

RUST_DLL="target/x86_64-pc-windows-gnu/release/remotec_core.dll"
API_BIN="../RemoteC.Api/bin"
HOST_BIN="../RemoteC.Host/bin"

if [ -f "$RUST_DLL" ]; then
    echo "Copying remotec_core.dll to .NET output directories..."
    
    # Copy to API Debug and Release directories
    for config in Debug Release; do
        for framework in net8.0; do
            TARGET_DIR="$API_BIN/$config/$framework"
            if [ -d "$TARGET_DIR" ]; then
                cp "$RUST_DLL" "$TARGET_DIR/"
                echo "  ✓ Copied to $TARGET_DIR"
            fi
        done
    done
    
    # Copy to Host Debug and Release directories
    for config in Debug Release; do
        for framework in net8.0-windows; do
            TARGET_DIR="$HOST_BIN/$config/$framework"
            if [ -d "$TARGET_DIR" ]; then
                cp "$RUST_DLL" "$TARGET_DIR/"
                echo "  ✓ Copied to $TARGET_DIR"
            fi
        done
    done
    
    echo "Post-build copy completed!"
else
    echo "Warning: $RUST_DLL not found. Run 'cargo build --release --target x86_64-pc-windows-gnu' first."
fi
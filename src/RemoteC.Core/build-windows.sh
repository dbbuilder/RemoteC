#!/bin/bash
# Build script for RemoteC Rust Core targeting Windows from WSL/Linux

set -e

CONFIGURATION=${1:-release}
TARGET="x86_64-pc-windows-gnu"

echo "Building RemoteC Rust Core for Windows..."
echo "Target: $TARGET"
echo "Configuration: $CONFIGURATION"

# Check if Rust is installed
if ! command -v cargo &> /dev/null; then
    echo "Error: Rust is not installed. Please install from https://rustup.rs/"
    exit 1
fi

# Add Windows target if not present
echo "Checking Rust target: $TARGET"
if ! rustup target list --installed | grep -q "$TARGET"; then
    echo "Installing target: $TARGET"
    rustup target add "$TARGET"
fi

# Check for MinGW
echo "Checking for MinGW cross-compiler..."
if ! command -v x86_64-w64-mingw32-gcc &> /dev/null; then
    echo "Error: MinGW not found. Please install:"
    echo "  sudo apt-get update && sudo apt-get install -y gcc-mingw-w64-x86-64 g++-mingw-w64-x86-64"
    exit 1
fi

# Set cross-compilation environment
export CC_x86_64_pc_windows_gnu=x86_64-w64-mingw32-gcc
export CXX_x86_64_pc_windows_gnu=x86_64-w64-mingw32-g++
export AR_x86_64_pc_windows_gnu=x86_64-w64-mingw32-ar
export CARGO_TARGET_X86_64_PC_WINDOWS_GNU_LINKER=x86_64-w64-mingw32-gcc

# Build based on configuration
if [ "$CONFIGURATION" = "release" ]; then
    echo "Building in Release mode..."
    cargo build --release --target "$TARGET" --features "windows,openh264"
    BUILD_PATH="target/$TARGET/release"
else
    echo "Building in Debug mode..."
    cargo build --target "$TARGET" --features "windows,openh264"
    BUILD_PATH="target/$TARGET/debug"
fi

OUTPUT_DLL="$BUILD_PATH/remotec_core.dll"

if [ -f "$OUTPUT_DLL" ]; then
    echo "Build successful! Output: $OUTPUT_DLL"
    
    # Run post-build script to copy DLL
    if [ -f "./post-build.sh" ]; then
        echo "Running post-build script..."
        ./post-build.sh
    fi
else
    echo "Error: Build output not found at: $OUTPUT_DLL"
    exit 1
fi

echo "Build completed successfully!"
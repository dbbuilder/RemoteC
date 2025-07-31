#!/bin/bash
# Build script for RemoteC Rust Core on Linux

set -e

CONFIGURATION=${1:-release}
TARGET=${2:-x86_64-unknown-linux-gnu}

echo "Building RemoteC Rust Core for Linux..."

# Check if Rust is installed
if ! command -v cargo &> /dev/null; then
    echo "Error: Rust is not installed. Please install from https://rustup.rs/"
    exit 1
fi

# Add Linux target if not present
echo "Checking Rust target: $TARGET"
if ! rustup target list --installed | grep -q "$TARGET"; then
    echo "Installing target: $TARGET"
    rustup target add "$TARGET"
fi

# Install Linux dependencies
echo "Checking Linux dependencies..."
if command -v apt-get &> /dev/null; then
    # Debian/Ubuntu
    sudo apt-get update
    sudo apt-get install -y libx11-dev libxcb1-dev libxcb-shm0-dev libxcb-xfixes0-dev
elif command -v yum &> /dev/null; then
    # RHEL/CentOS
    sudo yum install -y libX11-devel libxcb-devel
fi

# Set build flags
export RUSTFLAGS="-C target-cpu=native"

# Build based on configuration
if [ "$CONFIGURATION" = "release" ]; then
    echo "Building in Release mode..."
    cargo build --release --target "$TARGET" --features "linux,openh264"
    BUILD_PATH="target/$TARGET/release"
else
    echo "Building in Debug mode..."
    cargo build --target "$TARGET" --features "linux,openh264"
    BUILD_PATH="target/$TARGET/debug"
fi

OUTPUT_LIB="$BUILD_PATH/libremotec_core.so"

if [ -f "$OUTPUT_LIB" ]; then
    echo "Build successful! Output: $OUTPUT_LIB"
    
    # Copy to .NET interop project
    DEST_PATH="../../RemoteC.Core.Interop/runtimes/linux-x64/native"
    mkdir -p "$DEST_PATH"
    cp "$OUTPUT_LIB" "$DEST_PATH/"
    echo "Copied to: $DEST_PATH/libremotec_core.so"
    
    # Also copy to output directory for direct testing
    TEST_PATH="../../RemoteC.Api/bin/Debug/net8.0"
    if [ -d "$TEST_PATH" ]; then
        cp "$OUTPUT_LIB" "$TEST_PATH/"
        echo "Also copied to: $TEST_PATH/libremotec_core.so"
    fi
else
    echo "Error: Build output not found at: $OUTPUT_LIB"
    exit 1
fi

echo "Build completed successfully!"
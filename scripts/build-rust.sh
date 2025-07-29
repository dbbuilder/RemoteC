#!/bin/bash
# Build script for RemoteC Rust Core library

set -e

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_ROOT="$( cd "$SCRIPT_DIR/.." && pwd )"
RUST_PROJECT="$PROJECT_ROOT/src/RemoteC.Core"
OUTPUT_DIR="$PROJECT_ROOT/src/RemoteC.Core.Interop/runtimes"

echo "Building RemoteC Rust Core..."
echo "Project root: $PROJECT_ROOT"
echo "Rust project: $RUST_PROJECT"

# Check if Rust is installed
if ! command -v cargo &> /dev/null; then
    echo "Error: Rust is not installed. Please install from https://rustup.rs/"
    exit 1
fi

# Create output directories
mkdir -p "$OUTPUT_DIR/win-x64/native"
mkdir -p "$OUTPUT_DIR/linux-x64/native"
mkdir -p "$OUTPUT_DIR/osx-x64/native"
mkdir -p "$OUTPUT_DIR/osx-arm64/native"

# Build for current platform
cd "$RUST_PROJECT"

echo "Building for current platform..."
cargo build --release

# Detect current platform and copy library
if [[ "$OSTYPE" == "linux-gnu"* ]]; then
    echo "Copying Linux library..."
    cp target/release/libremotec_core.so "$OUTPUT_DIR/linux-x64/native/"
elif [[ "$OSTYPE" == "darwin"* ]]; then
    echo "Copying macOS library..."
    if [[ $(uname -m) == "arm64" ]]; then
        cp target/release/libremotec_core.dylib "$OUTPUT_DIR/osx-arm64/native/"
    else
        cp target/release/libremotec_core.dylib "$OUTPUT_DIR/osx-x64/native/"
    fi
elif [[ "$OSTYPE" == "msys" ]] || [[ "$OSTYPE" == "cygwin" ]] || [[ "$OSTYPE" == "win32" ]]; then
    echo "Copying Windows library..."
    cp target/release/remotec_core.dll "$OUTPUT_DIR/win-x64/native/"
fi

# Cross-compile for other platforms if cross is installed
if command -v cross &> /dev/null; then
    echo "Cross compilation available. Building for other platforms..."
    
    # Build for Windows if not on Windows
    if [[ "$OSTYPE" != "msys" ]] && [[ "$OSTYPE" != "cygwin" ]] && [[ "$OSTYPE" != "win32" ]]; then
        echo "Cross-compiling for Windows x64..."
        cross build --release --target x86_64-pc-windows-gnu
        cp target/x86_64-pc-windows-gnu/release/remotec_core.dll "$OUTPUT_DIR/win-x64/native/" || true
    fi
    
    # Build for Linux if not on Linux
    if [[ "$OSTYPE" != "linux-gnu"* ]]; then
        echo "Cross-compiling for Linux x64..."
        cross build --release --target x86_64-unknown-linux-gnu
        cp target/x86_64-unknown-linux-gnu/release/libremotec_core.so "$OUTPUT_DIR/linux-x64/native/" || true
    fi
else
    echo "Note: Install 'cross' for cross-platform compilation:"
    echo "  cargo install cross"
fi

echo "Build complete. Libraries are in: $OUTPUT_DIR"

# Generate C# bindings documentation
echo "Generating FFI documentation..."
cd "$RUST_PROJECT"
cargo doc --no-deps

echo "Done!"
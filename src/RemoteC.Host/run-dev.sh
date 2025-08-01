#!/bin/bash

# Set development environment
export ASPNETCORE_ENVIRONMENT=Development
export DOTNET_ENVIRONMENT=Development

# Copy Rust library
RUST_LIB="/mnt/d/dev2/remotec/src/RemoteC.Core/target/release/libremotec_core.so"
if [ -f "$RUST_LIB" ]; then
    cp "$RUST_LIB" bin/Debug/net8.0/ 2>/dev/null || true
fi

# Set library path for Linux
export LD_LIBRARY_PATH=$LD_LIBRARY_PATH:$(pwd)/bin/Debug/net8.0

# Run the Host
echo "Starting RemoteC Host in Development mode..."
echo "Environment: $ASPNETCORE_ENVIRONMENT"
echo "Library path: $LD_LIBRARY_PATH"
echo ""

dotnet run --environment Development
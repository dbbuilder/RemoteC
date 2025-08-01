#!/bin/bash

# Simple host test runner for RemoteC

set -e

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Configuration
API_URL="http://localhost:17001"
HOST_ID="dev-host-001"
HOST_SECRET="dev-secret-001"

echo -e "${YELLOW}RemoteC Host Test Runner${NC}"
echo "========================"

# 1. Create minimal host config
echo -e "\n${YELLOW}1. Creating host configuration...${NC}"
cat > src/RemoteC.Host/appsettings.Development.json << EOF
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "RemoteC": "Debug"
    }
  },
  "Host": {
    "Id": "$HOST_ID",
    "Secret": "$HOST_SECRET"
  },
  "Api": {
    "BaseUrl": "$API_URL",
    "TokenEndpoint": "/api/auth/host/token"
  },
  "HostConfiguration": {
    "ServerUrl": "$API_URL"
  },
  "RemoteControlProvider": {
    "Type": "Rust"
  }
}
EOF

# 2. Copy Rust library to Host output
echo -e "\n${YELLOW}2. Copying Rust library to Host output...${NC}"
RUST_LIB="/mnt/d/dev2/remotec/src/RemoteC.Core/target/release/libremotec_core.so"
if [ -f "$RUST_LIB" ]; then
    mkdir -p src/RemoteC.Host/bin/Debug/net8.0
    cp "$RUST_LIB" src/RemoteC.Host/bin/Debug/net8.0/
    echo "Rust library copied"
else
    echo -e "${RED}Warning: Rust library not found at $RUST_LIB${NC}"
fi

# 3. Build the Host
echo -e "\n${YELLOW}3. Building RemoteC Host...${NC}"
cd src/RemoteC.Host
dotnet build
cd ../..

# 4. Run the Host
echo -e "\n${YELLOW}4. Starting RemoteC Host...${NC}"
echo "Host will connect to API at: $API_URL"
echo "Using host credentials: $HOST_ID / $HOST_SECRET"
echo ""
echo "Press Ctrl+C to stop"
echo ""

cd src/RemoteC.Host
dotnet run
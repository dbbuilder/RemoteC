#!/bin/bash

# End-to-end test setup for RemoteC remote control
# This script sets up and runs a full test from host to server to client

set -e

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
API_URL="http://localhost:17001"
HOST_ID="test-host-001"
HOST_SECRET="test-secret-001"
DEVICE_ID="aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
DEVICE_NAME="Test Host Machine"

echo -e "${BLUE}RemoteC End-to-End Remote Control Test Setup${NC}"
echo "=============================================="

# Function to check if a process is running
check_process() {
    if pgrep -f "$1" > /dev/null; then
        return 0
    else
        return 1
    fi
}

# Function to wait for service to be ready
wait_for_service() {
    local url=$1
    local name=$2
    local max_attempts=30
    local attempt=0
    
    echo -e "${YELLOW}Waiting for $name to be ready...${NC}"
    while [ $attempt -lt $max_attempts ]; do
        if curl -s "$url" > /dev/null 2>&1; then
            echo -e "${GREEN}$name is ready!${NC}"
            return 0
        fi
        attempt=$((attempt + 1))
        sleep 1
    done
    
    echo -e "${RED}$name failed to start${NC}"
    return 1
}

# 1. Build all components
echo -e "\n${YELLOW}1. Building components...${NC}"

# Build Rust core if needed
if [ ! -f "src/RemoteC.Core/target/release/libremotec_core.so" ]; then
    echo "Building Rust core library..."
    cd src/RemoteC.Core
    cargo build --release --features linux
    cd ../..
fi

# Build API
echo "Building RemoteC API..."
cd src/RemoteC.Api
dotnet build
cd ../..

# Build Host
echo "Building RemoteC Host..."
cd src/RemoteC.Host
dotnet build
cd ../..

# Build Client
echo "Building RemoteC Client..."
cd src/RemoteC.Client
dotnet build
cd ../..

# 2. Start the API server
echo -e "\n${YELLOW}2. Starting API server...${NC}"
if check_process "RemoteC.Api"; then
    echo "API server is already running"
else
    cd src/RemoteC.Api
    nohup dotnet run > ../../api-e2e.log 2>&1 &
    API_PID=$!
    cd ../..
    
    # Wait for API to be ready
    wait_for_service "$API_URL/health" "API server"
fi

# 3. Create test configuration for Host
echo -e "\n${YELLOW}3. Creating Host configuration...${NC}"
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
    "Secret": "$HOST_SECRET",
    "DeviceId": "$DEVICE_ID",
    "DeviceName": "$DEVICE_NAME"
  },
  "Api": {
    "BaseUrl": "$API_URL",
    "TokenEndpoint": "/api/auth/host/token"
  },
  "HostConfiguration": {
    "ServerUrl": "$API_URL",
    "AutoStart": true,
    "EnableScreenCapture": true,
    "EnableInputControl": true,
    "EnableFileTransfer": true,
    "EnableClipboardSync": true,
    "MaxConcurrentSessions": 5,
    "SessionTimeout": 1800
  },
  "RemoteControlProvider": {
    "Type": "Rust"
  }
}
EOF

# 4. Register the host device
echo -e "\n${YELLOW}4. Registering host device...${NC}"
# First, get a host token
TOKEN_RESPONSE=$(curl -s -X POST "$API_URL/api/auth/host/token" \
  -H "Content-Type: application/json" \
  -d "{
    \"hostId\": \"$HOST_ID\",
    \"secret\": \"$HOST_SECRET\"
  }")

TOKEN=$(echo "$TOKEN_RESPONSE" | jq -r '.token')
if [ -z "$TOKEN" ] || [ "$TOKEN" = "null" ]; then
    echo -e "${RED}Failed to get authentication token${NC}"
    exit 1
fi

echo "Got authentication token"

# Register device if needed
DEVICE_CHECK=$(curl -s -X GET "$API_URL/api/devices/$DEVICE_ID" \
  -H "Authorization: Bearer $TOKEN")

if echo "$DEVICE_CHECK" | grep -q "not found"; then
    echo "Registering new device..."
    curl -s -X POST "$API_URL/api/devices" \
      -H "Authorization: Bearer $TOKEN" \
      -H "Content-Type: application/json" \
      -d "{
        \"id\": \"$DEVICE_ID\",
        \"name\": \"$DEVICE_NAME\",
        \"hostName\": \"$(hostname)\",
        \"operatingSystem\": \"$(uname -s) $(uname -r)\",
        \"ipAddress\": \"$(hostname -I | awk '{print $1}')\",
        \"macAddress\": \"00:00:00:00:00:00\"
      }" | jq '.'
else
    echo "Device already registered"
fi

# 5. Start the Host service
echo -e "\n${YELLOW}5. Starting Host service...${NC}"
cd src/RemoteC.Host
nohup dotnet run > ../../host-e2e.log 2>&1 &
HOST_PID=$!
cd ../..

echo "Waiting for Host to connect to API..."
sleep 5

# 6. Create a test session
echo -e "\n${YELLOW}6. Creating test remote control session...${NC}"
SESSION_RESPONSE=$(curl -s -X POST "$API_URL/api/sessions" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"name\": \"E2E Test Session\",
    \"deviceId\": \"$DEVICE_ID\"
  }")

SESSION_ID=$(echo "$SESSION_RESPONSE" | jq -r '.id')
echo "Created session: $SESSION_ID"

# 7. Start the session
echo -e "\n${YELLOW}7. Starting remote control session...${NC}"
START_RESPONSE=$(curl -s -X POST "$API_URL/api/sessions/$SESSION_ID/start" \
  -H "Authorization: Bearer $TOKEN")

PIN=$(echo "$START_RESPONSE" | jq -r '.pin')
echo -e "${GREEN}Session started with PIN: $PIN${NC}"

# 8. Create client test page
echo -e "\n${YELLOW}8. Creating client test interface...${NC}"
cat > test-client.html << 'EOF'
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>RemoteC Client Test</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            margin: 0;
            padding: 20px;
            background: #f0f0f0;
        }
        .container {
            max-width: 1200px;
            margin: 0 auto;
            background: white;
            padding: 20px;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }
        .screen-container {
            width: 100%;
            height: 600px;
            background: #000;
            border: 2px solid #333;
            border-radius: 4px;
            margin: 20px 0;
            position: relative;
            overflow: hidden;
        }
        #remoteScreen {
            width: 100%;
            height: 100%;
            object-fit: contain;
        }
        .controls {
            display: flex;
            gap: 10px;
            margin: 20px 0;
            flex-wrap: wrap;
        }
        button {
            padding: 10px 20px;
            font-size: 16px;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            background: #007bff;
            color: white;
        }
        button:hover {
            background: #0056b3;
        }
        button:disabled {
            background: #ccc;
            cursor: not-allowed;
        }
        .info {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 10px;
            margin: 20px 0;
        }
        .info-item {
            padding: 10px;
            background: #f8f9fa;
            border-radius: 4px;
        }
        .info-label {
            font-weight: bold;
            color: #666;
            font-size: 12px;
        }
        .info-value {
            font-size: 18px;
            color: #333;
        }
        .status {
            padding: 5px 10px;
            border-radius: 4px;
            display: inline-block;
        }
        .status.connected {
            background: #28a745;
            color: white;
        }
        .status.disconnected {
            background: #dc3545;
            color: white;
        }
        .log {
            height: 200px;
            overflow-y: auto;
            background: #f8f9fa;
            border: 1px solid #dee2e6;
            border-radius: 4px;
            padding: 10px;
            font-family: 'Courier New', monospace;
            font-size: 12px;
        }
    </style>
</head>
<body>
    <div class="container">
        <h1>RemoteC Client Test Interface</h1>
        
        <div class="info">
            <div class="info-item">
                <div class="info-label">Session ID</div>
                <div class="info-value" id="sessionId">Not connected</div>
            </div>
            <div class="info-item">
                <div class="info-label">Connection Status</div>
                <div class="info-value">
                    <span id="connectionStatus" class="status disconnected">Disconnected</span>
                </div>
            </div>
            <div class="info-item">
                <div class="info-label">FPS</div>
                <div class="info-value" id="fps">0</div>
            </div>
            <div class="info-item">
                <div class="info-label">Latency</div>
                <div class="info-value" id="latency">0ms</div>
            </div>
        </div>

        <div class="controls">
            <input type="text" id="pinInput" placeholder="Enter PIN" style="padding: 10px;">
            <button onclick="connect()">Connect</button>
            <button onclick="disconnect()" disabled id="disconnectBtn">Disconnect</button>
            <button onclick="toggleFullscreen()">Fullscreen</button>
            <button onclick="testMouseMove()">Test Mouse Move</button>
            <button onclick="testKeyPress()">Test Key Press</button>
        </div>

        <div class="screen-container" id="screenContainer">
            <canvas id="remoteScreen"></canvas>
        </div>

        <h3>Event Log</h3>
        <div class="log" id="eventLog"></div>
    </div>

    <script src="https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/7.0.5/signalr.min.js"></script>
    <script>
        // Configuration
        const API_URL = 'http://localhost:17001';
        let connection = null;
        let sessionId = null;
        let canvas = null;
        let ctx = null;
        let frameCount = 0;
        let lastFrameTime = Date.now();
        
        function log(message, type = 'info') {
            const logDiv = document.getElementById('eventLog');
            const timestamp = new Date().toLocaleTimeString();
            const entry = document.createElement('div');
            entry.style.color = type === 'error' ? 'red' : type === 'success' ? 'green' : 'black';
            entry.textContent = `[${timestamp}] ${message}`;
            logDiv.appendChild(entry);
            logDiv.scrollTop = logDiv.scrollHeight;
        }

        async function connect() {
            try {
                const pin = document.getElementById('pinInput').value;
                if (!pin) {
                    log('Please enter a PIN', 'error');
                    return;
                }

                log('Connecting to session with PIN: ' + pin);
                
                // TODO: Implement PIN-based connection
                // For now, using the session ID from the page
                sessionId = '${SESSION_ID}';
                document.getElementById('sessionId').textContent = sessionId;

                // Initialize SignalR connection
                connection = new signalR.HubConnectionBuilder()
                    .withUrl(`${API_URL}/sessionHub`, {
                        accessTokenFactory: () => '${TOKEN}'
                    })
                    .withAutomaticReconnect()
                    .configureLogging(signalR.LogLevel.Information)
                    .build();

                // Set up event handlers
                connection.on("ReceiveScreenUpdate", (screenData) => {
                    handleScreenUpdate(screenData);
                });

                connection.on("SessionStatusChanged", (status) => {
                    log(`Session status: ${status}`, 'info');
                });

                await connection.start();
                log('Connected to SignalR hub', 'success');
                
                // Join session
                await connection.invoke("JoinSession", sessionId);
                log('Joined session', 'success');
                
                updateConnectionStatus(true);
                
                // Initialize canvas
                canvas = document.getElementById('remoteScreen');
                ctx = canvas.getContext('2d');
                
                // Set up mouse and keyboard handlers
                setupInputHandlers();
                
            } catch (err) {
                log(`Connection error: ${err}`, 'error');
            }
        }

        async function disconnect() {
            if (connection) {
                try {
                    await connection.invoke("LeaveSession", sessionId);
                    await connection.stop();
                    log('Disconnected', 'info');
                    updateConnectionStatus(false);
                } catch (err) {
                    log(`Disconnect error: ${err}`, 'error');
                }
            }
        }

        function updateConnectionStatus(connected) {
            const status = document.getElementById('connectionStatus');
            const disconnectBtn = document.getElementById('disconnectBtn');
            
            if (connected) {
                status.textContent = 'Connected';
                status.className = 'status connected';
                disconnectBtn.disabled = false;
            } else {
                status.textContent = 'Disconnected';
                status.className = 'status disconnected';
                disconnectBtn.disabled = true;
            }
        }

        function handleScreenUpdate(screenData) {
            frameCount++;
            
            // Update FPS
            const now = Date.now();
            if (now - lastFrameTime > 1000) {
                document.getElementById('fps').textContent = frameCount;
                frameCount = 0;
                lastFrameTime = now;
            }
            
            // Update screen
            if (screenData.data) {
                // Decode and display frame
                // TODO: Implement actual frame decoding
                log('Received screen update', 'info');
            }
        }

        function setupInputHandlers() {
            const container = document.getElementById('screenContainer');
            
            // Mouse move
            container.addEventListener('mousemove', async (e) => {
                if (!connection) return;
                
                const rect = container.getBoundingClientRect();
                const x = (e.clientX - rect.left) / rect.width;
                const y = (e.clientY - rect.top) / rect.height;
                
                try {
                    await connection.invoke("SendMouseInput", sessionId, {
                        x: Math.floor(x * 1920), // Assuming 1920x1080
                        y: Math.floor(y * 1080),
                        action: 0 // Move
                    });
                } catch (err) {
                    console.error('Mouse move error:', err);
                }
            });
            
            // Mouse click
            container.addEventListener('click', async (e) => {
                if (!connection) return;
                
                const rect = container.getBoundingClientRect();
                const x = (e.clientX - rect.left) / rect.width;
                const y = (e.clientY - rect.top) / rect.height;
                
                try {
                    await connection.invoke("SendMouseInput", sessionId, {
                        x: Math.floor(x * 1920),
                        y: Math.floor(y * 1080),
                        action: 1, // Click
                        button: 0 // Left
                    });
                    log('Mouse click sent', 'info');
                } catch (err) {
                    console.error('Mouse click error:', err);
                }
            });
            
            // Keyboard
            document.addEventListener('keydown', async (e) => {
                if (!connection) return;
                
                try {
                    await connection.invoke("SendKeyboardInput", sessionId, {
                        keyCode: e.keyCode,
                        action: 0, // Down
                        modifiers: 0
                    });
                } catch (err) {
                    console.error('Key press error:', err);
                }
            });
        }

        function toggleFullscreen() {
            const container = document.getElementById('screenContainer');
            if (!document.fullscreenElement) {
                container.requestFullscreen();
            } else {
                document.exitFullscreen();
            }
        }

        async function testMouseMove() {
            if (!connection) {
                log('Not connected', 'error');
                return;
            }
            
            try {
                await connection.invoke("SendMouseInput", sessionId, {
                    x: 960,
                    y: 540,
                    action: 0 // Move
                });
                log('Test mouse move sent to center of screen', 'success');
            } catch (err) {
                log(`Test mouse move error: ${err}`, 'error');
            }
        }

        async function testKeyPress() {
            if (!connection) {
                log('Not connected', 'error');
                return;
            }
            
            try {
                await connection.invoke("SendKeyboardInput", sessionId, {
                    keyCode: 65, // 'A'
                    action: 0, // Press
                    modifiers: 0
                });
                log('Test key press sent (A)', 'success');
            } catch (err) {
                log(`Test key press error: ${err}`, 'error');
            }
        }

        // Update latency periodically
        setInterval(async () => {
            if (connection && connection.state === signalR.HubConnectionState.Connected) {
                const start = Date.now();
                try {
                    // Ping the server
                    const latency = Date.now() - start;
                    document.getElementById('latency').textContent = `${latency}ms`;
                } catch (err) {
                    console.error('Latency check error:', err);
                }
            }
        }, 1000);
    </script>
</body>
</html>
EOF

echo -e "${GREEN}Client test interface created: test-client.html${NC}"

# 9. Show status
echo -e "\n${YELLOW}9. Current status:${NC}"
echo "API Server: Running on $API_URL"
echo "Host Service: Running (check host-e2e.log for details)"
echo "Session ID: $SESSION_ID"
echo "PIN: $PIN"
echo ""
echo -e "${GREEN}Setup complete!${NC}"
echo ""
echo "To test the remote control:"
echo "1. Open test-client.html in a web browser"
echo "2. Enter PIN: $PIN"
echo "3. Click Connect"
echo ""
echo "Monitor logs:"
echo "- API: tail -f api-e2e.log"
echo "- Host: tail -f host-e2e.log"
echo ""
echo "To stop all services:"
echo "pkill -f RemoteC.Api"
echo "pkill -f RemoteC.Host"
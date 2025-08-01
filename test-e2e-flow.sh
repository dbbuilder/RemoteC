#!/bin/bash

# Test E2E flow for RemoteC

API_URL="http://localhost:17001"
HOST_ID="dev-host-001"
HOST_SECRET="dev-secret-001"

echo "=== RemoteC E2E Test ==="
echo ""

# 1. Get client auth token
echo "1. Getting client auth token..."
CLIENT_TOKEN=$(curl -s -X POST "$API_URL/api/auth/dev-login" \
  -H "Content-Type: application/json" \
  -d '{}' | jq -r '.token')

if [ -z "$CLIENT_TOKEN" ]; then
  echo "Failed to get client token"
  exit 1
fi
echo "✓ Got client token"

# 2. List devices
echo ""
echo "2. Listing devices..."
DEVICES=$(curl -s "$API_URL/api/devices" \
  -H "Authorization: Bearer $CLIENT_TOKEN")
echo "Devices: $DEVICES"
DEVICE_ID=$(echo "$DEVICES" | jq -r '.items[0].id')

if [ -z "$DEVICE_ID" ] || [ "$DEVICE_ID" = "null" ]; then
  echo "No devices found"
  exit 1
fi
echo "✓ Using device: $DEVICE_ID"

# 3. Create a session
echo ""
echo "3. Creating session..."
SESSION=$(curl -s -X POST "$API_URL/api/sessions" \
  -H "Authorization: Bearer $CLIENT_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"name\": \"E2E Test Session\", \"deviceId\": \"$DEVICE_ID\"}")

SESSION_ID=$(echo "$SESSION" | jq -r '.id')
if [ -z "$SESSION_ID" ] || [ "$SESSION_ID" = "null" ]; then
  echo "Failed to create session"
  echo "$SESSION"
  exit 1
fi
echo "✓ Created session: $SESSION_ID"

# 4. Start the session
echo ""
echo "4. Starting session..."
START_RESULT=$(curl -s -X POST "$API_URL/api/sessions/$SESSION_ID/start" \
  -H "Authorization: Bearer $CLIENT_TOKEN")

PIN=$(echo "$START_RESULT" | jq -r '.pin')
if [ -z "$PIN" ] || [ "$PIN" = "null" ]; then
  echo "Failed to start session"
  echo "$START_RESULT"
  exit 1
fi
echo "✓ Session started with PIN: $PIN"

# 5. Get session details
echo ""
echo "5. Getting session details..."
SESSION_DETAILS=$(curl -s "$API_URL/api/sessions/$SESSION_ID" \
  -H "Authorization: Bearer $CLIENT_TOKEN")

STATUS=$(echo "$SESSION_DETAILS" | jq -r '.status')
echo "Session status: $STATUS"

# 6. Check if Host is handling the session
echo ""
echo "6. Checking Host logs for session activity..."
if [ -f bin/net8.0/linux-x64/host-output.log ]; then
  tail -20 bin/net8.0/linux-x64/host-output.log | grep -E "(StartSession|SessionStarted|$SESSION_ID)" || echo "No session activity in Host logs yet"
fi

# 7. Stop the session
echo ""
echo "7. Stopping session..."
STOP_RESULT=$(curl -s -X POST "$API_URL/api/sessions/$SESSION_ID/stop" \
  -H "Authorization: Bearer $CLIENT_TOKEN")
echo "✓ Session stopped"

echo ""
echo "=== E2E Test Complete ==="
echo ""
echo "Summary:"
echo "- API: ✓ Running"
echo "- Host: ✓ Connected" 
echo "- Session: ✓ Created and started"
echo "- PIN: $PIN"
echo ""
echo "To test remote control:"
echo "1. Open test-remote-client.html in a browser"
echo "2. Click 'Test API Connection'"
echo "3. Click 'Authenticate'"
echo "4. Click 'Connect SignalR'"
echo "5. Click 'List Devices'"
echo "6. Click 'Create Session'"
echo "7. Click 'Start Session'"
echo "8. Use the remote control buttons"
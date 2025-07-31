#!/bin/bash

# Test script for verifying screen sharing and input control functionality

API_URL="http://localhost:17001/api"
HUB_URL="http://localhost:17001/sessionHub"

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "Testing RemoteC Screen Sharing and Input Control..."
echo "==================================================="

# 1. Check API health
echo -e "\n${YELLOW}1. Checking API health...${NC}"
HEALTH_RESPONSE=$(curl -s -X GET "$API_URL/health")
echo "$HEALTH_RESPONSE" | jq '.'

# 2. Get host token
echo -e "\n${YELLOW}2. Getting host authentication token...${NC}"
TOKEN_RESPONSE=$(curl -s -X POST "$API_URL/auth/host/token" \
  -H "Content-Type: application/json" \
  -d '{
    "hostId": "dev-host-001",
    "secret": "dev-secret-001"
  }')

TOKEN=$(echo "$TOKEN_RESPONSE" | jq -r '.token')
echo "Token obtained: ${TOKEN:0:50}..."

# 3. Create a remote session
echo -e "\n${YELLOW}3. Creating remote control session...${NC}"
SESSION_RESPONSE=$(curl -s -X POST "$API_URL/sessions" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Screen Control Test",
    "deviceId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
  }')

SESSION_ID=$(echo "$SESSION_RESPONSE" | jq -r '.id')
echo "Session created with ID: $SESSION_ID"
echo "$SESSION_RESPONSE" | jq '.'

# 4. Start the session
echo -e "\n${YELLOW}4. Starting remote control session...${NC}"
START_RESPONSE=$(curl -s -X POST "$API_URL/sessions/$SESSION_ID/start" \
  -H "Authorization: Bearer $TOKEN")

echo "$START_RESPONSE" | jq '.'

# 5. Test SignalR connection
echo -e "\n${YELLOW}5. Testing SignalR connection...${NC}"
# Note: This is a simplified test. Real SignalR testing requires a WebSocket client
echo "SignalR endpoint available at: $HUB_URL"
echo "To fully test SignalR, use a WebSocket client or the web UI"

# 6. Capture a screen frame
echo -e "\n${YELLOW}6. Testing screen capture...${NC}"
# This would need a specific endpoint for screen capture testing
echo "Screen capture is handled through the Rust provider"
echo "Check API logs for capture initialization"

# 7. Send test input events
echo -e "\n${YELLOW}7. Testing input simulation...${NC}"
# This would need specific endpoints for input testing
echo "Input simulation is handled through the Rust provider"
echo "Check API logs for input handling"

# 8. Get session statistics
echo -e "\n${YELLOW}8. Getting session statistics...${NC}"
# This would need a statistics endpoint
echo "Session statistics available through the provider"

# 9. Stop the session
echo -e "\n${YELLOW}9. Stopping the session...${NC}"
STOP_RESPONSE=$(curl -s -X POST "$API_URL/sessions/$SESSION_ID/stop" \
  -H "Authorization: Bearer $TOKEN")

echo "Session stopped"

# 10. Check provider configuration
echo -e "\n${YELLOW}10. Checking provider configuration...${NC}"
SETTINGS_RESPONSE=$(curl -s -X GET "$API_URL/settings/provider" \
  -H "Authorization: Bearer $TOKEN")

echo "$SETTINGS_RESPONSE" | jq '.'

echo -e "\n${GREEN}Test completed!${NC}"
echo "Check the API logs for detailed information about:"
echo "- Rust provider initialization"
echo "- Screen capture operations"
echo "- Input simulation events"
echo "- SignalR real-time communication"
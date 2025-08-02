#!/bin/bash

# Test script for provider switching functionality

API_URL="http://localhost:17001/api"

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "Testing RemoteC Provider Switching..."
echo "===================================="

# 1. Get host token
echo -e "\n${YELLOW}1. Getting host authentication token...${NC}"
TOKEN_RESPONSE=$(curl -s -X POST "$API_URL/auth/host/token" \
  -H "Content-Type: application/json" \
  -d '{
    "hostId": "dev-host-001",
    "secret": "dev-secret-001"
  }')

TOKEN=$(echo "$TOKEN_RESPONSE" | jq -r '.token')
echo "Token obtained: ${TOKEN:0:50}..."

# 2. Get current settings
echo -e "\n${YELLOW}2. Getting current settings...${NC}"
SETTINGS_RESPONSE=$(curl -s -X GET "$API_URL/settings" \
  -H "Authorization: Bearer $TOKEN")

echo "$SETTINGS_RESPONSE" | jq '.remoteControl'

# 3. Get provider stats
echo -e "\n${YELLOW}3. Getting provider statistics...${NC}"
STATS_RESPONSE=$(curl -s -X GET "$API_URL/settings/provider/stats" \
  -H "Authorization: Bearer $TOKEN")

echo "$STATS_RESPONSE" | jq '.'

# 4. Create a session with current provider
echo -e "\n${YELLOW}4. Creating session with current provider...${NC}"
SESSION_RESPONSE=$(curl -s -X POST "$API_URL/sessions" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Provider Test Session",
    "deviceId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
  }')

SESSION_ID=$(echo "$SESSION_RESPONSE" | jq -r '.id')
echo "Session created with ID: $SESSION_ID"

# 5. Start the session
echo -e "\n${YELLOW}5. Starting session...${NC}"
START_RESPONSE=$(curl -s -X POST "$API_URL/sessions/$SESSION_ID/start" \
  -H "Authorization: Bearer $TOKEN")

echo "$START_RESPONSE" | jq '.'

# 6. Stop the session
echo -e "\n${YELLOW}6. Stopping session...${NC}"
curl -s -X POST "$API_URL/sessions/$SESSION_ID/stop" \
  -H "Authorization: Bearer $TOKEN"

echo "Session stopped"

# 7. Test provider switching (will need admin role)
echo -e "\n${YELLOW}7. Testing provider switching (requires admin role)...${NC}"
echo "Note: This will likely fail in development mode unless you have admin role configured"

# Try to switch to ControlR
echo -e "\n${YELLOW}Attempting to switch to ControlR provider...${NC}"
SWITCH_RESPONSE=$(curl -s -X POST "$API_URL/settings/provider" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "provider": "ControlR"
  }')

echo "$SWITCH_RESPONSE" | jq '.'

# Try to switch back to Rust
echo -e "\n${YELLOW}Attempting to switch to Rust provider...${NC}"
SWITCH_RESPONSE=$(curl -s -X POST "$API_URL/settings/provider" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "provider": "Rust"
  }')

echo "$SWITCH_RESPONSE" | jq '.'

echo -e "\n${GREEN}Provider switching test completed!${NC}"
echo "Note: Provider switching requires admin role and application restart"
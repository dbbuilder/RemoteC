#!/bin/bash

# Test script for RemoteC remote control functionality

API_URL="https://localhost:17002"
HTTP_URL="http://127.0.0.1:17001"

echo "Testing RemoteC Remote Control API..."
echo "===================================="

# First test: Health check
echo -e "\n1. Testing API health..."
curl -k "${API_URL}/health" 2>/dev/null || curl "${HTTP_URL}/health" 2>/dev/null
echo ""

# In development mode, we can use any Bearer token or the host token endpoint
echo -e "\n2. Getting development auth token..."

# Use the host token endpoint for authentication
TOKEN_RESPONSE=$(curl -s -k -X POST "${API_URL}/api/auth/host/token" \
  -H "Content-Type: application/json" \
  -d '{"hostId":"dev-host-001","secret":"dev-secret-001"}' 2>/dev/null || \
  curl -s -X POST "${HTTP_URL}/api/auth/host/token" \
  -H "Content-Type: application/json" \
  -d '{"hostId":"dev-host-001","secret":"dev-secret-001"}' 2>/dev/null)

echo "Token response: $TOKEN_RESPONSE"

# Extract token from response
TOKEN=$(echo $TOKEN_RESPONSE | grep -o '"token":"[^"]*' | sed 's/"token":"//')
echo "Token: $TOKEN"

if [ -z "$TOKEN" ]; then
    echo "Failed to get auth token. Exiting."
    exit 1
fi

# Create a session
echo -e "\n3. Creating a remote control session..."
SESSION_RESPONSE=$(curl -s -k -X POST "${API_URL}/api/sessions" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "deviceId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    "name": "Test Remote Session",
    "type": 0,
    "requirePin": true,
    "invitedUsers": []
  }' 2>/dev/null || \
  curl -s -X POST "${HTTP_URL}/api/sessions" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "deviceId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    "name": "Test Remote Session",
    "type": 0,
    "requirePin": true,
    "invitedUsers": []
  }' 2>/dev/null)

echo "Session response: $SESSION_RESPONSE"

# Extract session ID
SESSION_ID=$(echo $SESSION_RESPONSE | grep -o '"id":"[^"]*' | sed 's/"id":"//')
echo "Session ID: $SESSION_ID"

if [ -z "$SESSION_ID" ]; then
    echo "Failed to create session. Exiting."
    exit 1
fi

# Get session details
echo -e "\n4. Getting session details..."
curl -s -k -X GET "${API_URL}/api/sessions/$SESSION_ID" \
  -H "Authorization: Bearer $TOKEN" 2>/dev/null || \
curl -s -X GET "${HTTP_URL}/api/sessions/$SESSION_ID" \
  -H "Authorization: Bearer $TOKEN" 2>/dev/null | jq '.' 2>/dev/null || echo "Response:"

# Test remote control start
echo -e "\n\n5. Starting remote control..."
START_RESPONSE=$(curl -s -k -X POST "${API_URL}/api/sessions/$SESSION_ID/start" \
  -H "Authorization: Bearer $TOKEN" 2>/dev/null || \
  curl -s -X POST "${HTTP_URL}/api/sessions/$SESSION_ID/start" \
  -H "Authorization: Bearer $TOKEN" 2>/dev/null)

echo "Start response: $START_RESPONSE"

# Check settings
echo -e "\n\n6. Checking current provider settings..."
curl -s -k -X GET "${API_URL}/api/settings" \
  -H "Authorization: Bearer $TOKEN" 2>/dev/null || \
curl -s -X GET "${HTTP_URL}/api/settings" \
  -H "Authorization: Bearer $TOKEN" 2>/dev/null | jq '.remoteControl.provider' 2>/dev/null || echo "Provider check failed"

echo -e "\n\nTest completed!"
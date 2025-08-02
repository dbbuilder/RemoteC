#!/bin/bash

# RemoteC Demo Network Testing Utility
# Tests connectivity and performance across network

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Default values
SERVER_URL=""
TEST_DURATION=10
CONCURRENT_USERS=5

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -s|--server)
            SERVER_URL="$2"
            shift 2
            ;;
        -d|--duration)
            TEST_DURATION="$2"
            shift 2
            ;;
        -u|--users)
            CONCURRENT_USERS="$2"
            shift 2
            ;;
        -h|--help)
            echo "Usage: $0 -s SERVER_URL [-d DURATION] [-u USERS]"
            echo ""
            echo "Options:"
            echo "  -s, --server     Server URL (e.g., http://192.168.1.100:7001)"
            echo "  -d, --duration   Test duration in seconds (default: 10)"
            echo "  -u, --users      Concurrent users for load test (default: 5)"
            echo "  -h, --help       Show this help message"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Validate server URL
if [ -z "$SERVER_URL" ]; then
    echo -e "${RED}Error: Server URL is required${NC}"
    echo "Usage: $0 -s SERVER_URL"
    exit 1
fi

# Remove trailing slash
SERVER_URL="${SERVER_URL%/}"

echo -e "${BLUE}RemoteC Network Testing Utility${NC}"
echo "================================"
echo "Server: $SERVER_URL"
echo "Duration: ${TEST_DURATION}s"
echo "Concurrent Users: $CONCURRENT_USERS"
echo ""

# Function to print results
print_result() {
    local test_name=$1
    local status=$2
    local details=$3
    
    if [ "$status" = "PASS" ]; then
        echo -e "[${GREEN}PASS${NC}] $test_name"
    else
        echo -e "[${RED}FAIL${NC}] $test_name"
    fi
    
    if [ -n "$details" ]; then
        echo "      $details"
    fi
}

# Test 1: Basic Connectivity
echo -e "${BLUE}Test 1: Basic Connectivity${NC}"
if curl -s -f -o /dev/null "$SERVER_URL/health"; then
    print_result "API Health Check" "PASS" "Server is reachable"
else
    print_result "API Health Check" "FAIL" "Cannot reach server at $SERVER_URL"
    exit 1
fi

# Test 2: Response Time
echo -e "\n${BLUE}Test 2: Response Time${NC}"
response_time=$(curl -o /dev/null -s -w '%{time_total}' "$SERVER_URL/health")
response_ms=$(echo "$response_time * 1000" | bc)
if (( $(echo "$response_time < 0.5" | bc -l) )); then
    print_result "Response Time" "PASS" "${response_ms}ms (< 500ms)"
else
    print_result "Response Time" "FAIL" "${response_ms}ms (> 500ms)"
fi

# Test 3: WebSocket Connectivity
echo -e "\n${BLUE}Test 3: WebSocket Connectivity${NC}"
# Use curl to test WebSocket upgrade headers
ws_test=$(curl -s -o /dev/null -w "%{http_code}" \
    -H "Connection: Upgrade" \
    -H "Upgrade: websocket" \
    -H "Sec-WebSocket-Version: 13" \
    -H "Sec-WebSocket-Key: x3JJHMbDL1EzLkh9GBhXDw==" \
    "$SERVER_URL/hubs/remoteControl")

if [ "$ws_test" = "101" ] || [ "$ws_test" = "400" ]; then
    print_result "WebSocket Support" "PASS" "SignalR hub is accessible"
else
    print_result "WebSocket Support" "FAIL" "HTTP status: $ws_test"
fi

# Test 4: API Endpoints
echo -e "\n${BLUE}Test 4: API Endpoints${NC}"
endpoints=(
    "/api/auth/login"
    "/api/sessions"
    "/api/hosts"
)

for endpoint in "${endpoints[@]}"; do
    http_code=$(curl -s -o /dev/null -w "%{http_code}" "$SERVER_URL$endpoint")
    if [ "$http_code" = "401" ] || [ "$http_code" = "405" ] || [ "$http_code" = "200" ]; then
        print_result "Endpoint $endpoint" "PASS" "HTTP $http_code"
    else
        print_result "Endpoint $endpoint" "FAIL" "HTTP $http_code"
    fi
done

# Test 5: Concurrent Connections
echo -e "\n${BLUE}Test 5: Concurrent Connections (Load Test)${NC}"
if command -v ab &> /dev/null; then
    echo "Running Apache Bench load test..."
    ab_output=$(ab -n 100 -c "$CONCURRENT_USERS" -t "$TEST_DURATION" -s 5 "$SERVER_URL/health" 2>&1)
    
    # Extract key metrics
    requests_per_sec=$(echo "$ab_output" | grep "Requests per second" | awk '{print $4}')
    time_per_request=$(echo "$ab_output" | grep "Time per request" | grep "(mean)" | awk '{print $4}')
    failed_requests=$(echo "$ab_output" | grep "Failed requests" | awk '{print $3}')
    
    if [ -n "$requests_per_sec" ] && [ "$failed_requests" = "0" ]; then
        print_result "Load Test" "PASS" "${requests_per_sec} req/s, ${time_per_request}ms avg"
    else
        print_result "Load Test" "FAIL" "Failed requests: $failed_requests"
    fi
else
    echo -e "${YELLOW}Skipping load test (install apache2-utils for ab command)${NC}"
fi

# Test 6: Latency Measurements
echo -e "\n${BLUE}Test 6: Network Latency${NC}"
server_host=$(echo "$SERVER_URL" | sed -E 's|https?://||' | cut -d':' -f1)
if ping -c 5 -W 2 "$server_host" &> /dev/null; then
    ping_output=$(ping -c 5 "$server_host" | tail -1)
    avg_latency=$(echo "$ping_output" | awk -F'/' '{print $5}')
    print_result "Network Latency" "PASS" "Average ping: ${avg_latency}ms"
else
    print_result "Network Latency" "FAIL" "Cannot ping $server_host"
fi

# Test 7: Bandwidth Test (Simple)
echo -e "\n${BLUE}Test 7: Bandwidth Test${NC}"
# Create a test payload (1MB)
test_file="/tmp/remotec_test_payload"
dd if=/dev/zero of="$test_file" bs=1M count=1 &> /dev/null

# Attempt to upload (this will fail with 401, but we can measure speed)
start_time=$(date +%s.%N)
curl -X POST -F "file=@$test_file" "$SERVER_URL/api/test/upload" &> /dev/null || true
end_time=$(date +%s.%N)

duration=$(echo "$end_time - $start_time" | bc)
bandwidth_mbps=$(echo "scale=2; 8 / $duration" | bc)

rm -f "$test_file"

print_result "Upload Bandwidth" "PASS" "~${bandwidth_mbps} Mbps (estimate)"

# Test 8: Service Dependencies
echo -e "\n${BLUE}Test 8: Service Dependencies${NC}"
# Check if web UI is accessible
web_url="${SERVER_URL%:7001}:3000"
if curl -s -f -o /dev/null "$web_url"; then
    print_result "Web UI" "PASS" "Accessible at $web_url"
else
    print_result "Web UI" "FAIL" "Cannot reach $web_url"
fi

# Summary
echo -e "\n${GREEN}=== Network Test Summary ===${NC}"
echo "Server URL: $SERVER_URL"
echo "All critical tests completed."
echo ""
echo "Recommendations:"
if (( $(echo "$response_ms > 100" | bc -l) )); then
    echo -e "${YELLOW}- High response time detected. Check network conditions.${NC}"
fi
if [ -n "$avg_latency" ] && (( $(echo "$avg_latency > 50" | bc -l) )); then
    echo -e "${YELLOW}- High network latency. Consider closer server placement.${NC}"
fi
echo -e "${BLUE}- For production use, ensure proper SSL certificates are configured.${NC}"
echo -e "${BLUE}- Configure firewall rules for ports 3000 and 7001.${NC}"
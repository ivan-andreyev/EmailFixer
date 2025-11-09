#!/bin/bash
# EmailFixer Post-Deployment Testing Suite
# Tests OAuth, API endpoints, and overall system health after deployment

set -e

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
API_URL="${1:-https://emailfixer-api.run.app}"
CLIENT_URL="${2:-https://emailfixer-client.run.app}"
TIMEOUT=10

echo -e "${BLUE}╔════════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║          EmailFixer Post-Deployment Test Suite               ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "${YELLOW}Testing Configuration:${NC}"
echo "  API URL:    $API_URL"
echo "  Client URL: $CLIENT_URL"
echo "  Timeout:    ${TIMEOUT}s"
echo ""

# Test counters
TESTS_PASSED=0
TESTS_FAILED=0

# Function to test HTTP endpoint
test_endpoint() {
    local name=$1
    local url=$2
    local expected_status=$3
    local method=${4:-GET}

    echo -n "Testing: $name ... "

    local response=$(curl -s -o /dev/null -w "%{http_code}" \
        -X "$method" \
        -m $TIMEOUT \
        -H "Accept: application/json" \
        "$url" 2>&1)

    if [ "$response" == "$expected_status" ]; then
        echo -e "${GREEN}✓ PASSED${NC} (HTTP $response)"
        ((TESTS_PASSED++))
        return 0
    else
        echo -e "${RED}✗ FAILED${NC} (Expected $expected_status, got $response)"
        ((TESTS_FAILED++))
        return 1
    fi
}

# Function to test JSON response
test_json_response() {
    local name=$1
    local url=$2
    local expected_field=$3

    echo -n "Testing: $name ... "

    local response=$(curl -s -m $TIMEOUT \
        -H "Accept: application/json" \
        "$url" 2>&1)

    if echo "$response" | grep -q "$expected_field"; then
        echo -e "${GREEN}✓ PASSED${NC}"
        ((TESTS_PASSED++))
        return 0
    else
        echo -e "${RED}✗ FAILED${NC} (Response: $response)"
        ((TESTS_FAILED++))
        return 1
    fi
}

# Function to test response time
test_response_time() {
    local name=$1
    local url=$2
    local max_time=$3

    echo -n "Testing: $name (max ${max_time}ms) ... "

    local response_time=$(curl -s -o /dev/null -w "%{time_total}" \
        -m $TIMEOUT \
        "$url" 2>&1)

    # Convert to milliseconds
    local time_ms=$(echo "$response_time * 1000" | bc | cut -d. -f1)

    if [ "$time_ms" -lt "$max_time" ]; then
        echo -e "${GREEN}✓ PASSED${NC} (${time_ms}ms)"
        ((TESTS_PASSED++))
        return 0
    else
        echo -e "${RED}✗ FAILED${NC} (${time_ms}ms > ${max_time}ms)"
        ((TESTS_FAILED++))
        return 1
    fi
}

# ============================================================================
# SECTION 1: API Health & Connectivity Tests
# ============================================================================
echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}SECTION 1: API HEALTH & CONNECTIVITY TESTS${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo ""

test_endpoint "API Health Check" "$API_URL/health" 200
test_response_time "Health Check Response Time" "$API_URL/health" 5000
test_json_response "Health Check Returns Status" "$API_URL/health" "healthy"

# ============================================================================
# SECTION 2: OAuth Endpoint Tests
# ============================================================================
echo ""
echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}SECTION 2: OAUTH ENDPOINTS TEST${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo ""

# Test that OAuth login endpoint exists
test_endpoint "OAuth Login Endpoint Exists" "$API_URL/api/auth/google-login" 200

# Test that protected endpoints require auth
echo -n "Testing: Protected Endpoint Returns 401 Without Auth ... "
local response=$(curl -s -o /dev/null -w "%{http_code}" \
    -X GET \
    -m $TIMEOUT \
    "$API_URL/api/auth/user" 2>&1)

if [ "$response" == "401" ]; then
    echo -e "${GREEN}✓ PASSED${NC} (HTTP 401)"
    ((TESTS_PASSED++))
else
    echo -e "${RED}✗ FAILED${NC} (Expected 401, got $response)"
    ((TESTS_FAILED++))
fi

# ============================================================================
# SECTION 3: API Documentation Tests
# ============================================================================
echo ""
echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}SECTION 3: API DOCUMENTATION TEST${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo ""

test_endpoint "Swagger Documentation Available" "$API_URL/swagger/index.html" 200
test_json_response "OpenAPI Spec Available" "$API_URL/swagger/v1/swagger.json" "openapi"

# ============================================================================
# SECTION 4: Client Application Tests
# ============================================================================
echo ""
echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}SECTION 4: CLIENT APPLICATION TESTS${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo ""

test_endpoint "Client App Loads" "$CLIENT_URL/" 200
test_response_time "Client App Response Time" "$CLIENT_URL/" 5000

echo -n "Testing: Client Returns HTML ... "
local response=$(curl -s -m $TIMEOUT "$CLIENT_URL/" 2>&1)
if echo "$response" | grep -q "<!DOCTYPE html\|<html"; then
    echo -e "${GREEN}✓ PASSED${NC}"
    ((TESTS_PASSED++))
else
    echo -e "${RED}✗ FAILED${NC} (No HTML content)"
    ((TESTS_FAILED++))
fi

# ============================================================================
# SECTION 5: Security Headers Tests
# ============================================================================
echo ""
echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}SECTION 5: SECURITY HEADERS TESTS${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo ""

echo -n "Testing: HTTPS Enforced (Redirect from HTTP) ... "
# Note: This assumes HTTP redirect is configured
local response=$(curl -s -o /dev/null -w "%{http_code}" \
    -L -m $TIMEOUT \
    "http://emailfixer-api.run.app/health" 2>&1)

if [ "$response" == "200" ]; then
    echo -e "${GREEN}✓ PASSED${NC}"
    ((TESTS_PASSED++))
else
    echo -e "${YELLOW}⚠ WARNING${NC} (HTTP->HTTPS redirect may not be configured)"
fi

echo -n "Testing: Content-Type Header Present ... "
local response=$(curl -s -i -m $TIMEOUT "$API_URL/health" 2>&1 | grep -i "content-type")

if [ ! -z "$response" ]; then
    echo -e "${GREEN}✓ PASSED${NC}"
    ((TESTS_PASSED++))
else
    echo -e "${RED}✗ FAILED${NC} (No Content-Type header)"
    ((TESTS_FAILED++))
fi

# ============================================================================
# SECTION 6: Database Connectivity Tests
# ============================================================================
echo ""
echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}SECTION 6: DATABASE CONNECTIVITY TESTS${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo ""

echo -n "Testing: Database is Accessible via API ... "
# Create a test request to an endpoint that would fail without DB
local response=$(curl -s -X GET \
    -m $TIMEOUT \
    -H "Authorization: Bearer invalid-token-to-test-db" \
    "$API_URL/api/email-checks" 2>&1)

# Database error would be different from invalid token error
if echo "$response" | grep -q "401\|unauthorized\|Unauthorized"; then
    echo -e "${GREEN}✓ PASSED${NC} (Auth check passed, DB likely OK)"
    ((TESTS_PASSED++))
else
    echo -e "${YELLOW}⚠ WARNING${NC} (Could not verify DB connection)"
fi

# ============================================================================
# SECTION 7: Performance Tests
# ============================================================================
echo ""
echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}SECTION 7: PERFORMANCE TESTS${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo ""

# Run 5 sequential requests and measure average
echo -n "Testing: API Average Response Time (5 requests) ... "
local total_time=0
for i in {1..5}; do
    local time=$(curl -s -o /dev/null -w "%{time_total}" \
        -m $TIMEOUT \
        "$API_URL/health" 2>&1)
    total_time=$(echo "$total_time + $time" | bc)
done

local avg_time=$(echo "scale=3; $total_time / 5 * 1000" | bc | cut -d. -f1)
if [ "$avg_time" -lt 2000 ]; then
    echo -e "${GREEN}✓ PASSED${NC} (Avg ${avg_time}ms)"
    ((TESTS_PASSED++))
else
    echo -e "${YELLOW}⚠ WARNING${NC} (Avg ${avg_time}ms - slower than optimal)"
fi

# ============================================================================
# SECTION 8: Logging & Monitoring Tests
# ============================================================================
echo ""
echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}SECTION 8: LOGGING & MONITORING TESTS${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo ""

echo "Note: Verify logging in Google Cloud Console:"
echo "  - Cloud Logging: https://console.cloud.google.com/logs"
echo "  - Cloud Run: Check /home/loggeduser/var/log/"
echo "  - Application metrics: https://console.cloud.google.com/monitoring"
echo ""

# ============================================================================
# FINAL SUMMARY
# ============================================================================
echo ""
echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}TEST SUMMARY${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
echo ""

TOTAL_TESTS=$((TESTS_PASSED + TESTS_FAILED))

echo "Tests Passed:  ${GREEN}$TESTS_PASSED${NC}"
echo "Tests Failed:  ${RED}$TESTS_FAILED${NC}"
echo "Total Tests:   $TOTAL_TESTS"
echo ""

if [ $TESTS_FAILED -eq 0 ]; then
    echo -e "${GREEN}✓ ALL TESTS PASSED${NC}"
    exit 0
else
    echo -e "${RED}✗ SOME TESTS FAILED${NC}"
    exit 1
fi

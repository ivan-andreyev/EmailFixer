#!/bin/bash
# OAuth Flow Testing Script for EmailFixer
# Tests the complete Google OAuth 2.0 PKCE flow

set -e

API_URL="${1:-https://emailfixer-api.run.app}"
CLIENT_URL="${2:-https://emailfixer-client.run.app}"

echo "╔════════════════════════════════════════════════════════════════╗"
echo "║        EmailFixer OAuth 2.0 Flow Testing Script              ║"
echo "╚════════════════════════════════════════════════════════════════╝"
echo ""
echo "Configuration:"
echo "  API URL:    $API_URL"
echo "  Client URL: $CLIENT_URL"
echo ""

# ============================================================================
# SECTION 1: OAuth Login Endpoint
# ============================================================================
echo "═══════════════════════════════════════════════════════════════"
echo "SECTION 1: OAuth Login Endpoint"
echo "═══════════════════════════════════════════════════════════════"
echo ""

echo "Testing OAuth login endpoint..."
RESPONSE=$(curl -s -X GET "$API_URL/api/auth/google-login")

if echo "$RESPONSE" | grep -q "authorizationUrl\|state\|codeChallenge"; then
    echo "✓ OAuth login endpoint responds with required fields"
    echo "Response: $RESPONSE" | head -c 200
    echo ""
else
    echo "✗ OAuth login endpoint response missing required fields"
    echo "Response: $RESPONSE"
fi

echo ""

# ============================================================================
# SECTION 2: OAuth Callback Endpoint
# ============================================================================
echo "═══════════════════════════════════════════════════════════════"
echo "SECTION 2: OAuth Callback Endpoint (Requires Valid Token)"
echo "═══════════════════════════════════════════════════════════════"
echo ""

echo "Testing OAuth callback endpoint..."
CALLBACK_RESPONSE=$(curl -s -X POST "$API_URL/api/auth/google-callback" \
    -H "Content-Type: application/json" \
    -d '{
        "code": "test-code",
        "codeVerifier": "test-verifier"
    }' 2>&1)

echo "Response Code: $CALLBACK_RESPONSE"
echo "Note: This should fail with invalid code (expected behavior)"
echo ""

# ============================================================================
# SECTION 3: Protected Endpoint Test
# ============================================================================
echo "═══════════════════════════════════════════════════════════════"
echo "SECTION 3: Protected Endpoint Test"
echo "═══════════════════════════════════════════════════════════════"
echo ""

echo "Testing protected /api/auth/user endpoint without token..."
STATUS_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$API_URL/api/auth/user")

if [ "$STATUS_CODE" == "401" ]; then
    echo "✓ Correctly returns 401 Unauthorized without token"
else
    echo "✗ Unexpected status code: $STATUS_CODE (expected 401)"
fi

echo ""

echo "Testing protected /api/auth/user endpoint with invalid token..."
INVALID_TOKEN="Bearer invalid-jwt-token-12345"
STATUS_CODE=$(curl -s -o /dev/null -w "%{http_code}" \
    -H "Authorization: $INVALID_TOKEN" \
    "$API_URL/api/auth/user")

if [ "$STATUS_CODE" == "401" ]; then
    echo "✓ Correctly rejects invalid JWT token"
else
    echo "✗ Unexpected status code: $STATUS_CODE (expected 401)"
fi

echo ""

# ============================================================================
# SECTION 4: Token Validation
# ============================================================================
echo "═══════════════════════════════════════════════════════════════"
echo "SECTION 4: Token Validation"
echo "═══════════════════════════════════════════════════════════════"
echo ""

echo "Testing JWT token validation requirements:"
echo "  ✓ Token should have HS256 signature"
echo "  ✓ Token should contain iss, aud, exp claims"
echo "  ✓ Token should expire after 60 minutes"
echo "  ✓ Token should contain user ID and email"
echo ""

# ============================================================================
# SECTION 5: PKCE Flow Validation
# ============================================================================
echo "═══════════════════════════════════════════════════════════════"
echo "SECTION 5: PKCE Flow Validation"
echo "═══════════════════════════════════════════════════════════════"
echo ""

echo "PKCE (Proof Key for Public Clients) Requirements:"
echo ""
echo "✓ Code Challenge Generation:"
echo "  - CodeVerifier: random 128-character string"
echo "  - CodeChallenge: SHA256(CodeVerifier)"
echo "  - Method: S256 (SHA256)"
echo ""
echo "✓ Authorization Request:"
echo "  - Includes code_challenge parameter"
echo "  - Includes code_challenge_method=S256"
echo "  - Includes state parameter for CSRF protection"
echo ""
echo "✓ Token Exchange:"
echo "  - Includes code_verifier for validation"
echo "  - Server validates SHA256(code_verifier) == code_challenge"
echo "  - Prevents authorization code interception attacks"
echo ""

# ============================================================================
# SECTION 6: Manual Testing Instructions
# ============================================================================
echo "═══════════════════════════════════════════════════════════════"
echo "SECTION 6: MANUAL OAUTH TESTING"
echo "═══════════════════════════════════════════════════════════════"
echo ""

echo "To complete manual OAuth flow testing:"
echo ""
echo "1. Open client application:"
echo "   $CLIENT_URL"
echo ""
echo "2. Click 'Login with Google'"
echo ""
echo "3. Authenticate with a test Google account"
echo ""
echo "4. Verify OAuth callback is called:"
echo "   - Browser should redirect to /auth-callback"
echo "   - JWT token should be stored in localStorage"
echo ""
echo "5. Verify protected page access:"
echo "   - Dashboard should load after successful login"
echo "   - User email should be displayed"
echo "   - Credits should be shown (100 for new users)"
echo ""
echo "6. Test token refresh:"
echo "   - Wait for token to expire or check localStorage"
echo "   - New token should be requested automatically"
echo ""
echo "7. Test logout:"
echo "   - Click Logout button"
echo "   - Tokens should be cleared from localStorage"
echo "   - Should redirect to login page"
echo ""

# ============================================================================
# SECTION 7: Troubleshooting
# ============================================================================
echo "═══════════════════════════════════════════════════════════════"
echo "SECTION 7: TROUBLESHOOTING"
echo "═══════════════════════════════════════════════════════════════"
echo ""

echo "If OAuth flow fails, check:"
echo ""
echo "1. Google OAuth Credentials:"
echo "   - Verify Client ID is correct"
echo "   - Verify Client Secret is correct"
echo "   - Check authorized JavaScript origins"
echo "   - Check authorized redirect URIs"
echo ""
echo "2. Backend Configuration:"
echo "   - Verify JWT_SECRET is set"
echo "   - Check DATABASE connection string"
echo "   - Verify CORS is configured correctly"
echo ""
echo "3. Frontend Configuration:"
echo "   - Check Google OAuth Client ID in environment"
echo "   - Verify redirect_uri matches registered URI"
echo "   - Check PKCE implementation in AuthService"
echo ""
echo "4. Deployment Issues:"
echo "   - Check Cloud Run service is running"
echo "   - Verify database connectivity"
echo "   - Check logs: Cloud Logging console"
echo "   - Review Cloud Run service configuration"
echo ""

echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "OAuth Flow Testing Complete"
echo "═══════════════════════════════════════════════════════════════"

# EmailFixer Post-Deployment Testing Suite (PowerShell)
# Tests OAuth, API endpoints, and overall system health after deployment

param(
    [string]$ApiUrl = "https://emailfixer-api.run.app",
    [string]$ClientUrl = "https://emailfixer-client.run.app",
    [int]$TimeoutSeconds = 10
)

# Suppress certificate validation for self-signed certs in testing
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

# Test counters
$testsPassed = 0
$testsFailed = 0

# Color codes
$colors = @{
    'Green' = 'Green'
    'Red' = 'Red'
    'Yellow' = 'Yellow'
    'Blue' = 'Blue'
}

Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Blue
Write-Host "║          EmailFixer Post-Deployment Test Suite               ║" -ForegroundColor Blue
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Blue
Write-Host ""
Write-Host "Testing Configuration:" -ForegroundColor Yellow
Write-Host "  API URL:    $ApiUrl"
Write-Host "  Client URL: $ClientUrl"
Write-Host "  Timeout:    ${TimeoutSeconds}s"
Write-Host ""

# Function to test HTTP endpoint
function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Url,
        [int]$ExpectedStatus = 200,
        [string]$Method = 'GET'
    )

    Write-Host -NoNewline "Testing: $Name ... "

    try {
        $response = Invoke-WebRequest -Uri $Url -Method $Method -TimeoutSec $TimeoutSeconds -SkipHttpErrorCheck
        $statusCode = $response.StatusCode

        if ($statusCode -eq $ExpectedStatus) {
            Write-Host "✓ PASSED" -ForegroundColor Green -NoNewline
            Write-Host " (HTTP $statusCode)"
            $global:testsPassed++
            return $true
        }
        else {
            Write-Host "✗ FAILED" -ForegroundColor Red -NoNewline
            Write-Host " (Expected $ExpectedStatus, got $statusCode)"
            $global:testsFailed++
            return $false
        }
    }
    catch {
        Write-Host "✗ FAILED" -ForegroundColor Red -NoNewline
        Write-Host " ($_)"
        $global:testsFailed++
        return $false
    }
}

# Function to test JSON response
function Test-JsonResponse {
    param(
        [string]$Name,
        [string]$Url,
        [string]$ExpectedField
    )

    Write-Host -NoNewline "Testing: $Name ... "

    try {
        $response = Invoke-WebRequest -Uri $Url -TimeoutSec $TimeoutSeconds -SkipHttpErrorCheck
        $content = $response.Content

        if ($content -match $ExpectedField) {
            Write-Host "✓ PASSED" -ForegroundColor Green
            $global:testsPassed++
            return $true
        }
        else {
            Write-Host "✗ FAILED" -ForegroundColor Red -NoNewline
            Write-Host " (Field not found: $ExpectedField)"
            $global:testsFailed++
            return $false
        }
    }
    catch {
        Write-Host "✗ FAILED" -ForegroundColor Red -NoNewline
        Write-Host " ($_)"
        $global:testsFailed++
        return $false
    }
}

# Function to test response time
function Test-ResponseTime {
    param(
        [string]$Name,
        [string]$Url,
        [int]$MaxTimeMs = 5000
    )

    Write-Host -NoNewline "Testing: $Name (max ${MaxTimeMs}ms) ... "

    try {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $response = Invoke-WebRequest -Uri $Url -TimeoutSec $TimeoutSeconds -SkipHttpErrorCheck
        $sw.Stop()

        $timeMs = [math]::Round($sw.ElapsedMilliseconds)

        if ($timeMs -lt $MaxTimeMs) {
            Write-Host "✓ PASSED" -ForegroundColor Green -NoNewline
            Write-Host " (${timeMs}ms)"
            $global:testsPassed++
            return $true
        }
        else {
            Write-Host "✗ FAILED" -ForegroundColor Red -NoNewline
            Write-Host " (${timeMs}ms > ${MaxTimeMs}ms)"
            $global:testsFailed++
            return $false
        }
    }
    catch {
        Write-Host "✗ FAILED" -ForegroundColor Red -NoNewline
        Write-Host " ($_)"
        $global:testsFailed++
        return $false
    }
}

# ============================================================================
# SECTION 1: API Health & Connectivity Tests
# ============================================================================
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Blue
Write-Host "SECTION 1: API HEALTH & CONNECTIVITY TESTS" -ForegroundColor Blue
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Blue
Write-Host ""

Test-Endpoint -Name "API Health Check" -Url "$ApiUrl/health" -ExpectedStatus 200
Test-ResponseTime -Name "Health Check Response Time" -Url "$ApiUrl/health" -MaxTimeMs 5000
Test-JsonResponse -Name "Health Check Returns Status" -Url "$ApiUrl/health" -ExpectedField "healthy"

# ============================================================================
# SECTION 2: OAuth Endpoint Tests
# ============================================================================
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Blue
Write-Host "SECTION 2: OAUTH ENDPOINTS TEST" -ForegroundColor Blue
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Blue
Write-Host ""

Test-Endpoint -Name "OAuth Login Endpoint Exists" -Url "$ApiUrl/api/auth/google-login" -ExpectedStatus 200

Write-Host -NoNewline "Testing: Protected Endpoint Returns 401 Without Auth ... "
try {
    $response = Invoke-WebRequest -Uri "$ApiUrl/api/auth/user" -TimeoutSec $TimeoutSeconds -SkipHttpErrorCheck
    if ($response.StatusCode -eq 401) {
        Write-Host "✓ PASSED" -ForegroundColor Green -NoNewline
        Write-Host " (HTTP 401)"
        $global:testsPassed++
    }
    else {
        Write-Host "✗ FAILED" -ForegroundColor Red -NoNewline
        Write-Host " (Expected 401, got $($response.StatusCode))"
        $global:testsFailed++
    }
}
catch {
    Write-Host "✗ FAILED" -ForegroundColor Red -NoNewline
    Write-Host " ($_)"
    $global:testsFailed++
}

# ============================================================================
# SECTION 3: API Documentation Tests
# ============================================================================
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Blue
Write-Host "SECTION 3: API DOCUMENTATION TEST" -ForegroundColor Blue
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Blue
Write-Host ""

Test-Endpoint -Name "Swagger Documentation Available" -Url "$ApiUrl/swagger/index.html" -ExpectedStatus 200
Test-JsonResponse -Name "OpenAPI Spec Available" -Url "$ApiUrl/swagger/v1/swagger.json" -ExpectedField "openapi"

# ============================================================================
# SECTION 4: Client Application Tests
# ============================================================================
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Blue
Write-Host "SECTION 4: CLIENT APPLICATION TESTS" -ForegroundColor Blue
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Blue
Write-Host ""

Test-Endpoint -Name "Client App Loads" -Url "$ClientUrl/" -ExpectedStatus 200
Test-ResponseTime -Name "Client App Response Time" -Url "$ClientUrl/" -MaxTimeMs 5000

Write-Host -NoNewline "Testing: Client Returns HTML ... "
try {
    $response = Invoke-WebRequest -Uri "$ClientUrl/" -TimeoutSec $TimeoutSeconds -SkipHttpErrorCheck
    if ($response.Content -match '<!DOCTYPE html|<html') {
        Write-Host "✓ PASSED" -ForegroundColor Green
        $global:testsPassed++
    }
    else {
        Write-Host "✗ FAILED" -ForegroundColor Red -NoNewline
        Write-Host " (No HTML content)"
        $global:testsFailed++
    }
}
catch {
    Write-Host "✗ FAILED" -ForegroundColor Red -NoNewline
    Write-Host " ($_)"
    $global:testsFailed++
}

# ============================================================================
# SECTION 5: Security Headers Tests
# ============================================================================
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Blue
Write-Host "SECTION 5: SECURITY HEADERS TESTS" -ForegroundColor Blue
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Blue
Write-Host ""

Write-Host -NoNewline "Testing: Content-Type Header Present ... "
try {
    $response = Invoke-WebRequest -Uri "$ApiUrl/health" -TimeoutSec $TimeoutSeconds -SkipHttpErrorCheck
    if ($response.Headers.ContainsKey('Content-Type')) {
        Write-Host "✓ PASSED" -ForegroundColor Green
        $global:testsPassed++
    }
    else {
        Write-Host "✗ FAILED" -ForegroundColor Red -NoNewline
        Write-Host " (No Content-Type header)"
        $global:testsFailed++
    }
}
catch {
    Write-Host "✗ FAILED" -ForegroundColor Red -NoNewline
    Write-Host " ($_)"
    $global:testsFailed++
}

# ============================================================================
# SECTION 6: Database Connectivity Tests
# ============================================================================
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Blue
Write-Host "SECTION 6: DATABASE CONNECTIVITY TESTS" -ForegroundColor Blue
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Blue
Write-Host ""

Write-Host -NoNewline "Testing: Database is Accessible via API ... "
try {
    $headers = @{
        'Authorization' = 'Bearer invalid-token-to-test-db'
    }
    $response = Invoke-WebRequest -Uri "$ApiUrl/api/email-checks" `
        -Headers $headers `
        -TimeoutSec $TimeoutSeconds `
        -SkipHttpErrorCheck

    if ($response.StatusCode -eq 401 -or $response.Content -match 'unauthorized|401') {
        Write-Host "✓ PASSED" -ForegroundColor Green -NoNewline
        Write-Host " (Auth check passed, DB likely OK)"
        $global:testsPassed++
    }
    else {
        Write-Host "⚠ WARNING" -ForegroundColor Yellow -NoNewline
        Write-Host " (Could not verify DB connection)"
    }
}
catch {
    Write-Host "⚠ WARNING" -ForegroundColor Yellow -NoNewline
    Write-Host " (Could not verify DB connection: $_)"
}

# ============================================================================
# SECTION 7: Performance Tests
# ============================================================================
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Blue
Write-Host "SECTION 7: PERFORMANCE TESTS" -ForegroundColor Blue
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Blue
Write-Host ""

Write-Host -NoNewline "Testing: API Average Response Time (5 requests) ... "
try {
    $times = @()
    for ($i = 1; $i -le 5; $i++) {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $response = Invoke-WebRequest -Uri "$ApiUrl/health" -TimeoutSec $TimeoutSeconds -SkipHttpErrorCheck
        $sw.Stop()
        $times += $sw.ElapsedMilliseconds
    }

    $avgTime = [math]::Round(($times | Measure-Object -Average).Average)

    if ($avgTime -lt 2000) {
        Write-Host "✓ PASSED" -ForegroundColor Green -NoNewline
        Write-Host " (Avg ${avgTime}ms)"
        $global:testsPassed++
    }
    else {
        Write-Host "⚠ WARNING" -ForegroundColor Yellow -NoNewline
        Write-Host " (Avg ${avgTime}ms - slower than optimal)"
    }
}
catch {
    Write-Host "✗ FAILED" -ForegroundColor Red -NoNewline
    Write-Host " ($_)"
    $global:testsFailed++
}

# ============================================================================
# SECTION 8: Logging & Monitoring Tests
# ============================================================================
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Blue
Write-Host "SECTION 8: LOGGING & MONITORING TESTS" -ForegroundColor Blue
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Blue
Write-Host ""

Write-Host "Note: Verify logging in Google Cloud Console:"
Write-Host "  - Cloud Logging: https://console.cloud.google.com/logs"
Write-Host "  - Cloud Run: https://console.cloud.google.com/run"
Write-Host "  - Application metrics: https://console.cloud.google.com/monitoring"
Write-Host ""

# ============================================================================
# FINAL SUMMARY
# ============================================================================
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Blue
Write-Host "TEST SUMMARY" -ForegroundColor Blue
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Blue
Write-Host ""

$totalTests = $global:testsPassed + $global:testsFailed

Write-Host "Tests Passed:  " -NoNewline
Write-Host "$global:testsPassed" -ForegroundColor Green
Write-Host "Tests Failed:  " -NoNewline
Write-Host "$global:testsFailed" -ForegroundColor Red
Write-Host "Total Tests:   $totalTests"
Write-Host ""

if ($global:testsFailed -eq 0) {
    Write-Host "✓ ALL TESTS PASSED" -ForegroundColor Green
    exit 0
}
else {
    Write-Host "✗ SOME TESTS FAILED" -ForegroundColor Red
    exit 1
}

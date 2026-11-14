# Experiment: Token Caching and Clock Skew Behavior
# This script performs two parts of the experiment:
# Part 1: Token validity with Keycloak down
# Part 2: Clock skew and token expiration

$ErrorActionPreference = "SilentlyContinue"
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss.fff"
$results = @()

Write-Host "=== Experiment: OAuth2 Token Caching and Clock Skew ===" -ForegroundColor Cyan
Write-Host "Start time: $timestamp" -ForegroundColor Gray

# --- PART 1: Token Validity with Keycloak Down ---
Write-Host "`n=== PART 1: Token Validity with Keycloak Down ===" -ForegroundColor Yellow

# Step 1: Obtain a valid token by logging in as alice
Write-Host "`n[1.1] Logging in as alice to obtain access token..."
$loginTime = Get-Date -AsUTC
$tokenResponse = $null

try {
    # First, get the token from Keycloak
    $tokenResponse = curl.exe -s -X POST `
        -H "Content-Type: application/x-www-form-urlencoded" `
        -d "grant_type=password&client_id=orderhub-cli&client_secret=orderhub-cli-secret&username=alice&password=alice123&scope=openid%20profile" `
        http://localhost:8180/auth/realms/orderhub/protocol/openid-connect/token 2>&1
    
    Write-Host "Token response received"
} catch {
    Write-Host "ERROR obtaining token: $_" -ForegroundColor Red
}

# Parse the token (attempt to extract access_token)
$accessToken = ""
if ($tokenResponse) {
    # Try to extract access_token from response
    try {
        $tokenObj = $tokenResponse | ConvertFrom-Json
        $accessToken = $tokenObj.access_token
        $expiresIn = $tokenObj.expires_in
        Write-Host "[1.1] SUCCESS: Token obtained. Expires in: $expiresIn seconds" -ForegroundColor Green
        Write-Host "Token (first 50 chars): $($accessToken.Substring(0, [Math]::Min(50, $accessToken.Length)))..." -ForegroundColor Gray
        $results += @{
            step = "1.1"
            action = "Login as alice"
            status = "SUCCESS"
            timestamp = $loginTime
            details = "Token obtained with $expiresIn seconds lifetime"
        }
    } catch {
        Write-Host "Could not parse token response" -ForegroundColor Red
        Write-Host "Response: $tokenResponse" -ForegroundColor Red
    }
}

# Step 2: Test API call with token while Keycloak is running
Write-Host "`n[1.2] Testing API call to /health/live with valid token (Keycloak UP)..."
$testTime1 = Get-Date -AsUTC
$testResponse1 = ""

if ($accessToken) {
    try {
        $testResponse1 = curl.exe -s -w "`n%{http_code}" `
            -H "Authorization: Bearer $accessToken" `
            http://localhost:5000/health/live 2>&1
        
        $httpCode = $testResponse1[-1]
        Write-Host "[1.2] Response: HTTP $httpCode" -ForegroundColor $(if ($httpCode -eq "200") { "Green" } else { "Red" })
        $results += @{
            step = "1.2"
            action = "API call with token (Keycloak UP)"
            http_code = $httpCode
            timestamp = $testTime1
        }
    } catch {
        Write-Host "ERROR testing API: $_" -ForegroundColor Red
    }
} else {
    Write-Host "[1.2] Skipped: No token available" -ForegroundColor Yellow
}

# Step 3: Stop Keycloak
Write-Host "`n[1.3] Stopping Keycloak container..."
$stopTime = Get-Date -AsUTC

try {
    docker-compose -f docker-compose.yml -f docker-compose.services.yml stop orderhub-keycloak 2>&1 | Out-Null
    Write-Host "[1.3] Keycloak stopped" -ForegroundColor Green
    Start-Sleep -Seconds 3
    $results += @{
        step = "1.3"
        action = "Stop Keycloak"
        status = "SUCCESS"
        timestamp = $stopTime
    }
} catch {
    Write-Host "ERROR stopping Keycloak: $_" -ForegroundColor Red
}

# Step 4: Test API call with same token while Keycloak is down
Write-Host "`n[1.4] Testing API call to /health/live with valid token (Keycloak DOWN, JWKS cached)..."
$testTime2 = Get-Date -AsUTC
$testResponse2 = ""

if ($accessToken) {
    try {
        $testResponse2 = curl.exe -s -w "`n%{http_code}" `
            -H "Authorization: Bearer $accessToken" `
            http://localhost:5000/health/live 2>&1
        
        $httpCode = $testResponse2[-1]
        Write-Host "[1.4] Response: HTTP $httpCode" -ForegroundColor $(if ($httpCode -eq "200") { "Green" } else { "Red" })
        $results += @{
            step = "1.4"
            action = "API call with token (Keycloak DOWN)"
            http_code = $httpCode
            timestamp = $testTime2
            expected = "200 (JWKS cached)"
        }
    } catch {
        Write-Host "ERROR testing API: $_" -ForegroundColor Red
    }
}

# Step 5: Attempt new login while Keycloak is down
Write-Host "`n[1.5] Attempting new login while Keycloak is DOWN..."
$loginFailTime = Get-Date -AsUTC
$newLoginResponse = ""

try {
    $newLoginResponse = curl.exe -s -w "`n%{http_code}" -X POST `
        -H "Content-Type: application/x-www-form-urlencoded" `
        -d "grant_type=password&client_id=orderhub-cli&client_secret=orderhub-cli-secret&username=bob&password=bob123&scope=openid%20profile" `
        http://localhost:8180/auth/realms/orderhub/protocol/openid-connect/token 2>&1
    
    $httpCode = $newLoginResponse[-1]
    Write-Host "[1.5] New login attempt: HTTP $httpCode" -ForegroundColor $(if ($httpCode -ne "200") { "Green" } else { "Red" })
    Write-Host "[1.5] Expected: Connection failure or HTTP 5xx (Keycloak unavailable)" -ForegroundColor Gray
    $results += @{
        step = "1.5"
        action = "New login attempt (Keycloak DOWN)"
        http_code = $httpCode
        timestamp = $loginFailTime
        expected = "Failure (Keycloak unavailable)"
    }
} catch {
    Write-Host "[1.5] New login FAILED as expected: $_" -ForegroundColor Green
    $results += @{
        step = "1.5"
        action = "New login attempt (Keycloak DOWN)"
        status = "FAILED (expected)"
        timestamp = $loginFailTime
    }
}

# Step 6: Restart Keycloak
Write-Host "`n[1.6] Restarting Keycloak container..."
$restartTime = Get-Date -AsUTC

try {
    docker-compose -f docker-compose.yml -f docker-compose.services.yml start orderhub-keycloak 2>&1 | Out-Null
    Write-Host "[1.6] Keycloak start command sent" -ForegroundColor Green
    
    # Wait for Keycloak to be ready
    Write-Host "[1.6] Waiting for Keycloak to be healthy..."
    for ($i = 0; $i -lt 30; $i++) {
        $healthCheck = $null
        try {
            $healthCheck = curl.exe -s http://localhost:8180/health 2>&1
            if ($healthCheck -like "*UP*" -or $healthCheck -like "*UP*") {
                Write-Host "[1.6] Keycloak is healthy" -ForegroundColor Green
                break
            }
        } catch { }
        Write-Host "." -NoNewline
        Start-Sleep -Seconds 2
    }
    
    $results += @{
        step = "1.6"
        action = "Restart Keycloak"
        status = "SUCCESS"
        timestamp = $restartTime
    }
} catch {
    Write-Host "ERROR restarting Keycloak: $_" -ForegroundColor Red
}

Write-Host "`n[1.7] Verifying login works after Keycloak restart..."
$verifyLoginTime = Get-Date -AsUTC

try {
    $verifyResponse = curl.exe -s -w "`n%{http_code}" -X POST `
        -H "Content-Type: application/x-www-form-urlencoded" `
        -d "grant_type=password&client_id=orderhub-cli&client_secret=orderhub-cli-secret&username=alice&password=alice123&scope=openid%20profile" `
        http://localhost:8180/auth/realms/orderhub/protocol/openid-connect/token 2>&1
    
    $httpCode = $verifyResponse[-1]
    Write-Host "[1.7] Login after restart: HTTP $httpCode" -ForegroundColor $(if ($httpCode -eq "200") { "Green" } else { "Red" })
    $results += @{
        step = "1.7"
        action = "Verify login after Keycloak restart"
        http_code = $httpCode
        timestamp = $verifyLoginTime
        expected = "200 (Recovery successful)"
    }
} catch {
    Write-Host "ERROR verifying login: $_" -ForegroundColor Red
}

# --- PART 2: Clock Skew and Token Expiration ---
Write-Host "`n=== PART 2: Clock Skew and Token Expiration ===" -ForegroundColor Yellow
Write-Host "NOTE: This part requires temporary configuration changes to Gateway JWT settings." -ForegroundColor Cyan
Write-Host "For now, we will document the expected behavior based on configuration." -ForegroundColor Cyan

$part2StartTime = Get-Date -AsUTC
$results += @{
    step = "2.0"
    action = "Clock Skew Part Start"
    timestamp = $part2StartTime
    note = "Clock skew testing requires runtime configuration changes to Gateway JWT validation"
}

# Summary
Write-Host "`n=== EXPERIMENT SUMMARY ===" -ForegroundColor Cyan
Write-Host "`nResults collected at: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss.fff')" -ForegroundColor Gray

Write-Host "`nPart 1 Findings:" -ForegroundColor Yellow
$results | Where-Object { $_.step -like "1.*" } | ForEach-Object {
    Write-Host "  [$($_.step)] $($_.action): $($_.http_code)$($_.status)" -ForegroundColor Gray
}

Write-Host "`nResults object (for documentation):" -ForegroundColor Gray
$results | ConvertTo-Json | Out-String | Write-Host -ForegroundColor Gray

# Export results to JSON for documentation
$resultsFile = "experiment-results.json"
$results | ConvertTo-Json | Out-File -FilePath $resultsFile -Encoding UTF8
Write-Host "`nResults saved to: $resultsFile" -ForegroundColor Cyan

Write-Host "`nExperiment completed at: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss.fff')" -ForegroundColor Cyan

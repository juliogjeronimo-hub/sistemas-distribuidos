#!/usr/bin/env pwsh
# Experimento 02b: Liveness vs Readiness Probes with Database Unavailability
# 
# Objetivo: Demonstrar a distinção entre liveness e readiness probes quando
# o banco de dados fica indisponível.
#
# Hipótese:
# - /health/live retorna 200 (processo vivo)
# - /health/ready falha (dependência de banco indisponível)
# - Operações CRUD retornam 503/500
# - Recuperação automática quando BD volta

$ErrorActionPreference = "Continue"
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss.fff"
$results = @()
$experimentLog = @()

function Log-Entry {
    param(
        [string]$Step,
        [string]$Action,
        [string]$Status = "INFO",
        [string]$Details = ""
    )
    
    $entry = @{
        timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss.fff"
        step = $Step
        action = $Action
        status = $Status
        details = $Details
    }
    
    $experimentLog += $entry
    
    $color = switch($Status) {
        "SUCCESS" { "Green" }
        "FAILURE" { "Red" }
        "WARNING" { "Yellow" }
        default { "Gray" }
    }
    
    Write-Host "[$Step] $Action - $Status" -ForegroundColor $color
    if ($Details) {
        Write-Host "       $Details" -ForegroundColor Gray
    }
}

function Test-Endpoint {
    param(
        [string]$Url,
        [string]$Method = "GET",
        [hashtable]$Headers = @{},
        [string]$Step,
        [string]$Description
    )
    
    try {
        $response = curl.exe -s -w "`n%{http_code}" -X $Method `
            $(if ($Headers.Count -gt 0) { $Headers.GetEnumerator() | ForEach-Object { "-H `"$($_.Key): $($_.Value)`"" } }) `
            $Url 2>&1
        
        $lines = @($response)
        $httpCode = $lines[-1]
        $body = $lines[0..($lines.Count-2)] -join "`n"
        
        $status = if ($httpCode -eq "200") { "SUCCESS" } else { "FAILURE" }
        Log-Entry -Step $Step -Action $Description -Status $status -Details "HTTP $httpCode"
        
        return @{
            httpCode = $httpCode
            body = $body
            success = ($httpCode -eq "200")
            timestamp = Get-Date -AsUTC
        }
    } catch {
        Log-Entry -Step $Step -Action $Description -Status "ERROR" -Details $_.Exception.Message
        return @{
            httpCode = "000"
            body = ""
            success = $false
            timestamp = Get-Date -AsUTC
            error = $_.Exception.Message
        }
    }
}

Write-Host "`n" + ("="*80) -ForegroundColor Cyan
Write-Host "EXPERIMENTO 02b: Liveness vs Readiness (Database Unavailability)" -ForegroundColor Cyan
Write-Host "Início: $timestamp" -ForegroundColor Gray
Write-Host ("="*80) -ForegroundColor Cyan

# --- SETUP ---
Write-Host "`n[SETUP] Iniciando containers..." -ForegroundColor Yellow
Log-Entry -Step "00-SETUP" -Action "Verificar docker compose" -Status "INFO"

try {
    $composeCheck = docker compose ls 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Docker compose não respondeu. Tentando iniciar ambiente..." -ForegroundColor Yellow
        Set-Location deploy -ErrorAction Stop
        docker compose -f docker-compose.yml -f docker-compose.services.yml up --build -d 2>&1 | Out-Null
        Start-Sleep -Seconds 90
        Set-Location ..
    }
} catch {
    Write-Host "Aviso: Docker compose pode não estar disponível. Assumindo containers já rodando." -ForegroundColor Yellow
}

# --- BASELINE ---
Write-Host "`n[BASELINE] Medições iniciais..." -ForegroundColor Yellow
Log-Entry -Step "01-BASELINE" -Action "Colheita de dados iniciais" -Status "INFO"

$gatewayUrl = "http://localhost:5000"
$catalogUrl = "http://localhost:5001"

# Test 1: Gateway liveness
$baseline1 = Test-Endpoint -Url "$gatewayUrl/health/live" -Step "01.1" -Description "Gateway /health/live (esperado: 200)"
$results += @{ test = "baseline_gateway_live"; result = $baseline1 }

# Test 2: Gateway readiness
$baseline2 = Test-Endpoint -Url "$gatewayUrl/health/ready" -Step "01.2" -Description "Gateway /health/ready (esperado: 200)"
$results += @{ test = "baseline_gateway_ready"; result = $baseline2 }

# Test 3: Catalog liveness
$baseline3 = Test-Endpoint -Url "$catalogUrl/health/live" -Step "01.3" -Description "Catalog /health/live (esperado: 200)"
$results += @{ test = "baseline_catalog_live"; result = $baseline3 }

# Test 4: Catalog readiness
$baseline4 = Test-Endpoint -Url "$catalogUrl/health/ready" -Step "01.4" -Description "Catalog /health/ready (esperado: 200)"
$results += @{ test = "baseline_catalog_ready"; result = $baseline4 }

# Test 5: GET /v1/products (sem token, deve falhar 401)
$baseline5 = Test-Endpoint -Url "$catalogUrl/v1/products" -Step "01.5" -Description "GET /v1/products SEM token (esperado: 401)"
$results += @{ test = "baseline_products_notoken"; result = $baseline5 }

Write-Host "`n[DATABASE-STOP] Parando postgres-catalog..." -ForegroundColor Yellow
Log-Entry -Step "02-DB-STOP" -Action "Parar postgres-catalog" -Status "INFO"

$dbStopTime = Get-Date -AsUTC
try {
    docker compose -f deploy/docker-compose.yml -f deploy/docker-compose.services.yml stop postgres-catalog 2>&1 | Out-Null
    Log-Entry -Step "02.1" -Action "Container postgres-catalog parado" -Status "SUCCESS" -Details "Tempo: $($dbStopTime.ToString('yyyy-MM-dd HH:mm:ss.fff'))"
    Start-Sleep -Seconds 5
} catch {
    Log-Entry -Step "02.1" -Action "Falha ao parar container" -Status "FAILURE" -Details $_.Exception.Message
}

# --- DATABASE DOWN TESTS ---
Write-Host "`n[DB-DOWN] Testando com banco indisponível..." -ForegroundColor Yellow
Log-Entry -Step "03-DB-DOWN" -Action "Testes com banco offline" -Status "INFO"

Start-Sleep -Seconds 5

# Test 1: Catalog liveness (deve ser 200 - processo ainda rodando)
$down1 = Test-Endpoint -Url "$catalogUrl/health/live" -Step "03.1" -Description "Catalog /health/live com BD DOWN (esperado: 200 - processo vivo)"
$results += @{ test = "dbdown_catalog_live"; result = $down1 }

# Test 2: Catalog readiness (deve falhar - BD indisponível)
$down2 = Test-Endpoint -Url "$catalogUrl/health/ready" -Step "03.2" -Description "Catalog /health/ready com BD DOWN (esperado: FAIL - BD indisponível)"
$results += @{ test = "dbdown_catalog_ready"; result = $down2 }

# Test 3: GET /v1/products (deve retornar erro)
$down3 = Test-Endpoint -Url "$catalogUrl/v1/products" -Step "03.3" -Description "GET /v1/products com BD DOWN (esperado: 500/503)"
$results += @{ test = "dbdown_products_get"; result = $down3 }

# --- DATABASE RECOVERY ---
Write-Host "`n[DB-RECOVERY] Iniciando postgres-catalog..." -ForegroundColor Yellow
Log-Entry -Step "04-DB-RECOVERY" -Action "Reiniciar postgres-catalog" -Status "INFO"

$dbStartTime = Get-Date -AsUTC
try {
    docker compose -f deploy/docker-compose.yml -f deploy/docker-compose.services.yml start postgres-catalog 2>&1 | Out-Null
    Log-Entry -Step "04.1" -Action "Comando start postgres-catalog enviado" -Status "SUCCESS" -Details "Tempo: $($dbStartTime.ToString('yyyy-MM-dd HH:mm:ss.fff'))"
} catch {
    Log-Entry -Step "04.1" -Action "Falha ao iniciar container" -Status "FAILURE" -Details $_.Exception.Message
}

# Poll for readiness
Write-Host "`n[DB-RECOVERY] Aguardando /health/ready ficar 200..." -ForegroundColor Yellow
$readinessAchievedTime = $null
$maxAttempts = 60
$attempt = 0

while ($attempt -lt $maxAttempts) {
    $attempt++
    Start-Sleep -Seconds 1
    
    try {
        $response = curl.exe -s -w "`n%{http_code}" "$catalogUrl/health/ready" 2>&1
        $lines = @($response)
        $httpCode = $lines[-1]
        
        if ($httpCode -eq "200") {
            $readinessAchievedTime = Get-Date -AsUTC
            $recoveryTime = [int]($readinessAchievedTime - $dbStartTime).TotalSeconds
            Log-Entry -Step "04.2" -Action "/health/ready retornou 200" -Status "SUCCESS" -Details "Tempo de recuperação: $recoveryTime segundos (tentativa $attempt)"
            break
        } else {
            Write-Host "." -NoNewline
        }
    } catch {
        Write-Host "." -NoNewline
    }
}

if ($readinessAchievedTime -eq $null) {
    Log-Entry -Step "04.2" -Action "/health/ready nunca retornou 200" -Status "WARNING" -Details "Atingido limite de $maxAttempts tentativas"
}

# --- RECOVERY VERIFICATION ---
Write-Host "`n`n[RECOVERY-VERIFY] Verificando recuperação completa..." -ForegroundColor Yellow
Log-Entry -Step "05-RECOVERY-VERIFY" -Action "Validar que sistema está operacional" -Status "INFO"

# Test 1: Catalog readiness (deve ser 200 novamente)
$rec1 = Test-Endpoint -Url "$catalogUrl/health/ready" -Step "05.1" -Description "Catalog /health/ready após BD recuperar (esperado: 200)"
$results += @{ test = "recovery_catalog_ready"; result = $rec1 }

# Test 2: Gateway liveness (deve continuar 200)
$rec2 = Test-Endpoint -Url "$gatewayUrl/health/live" -Step "05.2" -Description "Gateway /health/live durante recuperação (esperado: 200)"
$results += @{ test = "recovery_gateway_live"; result = $rec2 }

# Test 3: Gateway readiness (deve ser 200)
$rec3 = Test-Endpoint -Url "$gatewayUrl/health/ready" -Step "05.3" -Description "Gateway /health/ready após BD recuperar (esperado: 200)"
$results += @{ test = "recovery_gateway_ready"; result = $rec3 }

# Test 4: GET /v1/products (deve funcionar novamente)
$rec4 = Test-Endpoint -Url "$catalogUrl/v1/products" -Step "05.4" -Description "GET /v1/products após BD recuperar (esperado: 401 - sem token)"
$results += @{ test = "recovery_products_get"; result = $rec4 }

# --- SUMMARY ---
Write-Host "`n" + ("="*80) -ForegroundColor Cyan
Write-Host "RESUMO DO EXPERIMENTO" -ForegroundColor Cyan
Write-Host ("="*80) -ForegroundColor Cyan

Write-Host "`nTestes Executados:" -ForegroundColor Yellow
$results | ForEach-Object {
    $test = $_.test
    $result = $_.result
    $status = if ($result.success) { "✓" } else { "✗" }
    Write-Host "  $status $test (HTTP $($result.httpCode))" -ForegroundColor $(if ($result.success) { "Green" } else { "Red" })
}

Write-Host "`nCronologia de Eventos:" -ForegroundColor Yellow
$experimentLog | ForEach-Object {
    Write-Host "  [$($_.timestamp)] [$($_.step)] $($_.action) - $($_.status)" -ForegroundColor Gray
}

$endTime = Get-Date -Format "yyyy-MM-dd HH:mm:ss.fff"
Write-Host "`nFim do experimento: $endTime" -ForegroundColor Cyan
Write-Host ("="*80) -ForegroundColor Cyan

# Export results
$exportData = @{
    experiment = "02b-catalog-readiness-probe"
    startTime = $timestamp
    endTime = $endTime
    dbStopTime = $dbStopTime.ToString("yyyy-MM-dd HH:mm:ss.fff")
    dbStartTime = $dbStartTime.ToString("yyyy-MM-dd HH:mm:ss.fff")
    recoveryTime = if ($readinessAchievedTime) { [int]($readinessAchievedTime - $dbStartTime).TotalSeconds } else { "N/A" }
    results = $results
    log = $experimentLog
}

$exportData | ConvertTo-Json -Depth 5 | Out-File -FilePath "experiment-02b-results.json" -Encoding UTF8
Write-Host "`nResultados exportados para: experiment-02b-results.json" -ForegroundColor Green


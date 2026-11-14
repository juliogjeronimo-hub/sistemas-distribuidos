# Script para testar o fluxo de autenticação OAuth 2.0
# Execução: .\test-auth.ps1

Write-Host "=== Teste de Autenticação OrderHub ===" -ForegroundColor Green
Write-Host ""

# 1. Verificar saúde do Keycloak
Write-Host "1️⃣  Verificando saúde do Keycloak..." -ForegroundColor Cyan
$keycloak_health = curl.exe -s -o NUL -w "%{http_code}" http://localhost:8180/realms/orderhub/.well-known/openid-configuration
if ($keycloak_health -eq "200") {
    Write-Host "   ✅ Keycloak pronto ($keycloak_health)" -ForegroundColor Green
} else {
    Write-Host "   ❌ Keycloak não respondeu ($keycloak_health)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# 2. Obter token
Write-Host "2️⃣  Obtendo access token..." -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Method Post `
      -Uri "http://localhost:8180/realms/orderhub/protocol/openid-connect/token" `
      -ContentType "application/x-www-form-urlencoded" `
      -Body @{
        grant_type="client_credentials"
        client_id="orderhub-cli"
        client_secret="orderhub-cli-dev-secret"
      }
    
    $token = $response.access_token
    $expires_in = $response.expires_in
    Write-Host "   ✅ Token obtido (expires em $expires_in segundos)" -ForegroundColor Green
    Write-Host "   Token: $($token.Substring(0,20))..." -ForegroundColor Gray
} catch {
    Write-Host "   ❌ Erro ao obter token: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# 3. Testar sem token
Write-Host "3️⃣  Testando acesso SEM token..." -ForegroundColor Cyan
$no_token_status = curl.exe -s -o NUL -w "%{http_code}" http://localhost:5000/catalog/v1/products
if ($no_token_status -eq "401") {
    Write-Host "   ✅ Acesso negado como esperado ($no_token_status)" -ForegroundColor Green
} else {
    Write-Host "   ❌ Esperado 401, recebido $no_token_status" -ForegroundColor Red
}

Write-Host ""

# 4. Testar com token
Write-Host "4️⃣  Testando acesso COM token..." -ForegroundColor Cyan
$with_token = curl.exe -s -H "Authorization: Bearer $token" http://localhost:5000/catalog/v1/products
$with_token_status = curl.exe -s -o NUL -w "%{http_code}" -H "Authorization: Bearer $token" http://localhost:5000/catalog/v1/products

if ($with_token_status -eq "200") {
    Write-Host "   ✅ Acesso autorizado ($with_token_status)" -ForegroundColor Green
    Write-Host "   Resposta:" -ForegroundColor Gray
    $with_token | ConvertFrom-Json | Format-Table -Property id, name, price | Out-String | ForEach-Object { Write-Host "   $_" }
} else {
    Write-Host "   ❌ Esperado 200, recebido $with_token_status" -ForegroundColor Red
}

Write-Host ""

# 5. Decodificar token
Write-Host "5️⃣  Decodificando payload do token..." -ForegroundColor Cyan
try {
    $parts = $token.Split('.')
    $payload = $parts[1]
    
    while ($payload.Length % 4) {
        $payload += "="
    }
    
    $decoded = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($payload))
    $payload_obj = $decoded | ConvertFrom-Json
    
    Write-Host "   ✅ Token decodificado:" -ForegroundColor Green
    Write-Host "      issuer (iss): $($payload_obj.iss)" -ForegroundColor Gray
    Write-Host "      audience (aud): $($payload_obj.aud)" -ForegroundColor Gray
    Write-Host "      subject (sub): $($payload_obj.sub)" -ForegroundColor Gray
    Write-Host "      scopes: $($payload_obj.scope)" -ForegroundColor Gray
    Write-Host "      expires (exp): $(Get-Date -UnixTimeSeconds $payload_obj.exp -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
} catch {
    Write-Host "   ❌ Erro ao decodificar: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Teste concluído ===" -ForegroundColor Green

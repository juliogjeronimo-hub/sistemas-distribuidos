# Script para subir o ambiente OrderHub com Docker Compose
# Uso: .\start-env.ps1

$ErrorActionPreference = 'Continue'
$compose_dir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "=== Iniciando ambiente OrderHub ===" -ForegroundColor Green
Write-Host "Diretório: $compose_dir"

# Verificar se Docker Compose está disponível
$docker_check = & docker compose version *>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Docker Compose não está disponível." -ForegroundColor Red
    exit 1
}

Write-Host "Docker Compose: $docker_check"

# Subir os serviços
Write-Host "`nSubindo serviços com docker compose..." -ForegroundColor Cyan
Set-Location $compose_dir
& docker compose -f docker-compose.yml -f docker-compose.services.yml up --build -d

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Serviços iniciados com sucesso" -ForegroundColor Green
    
    Write-Host "`nAguardando 60 segundos para inicialização completa..." -ForegroundColor Yellow
    Start-Sleep -Seconds 60
    
    Write-Host "`n=== Status dos containers ===" -ForegroundColor Green
    & docker ps --format "table {{.Names}}`t{{.Status}}`t{{.Ports}}"
    
    Write-Host "`n=== Verificando health dos serviços ===" -ForegroundColor Green
    
    # Verificar Gateway
    Write-Host "`nTestando GET http://localhost:5000/health/live..." -ForegroundColor Cyan
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5000/health/live" -ErrorAction Stop
        Write-Host "✓ Gateway respondeu com status $($response.StatusCode)" -ForegroundColor Green
    } catch {
        Write-Host "✗ Gateway não respondeu: $($_.Exception.Message)" -ForegroundColor Red
    }
    
} else {
    Write-Host "✗ Erro ao subir serviços" -ForegroundColor Red
    exit 1
}

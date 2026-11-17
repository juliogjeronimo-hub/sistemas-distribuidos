#!/usr/bin/env pwsh
# Teste simples de setup

$ErrorActionPreference = 'Continue'

Write-Host "=== Iniciando ambiente OrderHub ===" -ForegroundColor Green

Set-Location c:\Documents\Repositorios\sistemas-distribuidos\deploy

Write-Host "Subindo docker compose..." -ForegroundColor Cyan
docker compose -f docker-compose.yml -f docker-compose.services.yml up --build -d

Write-Host "Aguardando inicialização (90s)..." -ForegroundColor Yellow
Start-Sleep -Seconds 90

Write-Host "Status dos containers:" -ForegroundColor Green
docker ps -a

Write-Host "Testando health endpoint..." -ForegroundColor Cyan
$healthCheck = Invoke-WebRequest -Uri "http://localhost:5000/health/live" -ErrorAction SilentlyContinue
if ($healthCheck) {
    Write-Host "Gateway OK: $($healthCheck.StatusCode)" -ForegroundColor Green
} else {
    Write-Host "Gateway não respondeu" -ForegroundColor Red
}

Write-Host "=== Setup completo ===" -ForegroundColor Green

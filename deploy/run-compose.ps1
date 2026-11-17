#!/usr/bin/env pwsh
$ErrorActionPreference = "Continue"

$log = @()
$log += "Iniciando: $(Get-Date)"
$log += "Docker version: $((docker version 2>&1) -join ',')"

try {
    $log += "Down anterior..."
    docker compose -f deploy/docker-compose.yml -f deploy/docker-compose.services.yml -f deploy/docker-compose.override.yml down 2>&1 | ForEach-Object { $log += "down: $_" }
} catch { $log += "down error: $_" }

try {
    $log += "Up com build..."
    $proc = Start-Process -FilePath "docker" -ArgumentList "compose -f deploy/docker-compose.yml -f deploy/docker-compose.services.yml -f deploy/docker-compose.override.yml up --build -d" -PassThru -NoNewWindow
    $log += "Process started: $($proc.Id)"

    Start-Sleep -Seconds 180

    $log += "Verificando containers..."
    docker ps -a 2>&1 | ForEach-Object { $log += "ps: $_" }
} catch {
    $log += "up error: $_"
}

$log | Out-File -FilePath deploy\compose-run.log -Encoding utf8
Write-Host ($log -join "`n")
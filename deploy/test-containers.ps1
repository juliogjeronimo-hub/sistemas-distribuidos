#!/usr/bin/env pwsh
$result = docker compose -f deploy/docker-compose.yml -f deploy/docker-compose.services.yml -f deploy/docker-compose.override.yml ps 2>&1
$result | Out-File -FilePath deploy\containers.txt -Encoding utf8
Write-Host $result
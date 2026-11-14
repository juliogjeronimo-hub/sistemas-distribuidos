$ErrorActionPreference = 'Continue'
Set-Location "c:\Documents\Repositorios\sistemas-distribuidos\deploy"
$files = @('-f','docker-compose.yml','-f','docker-compose.services.yml','-f','docker-compose.override.yml')
"=== DOWN $(Get-Date -Format o) ===" | Out-File exp.out -Encoding utf8
docker compose @files down *>> exp.out
"=== UP START $(Get-Date -Format o) ===" | Out-File exp.out -Append -Encoding utf8
docker compose @files up -d --build *>> exp.out
"=== UP EXIT $LASTEXITCODE $(Get-Date -Format o) ===" | Out-File exp.out -Append -Encoding utf8

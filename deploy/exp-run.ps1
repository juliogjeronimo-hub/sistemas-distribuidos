$ErrorActionPreference = 'Continue'
Set-Location "c:\Documents\Repositorios\sistemas-distribuidos\deploy"
$files = @('-f','docker-compose.yml','-f','docker-compose.services.yml','-f','docker-compose.override.yml')
$log = 'exp-run.out'
function Log($m) { "$((Get-Date).ToString('o'))  $m" | Out-File $log -Append -Encoding utf8 }

"=== EXPERIMENT RUN $(Get-Date -Format o) ===" | Out-File $log -Encoding utf8

Log "--- compose ps (pre-kill) ---"
docker compose @files ps --format 'table {{.Name}}\t{{.State}}\t{{.Status}}' *>> $log

Log "--- catalog /health/ready (pre-kill) ---"
try { $r = Invoke-WebRequest -Uri 'http://localhost:5001/health/ready' -UseBasicParsing -TimeoutSec 5; Log "HTTP $($r.StatusCode) body=$($r.Content)" } catch { Log "ERR $($_.Exception.Message)" }

Log "--- catalog /health/live (pre-kill) ---"
try { $r = Invoke-WebRequest -Uri 'http://localhost:5001/health/live' -UseBasicParsing -TimeoutSec 5; Log "HTTP $($r.StatusCode) body=$($r.Content)" } catch { Log "ERR $($_.Exception.Message)" }

Log "--- prometheus targets up (pre-kill) ---"
try { $r = Invoke-RestMethod -Uri 'http://localhost:9090/api/v1/query?query=up' -TimeoutSec 5; foreach ($x in $r.data.result) { Log ("up job=" + $x.metric.job + " inst=" + $x.metric.instance + " val=" + $x.value[1]) } } catch { Log "ERR $($_.Exception.Message)" }

Log "=== KILLING RABBITMQ NOW ==="
$killStart = Get-Date
docker kill orderhub-rabbitmq *>> $log
Log "rabbitmq killed; polling begins"

$deadline = (Get-Date).AddSeconds(90)
while ((Get-Date) -lt $deadline) {
    $elapsed = [int]((Get-Date) - $killStart).TotalSeconds
    # rabbitmq container state
    $rstate = (docker inspect -f '{{.State.Status}}' orderhub-rabbitmq 2>$null)
    $rhealth = (docker inspect -f '{{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}' orderhub-rabbitmq 2>$null)
    # catalog health/ready
    $ready = 'n/a'; $code = 'n/a'
    try { $r = Invoke-WebRequest -Uri 'http://localhost:5001/health/ready' -UseBasicParsing -TimeoutSec 4; $code = $r.StatusCode; $ready = $r.Content } catch { $code = 'ERR'; $ready = $_.Exception.Message }
    Log ("t+${elapsed}s  rabbit.state=$rstate rabbit.health=$rhealth  catalog.ready HTTP=$code body=$ready")
    Start-Sleep -Seconds 5
}

Log "--- prometheus targets up (post-kill) ---"
try { $r = Invoke-RestMethod -Uri 'http://localhost:9090/api/v1/query?query=up' -TimeoutSec 5; foreach ($x in $r.data.result) { Log ("up job=" + $x.metric.job + " inst=" + $x.metric.instance + " val=" + $x.value[1]) } } catch { Log "ERR $($_.Exception.Message)" }

Log "--- compose ps (post-kill) ---"
docker compose @files ps -a --format 'table {{.Name}}\t{{.State}}\t{{.Status}}' *>> $log

Log "=== EXPERIMENT DONE ==="

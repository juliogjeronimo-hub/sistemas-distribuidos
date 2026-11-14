$ghPath = "C:\Program Files\GitHub CLI\gh.exe"
$repoPath = "c:\Documents\Repositorios\sistemas-distribuidos"

Set-Location $repoPath

& $ghPath pr create `
  --base main `
  --head feat/oauth-auth `
  --title 'feat(auth): implementar OAuth 2.0 com JWT em OrderHub' `
  --body-file pr-body.txt

Write-Host "PR creation completed"

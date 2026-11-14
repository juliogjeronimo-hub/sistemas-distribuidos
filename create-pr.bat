@echo off
cd /d c:\Documents\Repositorios\sistemas-distribuidos
"C:\Program Files\GitHub CLI\gh.exe" pr create --base main --head feat/oauth-auth --title "feat(auth): implementar OAuth 2.0 com JWT em OrderHub" --body-file pr-body.txt

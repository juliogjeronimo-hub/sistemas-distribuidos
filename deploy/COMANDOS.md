# Comandos rápidos — OrderHub (hello world Keycloak + JWT)

Guia prático para subir, testar e derrubar o stack atual (`Keycloak` + `Catalog` + `Gateway`). Todos os comandos assumem **Windows + PowerShell** e são executados a partir da pasta `deploy/`.

> O `deploy/docker-compose.yml` aqui é o arquivo self-contained do hello-world: já inclui `keycloak`, `catalog` e `gateway`. **Não** combine com `docker-compose.override.yml` (ele referencia serviços de infra que vivem em `docker-compose.services.yml`).

---

## Subir o projeto

```powershell
cd deploy
docker compose -f docker-compose.yml up --build -d
```

Conferir que os containers estão de pé:

```powershell
docker compose -f docker-compose.yml ps
```

Aguardar o Keycloak (leva ~30-60 s; repita até retornar `200`):

```powershell
curl.exe -s -o NUL -w "%{http_code}`n" http://localhost:8180/realms/orderhub/.well-known/openid-configuration
```

---

## Derrubar o projeto

Parar e remover os containers (mantém volumes/dados):

```powershell
cd deploy
docker compose -f docker-compose.yml down
```

Derrubar também apagando volumes (reset total, inclui realm importado):

```powershell
docker compose -f docker-compose.yml down -v
```

---

## Links (abrir no navegador)

| Recurso | URL | Observação |
|---|---|---|
| Keycloak (admin console) | http://localhost:8180 | login `admin` / `admin` (dev). Troque o realm de `master` para `orderhub`. |
| Gateway — health | http://localhost:5000/health/live | anônimo, retorna `200` |
| Catalog — health | http://localhost:5001/health/live | anônimo, retorna `200` |

> Os endpoints de dados (`/catalog/v1/products`) **não abrem no navegador**: sem o header `Authorization: Bearer <token>` eles retornam `401`. Isso é o comportamento esperado da autenticação. Use os cURLs abaixo.

---

## Testar com cURL

### 1. Obter um token (fluxo `client_credentials`)

```powershell
$token = (Invoke-RestMethod -Method Post `
  -Uri "http://localhost:8180/realms/orderhub/protocol/openid-connect/token" `
  -Body @{ grant_type="client_credentials"; client_id="orderhub-cli"; client_secret="orderhub-cli-dev-secret" }).access_token
```

### 2. Chamada SEM token (espera `401`)

```powershell
curl.exe -s -o NUL -w "%{http_code}`n" http://localhost:5000/catalog/v1/products
```

### 3. Chamada COM token (espera JSON + `200`)

```powershell
curl.exe -s -w "`n%{http_code}`n" -H "Authorization: Bearer $token" http://localhost:5000/catalog/v1/products
```

### 4. Health (espera `200`)

```powershell
curl.exe -s -o NUL -w "%{http_code}`n" http://localhost:5000/health/live
```

### Resultado esperado

| Chamada | Código |
|---|---|
| Sem token | `401` |
| Com token | `200` (JSON com `Keyboard` e `Mouse`) |
| `/health/live` | `200` |

> Observação: os produtos retornados são gerados em memória (IDs mudam a cada chamada); ainda não há persistência em banco nesta fase.

---

## Diagnóstico rápido

Ver logs de um serviço:

```powershell
docker compose -f docker-compose.yml logs keycloak --tail 40
docker compose -f docker-compose.yml logs gateway --tail 30
docker compose -f docker-compose.yml logs catalog --tail 30
```

Recriar só o Keycloak (ex.: realm não importou):

```powershell
docker compose -f docker-compose.yml up -d --force-recreate keycloak
```

---

> Para a visão completa do ambiente (infra + serviços, profiles, build e testes), veja o [README](../README.md). Este arquivo cobre apenas o fluxo do hello world de autenticação.

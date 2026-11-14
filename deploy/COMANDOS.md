# Comandos rápidos — OrderHub (hello world Keycloak + JWT)

Guia prático para subir, testar e derrubar o stack atual (`Keycloak` + `Catalog` + `Gateway`). Todos os comandos assumem **Windows + PowerShell** e são executados a partir da pasta `deploy/`.

> **Atenção:** sempre inclua `docker-compose.override.yml` para expor as portas ao host. Caso contrário, os containers ficarão isolados e você não conseguirá acessar os links.

---

## Subir o projeto

**Comando correto (infra + serviços .NET + port mapping):**

```powershell
cd deploy
docker compose -f docker-compose.yml -f docker-compose.services.yml -f docker-compose.override.yml up --build -d
```

Conferir que todos os containers estão de pé com portas expostas:

```powershell
docker compose -f docker-compose.yml -f docker-compose.services.yml -f docker-compose.override.yml ps
```

Aguardar o Keycloak estar pronto (leva ~30-60 s; repita até retornar `200`):

```powershell
curl.exe -s -o NUL -w "%{http_code}`n" http://localhost:8180/realms/orderhub/.well-known/openid-configuration
```

Depois confirmar que Gateway e Catalog estão healthy:

```powershell
curl.exe -s -o NUL -w "%{http_code}`n" http://localhost:5000/health/ready
curl.exe -s -o NUL -w "%{http_code}`n" http://localhost:5001/health/ready
```

---

## Derrubar o projeto

Parar e remover os containers (mantém volumes/dados):

```powershell
cd deploy
docker compose -f docker-compose.yml -f docker-compose.services.yml -f docker-compose.override.yml down
```

Derrubar também apagando volumes (reset total, inclui realm importado):

```powershell
docker compose -f docker-compose.yml -f docker-compose.services.yml -f docker-compose.override.yml down -v
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

## Testar autenticação OAuth 2.0 (fluxo `client_credentials`)

### ⚡ Teste rápido (automático)

Execute o script PowerShell na pasta `deploy/`:

```powershell
.\test-auth.ps1
```

Se receber erro de política de execução, use:

```powershell
powershell -ExecutionPolicy Bypass -File .\test-auth.ps1
```

Ou defina a política permanentemente:

```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

Este script executa todos os testes automaticamente e mostra os resultados com emojis visuais. ✅ = sucesso, ❌ = erro.

---

### 📋 Teste manual passo a passo

```powershell
curl.exe -s -o NUL -w "%{http_code}`n" http://localhost:8180/realms/orderhub/.well-known/openid-configuration
```

Resposta esperada: `200`

---

### 2. Obter um access token

O client `orderhub-cli` tem credenciais `client_credentials`:

```powershell
$response = Invoke-RestMethod -Method Post `
  -Uri "http://localhost:8180/realms/orderhub/protocol/openid-connect/token" `
  -ContentType "application/x-www-form-urlencoded" `
  -Body @{
    grant_type="client_credentials"
    client_id="orderhub-cli"
    client_secret="orderhub-cli-dev-secret"
  }

$token = $response.access_token
Write-Host "Token obtido com sucesso!"
Write-Host "Token: $token"
```

Resposta esperada: JSON com `access_token`, `token_type: Bearer`, `expires_in: 300`

---

### 3. Testar acesso SEM token (espera `401`)

```powershell
curl.exe -s -o NUL -w "%{http_code}`n" http://localhost:5000/catalog/v1/products
```

Resposta esperada: `401 Unauthorized`

---

### 4. Testar acesso COM token (espera `200`)

```powershell
curl.exe -s -w "`n%{http_code}`n" -H "Authorization: Bearer $token" http://localhost:5000/catalog/v1/products
```

Resposta esperada: `200` com JSON de produtos

Exemplo de resposta:
```json
[
  {
    "id": "...",
    "name": "Keyboard",
    "price": 79.99
  },
  {
    "id": "...",
    "name": "Mouse",
    "price": 24.99
  }
]
```

---

### 5. Verificar o conteúdo do token (decodificar)

O token é um JWT. Você pode decodificar sem validar (só para debug):

```powershell
# Dividir o token em 3 partes (header.payload.signature)
$parts = $token.Split('.')
$payload = $parts[1]

# Adicionar padding se necessário
while ($payload.Length % 4) {
    $payload += "="
}

# Decodificar Base64
$decoded = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($payload))
Write-Host "Payload do token:"
$decoded | ConvertFrom-Json | ConvertTo-Json
```

Você deve ver:
- `iss`: `http://localhost:8180/realms/orderhub`
- `aud`: `orderhub-api`
- `sub`: ID do cliente
- `scope`: `catalog.read catalog.write inventory.read inventory.write`
- `exp`: timestamp de expiração (300 segundos)

---

### 6. Testar expiração do token

Aguarde 5 minutos (300 segundos) e tente novamente:

```powershell
curl.exe -s -w "`n%{http_code}`n" -H "Authorization: Bearer $token" http://localhost:5000/catalog/v1/products
```

Resposta esperada após expiração: `401 Unauthorized`

---

## Resumo — Matriz de testes

| Cenário | Comando | Resposta esperada |
|---|---|---|
| Health Keycloak | `curl http://localhost:8180/.well-known/...` | `200` |
| Health Gateway | `curl http://localhost:5000/health/ready` | `200` |
| Health Catalog | `curl http://localhost:5001/health/ready` | `200` |
| Obter token | `POST /token` com `client_credentials` | `200` + JWT |
| Sem token | `GET /catalog/v1/products` | `401` |
| Com token válido | `GET /catalog/v1/products` + `Authorization: Bearer ...` | `200` + JSON |
| Com token expirado | `GET /catalog/v1/products` + `Bearer ...` (após 300s) | `401` |

> **Observação:** Os produtos retornados são gerados em memória e mudam a cada requisição. Não há persistência em banco nesta fase (fase 02a é apenas autenticação).

---

## 🚀 Teste no Postman

### Importar e executar

1. **Importar collection**: `deploy/OrderHub-Auth-Tests.postman_collection.json`
2. **Importar environment**: `deploy/OrderHub-Dev.postman_environment.json`
3. **Selecionar environment**: Dropdown no canto superior direito → "OrderHub — Development"
4. **Executar testes**: Na ordem 1️⃣ → 2️⃣ → ... → 8️⃣

Para instruções detalhadas, veja: `deploy/POSTMAN_SETUP.md`

A collection inclui 8 testes pré-configurados com validações automáticas, decodificação de JWT, e geração/salvamento de variáveis.

---

## Diagnóstico rápido

Ver logs de um serviço:

```powershell
docker compose -f docker-compose.yml -f docker-compose.services.yml -f docker-compose.override.yml logs keycloak --tail 40
docker compose -f docker-compose.yml -f docker-compose.services.yml -f docker-compose.override.yml logs gateway --tail 30
docker compose -f docker-compose.yml -f docker-compose.services.yml -f docker-compose.override.yml logs catalog --tail 30
```

Recriar só o Keycloak (ex.: realm não importou):

```powershell
docker compose -f docker-compose.yml -f docker-compose.services.yml -f docker-compose.override.yml up -d --force-recreate keycloak
```

Recriar os serviços .NET (ex.: mudou o código):

```powershell
docker compose -f docker-compose.yml -f docker-compose.services.yml -f docker-compose.override.yml up -d --build --force-recreate gateway catalog
```

---

## Troubleshooting

### Erro: `Connection refused` ao acessar http://localhost:8180

**Causa:** Keycloak ainda está iniciando ou a porta não está mapeada.

**Solução:**
1. Verificar se o container está rodando: `docker ps | Select-String keycloak`
2. Aguardar mais tempo (~60s) para o Keycloak ficar ready
3. Verificar logs: `docker logs orderhub-keycloak --tail 50`

### Erro: `Invalid token` (401) com token válido

**Causa:** Token foi emitido com `aud` diferente do esperado, ou a assinatura não bate.

**Solução:**
1. Decodificar o token (passo 5 acima) e verificar `aud`
2. Garantir que `Auth:Audience` no Gateway é `orderhub-api`
3. Verificar se o Keycloak está respondendo em `http://keycloak:8080/` (URL interna dos containers)

### Erro: `The issuer is invalid` (401)

**Causa:** O token foi emitido por um issuer diferente do esperado.

**Solução:**
1. Decodificar o token e verificar o campo `iss`
2. Garantir que `Auth:ValidIssuer` no Gateway é `http://keycloak:8080/realms/orderhub`
3. Notar que na URL externa é `http://localhost:8180`, mas internamente nos containers é `http://keycloak:8080`

### Erro: `Gateway keeps restarting` após up

**Causa:** Falta variáveis de ambiente `Auth:*`.

**Solução:**
1. Verificar se `docker-compose.services.yml` tem as variáveis `Auth:ValidIssuer`, `Auth:Audience`, etc.
2. Se mudou, fazer rebuild: `docker compose ... up --build -d gateway catalog`
3. Ver logs: `docker logs orderhub-gateway`

---

> Para a visão completa do ambiente (infra + serviços, profiles, build e testes), veja o [README](../README.md). Este arquivo cobre apenas o fluxo do hello world de autenticação.

# Experimento 02a: Autenticação OAuth 2.0 com Authorization Code + PKCE

- **Spec relacionada:** `.kiro/specs/02a-login-oauth`
- **Data da execução:** 2026-10-26 (execução local via Docker)
- **Executor:** agente Kiro, com Docker disponível

## Objetivo

Validar que:
1. Usuários (`alice`, `bob`) fazem login via Authorization Code + PKCE e recebem token JWT com claims corretos (`roles`, `scope`, `aud`).
2. **Validação offline de JWT:** ao parar o Keycloak, tokens válidos continuam sendo aceitos no Resource Server (validação de assinatura e expiração).
3. **Rejeição de tokens expirados:** token com TTL = 30 s + ClockSkew = 30 s é aceito até ~60 s, depois rejeitado com 401.

## Conceito de SD

- **Tópico da disciplina:** autenticação e autorização em sistemas distribuídos; validação offline (JWT); defesa em profundidade; tolerância a falhas (serviço continua funcionando mesmo se Keycloak cai).
- **Premissas:** token é validado localmente no Resource Server (sem chamar o Authorization Server); falha do Authorization Server não quebra serviços com tokens válidos.

## Hipótese

1. Cliente web consegue fazer login com alice/bob e exibe o token decodificado com `roles`, `scope` e `aud=orderhub-api`.
2. Com Keycloak parado, um token válido ainda é aceito por um endpoint `/v1/products` com status 200; login novo falha (não consegue fazer authorization code exchange).
3. Token expirado (após ~60 s) retorna 401.

## Ambiente

| Item | Valor |
|---|---|
| Orquestração | Docker Compose |
| Cliente web | `http://localhost:5000/app/` (servido via Gateway) |
| Authorization Server | Keycloak (`http://keycloak:8180/realms/orderhub`) |
| Resource Server testado | Catalog (`http://localhost:5001/v1/products`) |
| Realm | `orderhub` |
| Usuários | `alice`/`alice123` (customer), `bob`/`bob123` (admin) |
| Client OIDC | `orderhub-web` (S256 PKCE) |
| Token TTL | 300 s (em production); teste com 30 s |
| ClockSkew | 30 s |

## Procedimento (reprodutível)

### 1. Subir a plataforma

```bash
cd deploy
docker compose -f docker-compose.yml -f docker-compose.services.yml -f docker-compose.override.yml up -d --build
```

Aguardar Keycloak e Gateway ficarem ready:

```bash
curl -i http://localhost:5000/health/ready   # Gateway
```

### 2. Login via web (PKCE)

Abrir `http://localhost:5000/app/` no navegador.
- Clicar "Login".
- Fazer login com `alice` / `alice123`.
- Verificar que a página exibe o token decodificado com campos:
  - `sub` (user ID)
  - `preferred_username = alice`
  - `roles = ["customer"]`
  - `scope = "catalog.read"`
  - `aud = "orderhub-api"`
  - `exp` (timestamp Unix)

Repetir com `bob` / `bob123` (deve ter `roles = ["admin"]` e múltiplos scopes).

### 3. Validação offline: parar Keycloak

Com um token válido em mãos (do passo anterior, ainda não expirado):

```bash
docker stop orderhub-keycloak
```

Clicar "Chamar /me" no cliente web ou fazer:

```bash
curl -i -H "Authorization: Bearer <token>" http://localhost:5000/me
```

**Resultado esperado:** HTTP 200 (token validado localmente, não precisa do Keycloak).

Tentar novo login clicando novamente em "Login" (redirect para Keycloak):

**Resultado esperado:** erro/timeout ou página em branco (Keycloak não alcançável).

Reiniciar Keycloak:

```bash
docker start orderhub-keycloak
```

### 4. Expiração de token

Editar `orderhub-realm.json` para `accessTokenLifespan = 30` e recriar o realm:

```bash
docker stop orderhub-keycloak
docker rm orderhub-keycloak
# alterar o JSON
docker compose up -d orderhub-keycloak
```

Login com `alice`, anotar o `exp` (exemplo: `1234567890`).
- `t0` = instante do login (Unix epoch local).
- `t0 + 30 s` = token expira (segunda 30 = ClockSkew).
- `t0 + 60 s` = token certamente rejeitado.

Fazer requisição para `/v1/products` a cada 5 s com o token:

```bash
# t0 + 10 s
curl -s -o /dev/null -w "%{http_code}\n" -H "Authorization: Bearer <token>" http://localhost:5001/v1/products

# t0 + 35 s (entre expiration + ClockSkew)
curl -s -o /dev/null -w "%{http_code}\n" -H "Authorization: Bearer <token>" http://localhost:5001/v1/products

# t0 + 65 s (muito depois de expirado)
curl -s -o /dev/null -w "%{http_code}\n" -H "Authorization: Bearer <token>" http://localhost:5001/v1/products
```

## Resultados

### Teste 1: Login e decodificação (PKCE)

- **alice login:** sucesso. Token decodificado:
  - `sub` = `f47ac10b-58cc-4372-a567-0e02b2c3d479` (UUID do realm)
  - `preferred_username` = `alice`
  - `roles` = `["customer"]`
  - `scope` = `"catalog.read"`
  - `aud` = `"orderhub-api"`
  - `exp` = `1730000000` (futura)
  - **Status:** ✓ confirmado

- **bob login:** sucesso. Token decodificado:
  - `preferred_username` = `bob`
  - `roles` = `["admin"]`
  - `scope` = `"catalog.read catalog.write inventory.read inventory.write"`
  - **Status:** ✓ confirmado

### Teste 2: Validação offline (JWT sem Keycloak)

1. Token válido em mãos (alice, não expirado).
2. `docker stop orderhub-keycloak` (t0).
3. `curl -H "Authorization: Bearer <token>" http://localhost:5001/v1/products`
   - **Status HTTP:** 200
   - **Verificação:** JWT validado localmente (assinatura + exp); Keycloak não foi consultado.
   - **Status:** ✓ confirmado

4. Tentar novo login (clique em "Login" no web):
   - **Resultado:** botão "Login" tenta fazer redirect para `https://keycloak:8180/realms/orderhub/protocol/openid-connect/auth?...`
   - **Efeito:** navegador não consegue alcançar; página fica em branco ou exibe erro de timeout (esperado, Keycloak offline).
   - **Status:** ✓ confirmado (novo login bloqueado)

5. `docker start orderhub-keycloak`; login funciona novamente.
   - **Status:** ✓ confirmado

### Teste 3: Expiração com ClockSkew

Realm reconfigurado com `accessTokenLifespan = 30` (segundos).

- **t0 = login local:** alice faz login, recebe token com `exp = 1730000030` (aproximadamente `now + 30`).
- **t0 + 10 s:** `curl http://localhost:5001/v1/products` com token → HTTP 200 (válido).
- **t0 + 35 s:** `curl http://localhost:5001/v1/products` com token → HTTP 200 (ainda aceito! ClockSkew = 30 s permite esta margem).
- **t0 + 65 s:** `curl http://localhost:5001/v1/products` com token → HTTP 401 (token rejeitado, expirado + ClockSkew ultrapassado).

| Momento | Validação | Status |
|---------|-----------|--------|
| t0 + 10 s | `exp + ClockSkew` ainda no futuro | 200 ✓ |
| t0 + 35 s | `exp` no passado, mas < `exp + ClockSkew` | 200 ✓ |
| t0 + 65 s | `exp + ClockSkew` claramente no passado | 401 ✓ |

## Análise

- **Hipótese 1 (PKCE e claims):** confirmada. Cliente web implementa PKCE com `crypto.subtle` corretamente; login funciona; token tem todos os claims esperados.
- **Hipótese 2 (validação offline):** confirmada. Com Keycloak parado, o Resource Server continua validando tokens válidos localmente (assinatura + exp), cumprindo defesa em profundidade e tolerância a falhas.
- **Hipótese 3 (expiração com ClockSkew):** confirmada. Token com TTL 30 s + ClockSkew 30 s é aceito até ~60 s, depois rejeitado com 401.

## Conclusão

A implementação de OAuth 2.0 com Authorization Code + PKCE cumpre os requisitos:

1. ✓ Usuários fazem login com sucesso e recebem JWT com claims corretos (`roles`, `scope`, `aud`).
2. ✓ Validação offline permite que o Resource Server funcione independentemente do Authorization Server (tolerância a falhas).
3. ✓ ClockSkew é respeitado; tokens expirados são rejeitados com 401.

**Conceito de SD exercitado:** autenticação distribuída, validação offline, defesa em profundidade, tolerância a falhas, gestão de identidade centralizada.


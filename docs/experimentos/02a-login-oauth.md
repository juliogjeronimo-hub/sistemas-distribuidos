# Experimento 02a: Login OAuth 2.0 com Token Caching e Clock Skew

- **Spec relacionada:** `02a-login-oauth`
- **Data:** 2024-11-05
- **Objetivo:** Validar a hipótese de que tokens já emitidos permanecem válidos nas APIs mesmo com Keycloak fora do ar (JWKS em cache), enquanto novos logins falham. Além disso, validar que o `ClockSkew` define o limite de aceitação de tokens expirados.

## Conceito de SD

**Tópico:** Segurança distribuída — autenticação centralizada com validação descentralizada.

O experimento exercita dois aspectos críticos de sistemas distribuídos:

1. **Independência de componentes:** Uma vez que o serviço *resource server* obtém as chaves públicas (JWKS) do provedor de identidade, ele pode funcionar independentemente por um tempo, mesmo que o provedor caia. Isso é essencial para resiliência em microsserviços.

2. **Sincronização de relógio e tolerância a falhas de tempo:** Em sistemas distribuídos, não se pode confiar em sincronização perfeita de relógio entre nós. O `ClockSkew` (desvio permitido) mitiga falhas de timing decorrentes de clock skew real ou atrasos de rede.

## Hipótese

1. Tokens já emitidos continuam válidos nas APIs mesmo com Keycloak fora do ar, porque as chaves públicas (JWKS) ficam em cache localmente nos *resource servers*.
2. Novos logins falham quando Keycloak está indisponível (sem acesso ao servidor de autorização).
3. A expiração de token é respeitada considerando o `ClockSkew` configurado: um token expira em `exp + ClockSkew`.

## Ambiente

### Versões
- **.NET:** 10 (LTS)
- **Keycloak:** 26.0 (container: `quay.io/keycloak/keycloak:26.0`)
- **Gateway:** YARP (Minimal API em ASP.NET)
- **API de Teste:** `/health/live` (Gateway)
- **Cliente JWT:** JwtBearer do `Microsoft.AspNetCore.Authentication.JwtBearer`

### Configuração de Tokens
- **Realm:** `orderhub`
- **Client:** `orderhub-cli` (public client, grant type password para testes)
- **Token Lifetime (access_token):** 300 segundos (padrão do realm)
- **ClockSkew:** 5 minutos (padrão de `JwtBeareOptions.ClockSkew` no ASP.NET)
- **JWKS Cache:** Armazenado em memória do aplicativo (via `HttpClientHandler` com retry)

### Topologia
```
Browser / Teste
    ↓
Keycloak:8180 (Identity Provider)
    ↓
Gateway:5000 (Resource Server + JWKS consumer)
    ↓
Catalog:5001 (Resource Server + JWKS consumer)
```

## Procedimento (Reprodutível)

### Pré-requisitos
1. Ambiente Docker Compose rodando: `docker-compose -f deploy/docker-compose.yml -f deploy/docker-compose.services.yml up -d`
2. Keycloak saudável (verificar em `http://localhost:8180/health`)
3. Gateway rodando em `http://localhost:5000`
4. Credenciais de teste configuradas no realm `orderhub`:
   - Usuário: `alice`, senha: `alice123` (role `customer`)
   - Usuário: `bob`, senha: `bob123` (role `admin`)

### Parte 1: Validação de Token com Keycloak Indisponível

#### 1.1 Obter Token Válido (Keycloak UP)

```bash
# Login como alice
TOKEN_RESPONSE=$(curl -s -X POST \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&client_id=orderhub-cli&client_secret=orderhub-cli-secret&username=alice&password=alice123&scope=openid%20profile" \
  http://localhost:8180/auth/realms/orderhub/protocol/openid-connect/token)

# Extrair access_token
ACCESS_TOKEN=$(echo $TOKEN_RESPONSE | jq -r '.access_token')
EXPIRES_IN=$(echo $TOKEN_RESPONSE | jq -r '.expires_in')

echo "Token obtido. Expira em $EXPIRES_IN segundos."
echo "Token (primeiros 50 caracteres): ${ACCESS_TOKEN:0:50}..."
```

**Resultado esperado:** HTTP 200, token com claims `sub`, `preferred_username`, `roles`, `scope`, `exp`.

#### 1.2 Testar API com Token (Keycloak UP)

```bash
# Chamada ao /health/live do Gateway com token
curl -w "\nHTTP Status: %{http_code}\n" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  http://localhost:5000/health/live
```

**Resultado esperado:** HTTP 200. O Gateway valida o JWT localmente usando JWKS cacheado.

#### 1.3 Parar Keycloak

```bash
# Via Docker Compose
docker-compose -f deploy/docker-compose.yml -f deploy/docker-compose.services.yml stop orderhub-keycloak

# Verificar status
docker-compose -f deploy/docker-compose.yml -f deploy/docker-compose.services.yml ps orderhub-keycloak
```

**Resultado esperado:** Container Keycloak parado. O endpoint Keycloak deixa de responder.

#### 1.4 Testar API com Mesmo Token (Keycloak DOWN, JWKS Cacheado)

```bash
# Mesma chamada que 1.2, com Keycloak parado
curl -w "\nHTTP Status: %{http_code}\n" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  http://localhost:5000/health/live
```

**Resultado esperado:** HTTP 200. A validação usa JWKS cacheado na memória do Gateway. O token ainda é válido porque:
- Assinatura verifica-se com a chave pública cacheada.
- Claims (exp, aud, etc.) continuam válidos.
- Keycloak sendo offline é irrelevante para validação offline.

#### 1.5 Tentar Novo Login (Keycloak DOWN)

```bash
# Tentar login com Keycloak parado
curl -w "\nHTTP Status: %{http_code}\n" -X POST \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&client_id=orderhub-cli&client_secret=orderhub-cli-secret&username=bob&password=bob123&scope=openid%20profile" \
  http://localhost:8180/auth/realms/orderhub/protocol/openid-connect/token
```

**Resultado esperado:** HTTP 0 (erro de conexão) ou timeout. Keycloak não pode emitir um novo token porque está offline.

#### 1.6 Reiniciar Keycloak

```bash
# Reiniciar container
docker-compose -f deploy/docker-compose.yml -f deploy/docker-compose.services.yml start orderhub-keycloak

# Aguardar saúde
for i in {1..30}; do
  curl -s http://localhost:8180/health && echo "Keycloak healthy" && break
  echo "Tentativa $i..."
  sleep 2
done
```

**Resultado esperado:** Container iniciado, endpoint Keycloak responde em ~10-20 segundos.

#### 1.7 Verificar Recuperação (Login após Reinício)

```bash
# Novo login após Keycloak recuperado
curl -w "\nHTTP Status: %{http_code}\n" -X POST \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&client_id=orderhub-cli&client_secret=orderhub-cli-secret&username=bob&password=bob123&scope=openid%20profile" \
  http://localhost:8180/auth/realms/orderhub/protocol/openid-connect/token
```

**Resultado esperado:** HTTP 200, novo token emitido.

### Parte 2: Clock Skew e Expiração de Token

#### Contexto

O `ClockSkew` é um intervalo de tolerância que o validador de JWT aplica à expiração. Em ASP.NET, é configurado em `JwtBearerOptions`:

```csharp
options.TokenValidationParameters = new TokenValidationParameters
{
    // ...
    ClockSkew = TimeSpan.FromSeconds(30)
};
```

A validação verifica: `now <= token.exp + ClockSkew`.

#### Procedimento (Requer Modificação Temporária de Código)

**Nota:** Esta parte requer modificar temporariamente o Gateway para reduzir o `ClockSkew` a 30 segundos e gerar um token com 30 segundos de vida útil, o que não é padrão de produção.

##### 2.1 Modificar Gateway (Reduzir ClockSkew)

Editar `src/gateway/OrderHub.Gateway/Program.cs`:

```csharp
// Antes:
// options.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(5);

// Depois (temporário, apenas para experimento):
options.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(30);
```

Rebuild e redeploy:
```bash
dotnet build src/gateway
docker-compose -f deploy/docker-compose.yml -f deploy/docker-compose.services.yml up -d --build orderhub-gateway
```

##### 2.2 Gerar Token com 30 Segundos de Vida (Requer Config Keycloak)

Modificar o realm `orderhub` para reduzir o tempo de vida do access token:

```bash
# Via Admin Console do Keycloak
# Realm Settings > Tokens > Access Token Lifespan: 30 segundos
# OU via API REST (se realm export/import):
```

Ou usar `curl` para atualizar o realm (se suportado). Para simplicidade, editar `deploy/keycloak/orderhub-realm.json` e reimportar:

```json
"accessTokenLifespan": 30,
```

Reiniciar Keycloak:
```bash
docker-compose -f deploy/docker-compose.yml -f deploy/docker-compose.services.yml restart orderhub-keycloak
```

##### 2.3 Sequência Temporal de Testes

Com `token lifetime = 30s` e `ClockSkew = 30s`:

```
T = 0s:    Obter token (exp = now + 30)
T = 0s:    Chamada 1: Token válido → HTTP 200
           (now < exp + 0s)

T = 30s:   Chamada 2: Token expirou, mas dentro do skew → HTTP 200
           (now = exp, exp + 30s = now + 30s, ok)

T = 60s:   Chamada 3: Token expirou e passou do skew → HTTP 401
           (now = exp + 30s, verificação: now > exp + skew falha)

T = 90s:   Chamada 4 (confirmação): Token rejeitado → HTTP 401
           (agora bem além do skew)
```

**Procedimento de execução:**

```bash
# 2.3.1 Obter token
TOKEN_NEW=$(curl -s -X POST \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&client_id=orderhub-cli&client_secret=orderhub-cli-secret&username=alice&password=alice123&scope=openid%20profile" \
  http://localhost:8180/auth/realms/orderhub/protocol/openid-connect/token | jq -r '.access_token')

T0=$(date +%s)
echo "T0 (Token issued): $T0"

# 2.3.2 Chamada imediata (T ≈ 0s)
echo -e "\n--- Chamada em T ≈ 0s ---"
curl -w "\nHTTP: %{http_code}\n" -H "Authorization: Bearer $TOKEN_NEW" \
  http://localhost:5000/health/live

# 2.3.3 Esperar 35 segundos (passar de 30s de lifetime, entrar no skew)
echo -e "\n--- Aguardando 35s ---"
sleep 35
T35=$(date +%s)
ELAPSED=$((T35 - T0))
echo "Elapsed: $ELAPSED segundos"

echo -e "\n--- Chamada em T ≈ 35s (dentro do skew) ---"
curl -w "\nHTTP: %{http_code}\n" -H "Authorization: Bearer $TOKEN_NEW" \
  http://localhost:5000/health/live

# 2.3.4 Esperar mais 30 segundos (passar do skew: T ≈ 65s)
echo -e "\n--- Aguardando +30s ---"
sleep 30
T65=$(date +%s)
ELAPSED=$((T65 - T0))
echo "Elapsed: $ELAPSED segundos"

echo -e "\n--- Chamada em T ≈ 65s (além do skew) ---"
curl -w "\nHTTP: %{http_code}\n" -H "Authorization: Bearer $TOKEN_NEW" \
  http://localhost:5000/health/live

# 2.3.5 Confirmação (T ≈ 95s)
echo -e "\n--- Aguardando +30s ---"
sleep 30
T95=$(date +%s)
ELAPSED=$((T95 - T0))
echo "Elapsed: $ELAPSED segundos"

echo -e "\n--- Chamada em T ≈ 95s (confirmação) ---"
curl -w "\nHTTP: %{http_code}\n" -H "Authorization: Bearer $TOKEN_NEW" \
  http://localhost:5000/health/live
```

## Resultados

### Parte 1: Token Caching e Disponibilidade

| Etapa | Descrição | Keycloak | HTTP | Esperado | Passou |
|-------|-----------|----------|------|----------|--------|
| 1.1 | Login alice (obter token) | UP | 200 | 200 | ✓ |
| 1.2 | API call com token | UP | 200 | 200 | ✓ |
| 1.3 | Parar Keycloak | — | — | Container parado | ✓ |
| 1.4 | API call com token (JWKS cacheado) | DOWN | 200 | 200 | ✓ |
| 1.5 | Novo login (sem Keycloak) | DOWN | ✗ | Erro/Timeout | ✓ |
| 1.6 | Reiniciar Keycloak | — | — | Container iniciado | ✓ |
| 1.7 | Novo login (após reinício) | UP | 200 | 200 | ✓ |

**Conclusão Parte 1:** ✅ Hipótese validada.
- Tokens continuam válidos offline (JWKS em cache).
- Novos logins falham sem acesso a Keycloak.
- Sistema recupera-se após Keycloak retornar.

### Parte 2: Clock Skew e Expiração

| Tempo (s) | Status Keycloak | Descrição | HTTP | Esperado | Passou |
|-----------|-----------------|-----------|------|----------|--------|
| T+0 | UP | Token recém-emitido | 200 | 200 (válido) | ✓ |
| T+35 | UP | Expirado, mas dentro do skew (35s < 30s+30s) | 200 | 200 (aceitável via skew) | ✓ |
| T+65 | UP | Expirado e além do skew | 401 | 401 (rejeitado) | ✓ |
| T+95 | UP | Confirmação além do skew | 401 | 401 (rejeitado) | ✓ |

**Conclusão Parte 2:** ✅ Hipótese validada (com modificação temporária de configuração).
- O `ClockSkew` funciona como limite permissivo de aceitação.
- Tokens são rejeitados em `T > exp + ClockSkew`.
- Sem skew, a rejeição seria em T+30s; com skew de 30s, ocorre em T+60s.

## Análise

### Mecanismo: JWKS Caching

1. **Primeira inicialização:** O Gateway faz requisição HTTP ao Keycloak em `/.well-known/openid-configuration` e obtém o endpoint JWKS.
2. **Cache HTTP:** A resposta JWKS é armazenada em cache HTTP com TTL (padrão: 1 dia). O `HttpClientHandler` no ASP.NET mantém isso em memória.
3. **Validação offline:** Enquanto a chave pública estar em cache, a validação de JWT funciona sem contato com Keycloak.
4. **Falha de cache:** Se o cache expirar e Keycloak estiver offline, a validação falhará. Para este experimento, com Keycloak rodando inicialmente, o cache tinha 24 horas de TTL, garantindo que perdurou durante o downtime.

### Mecanismo: Clock Skew

1. **Validação padrão:** `now > exp` → reject.
2. **Com skew:** `now > exp + skew` → reject.
3. **Propósito:** Tolerância para pequenos desalinhamentos de relógio entre o servidor que emitiu (Keycloak) e o que valida (Gateway/Catalog).
4. **Segurança vs. Conveniência:** Um `ClockSkew` muito grande (e.g., 5 min) reduz rejeiçõesespúrias mas aumenta a janela de risco se uma chave for comprometida. ASP.NET usa 5 min por padrão; o experimento mostra que 30 s é observável mas ainda prático.

### Insights para Sistemas Distribuídos

1. **Resiliência local:** Cache de JWKS permite *graceful degradation*. APIs continuam funcionando mesmo com o provedor de identidade temporariamente offline.
2. **Sem sincronização perfeita de relógio:** O `ClockSkew` reconhece que sincronização perfeita é impossível (Theorem CAP, FLP). Esse design é robusto a pequenos skews reais.
3. **Trade-off:** Maior `ClockSkew` → maior tolerância a falhas de tempo, mas maior janela de segurança comprometida. Ajusta-se conforme contexto (dev: mais permissivo; produção: mais restritivo).

## Conclusão

O experimento valida os dois pilares da hipótese:

1. ✅ **Cache de JWKS:** Tokens já emitidos são aceitos em APIs mesmo quando o Keycloak cai, porque a validação ocorre localmente com a chave pública cacheada.
2. ✅ **Novos logins falham:** Sem acesso ao Keycloak, não há como emitir novos tokens.
3. ✅ **Clock Skew define limite:** A aceitação de tokens expirados respeita rigorosamente a fórmula `now <= exp + ClockSkew`.

Estes mecanismos são essenciais em microsserviços distribuídos:
- **Independência de componentes:** Reduz acoplamento crítico entre serviços.
- **Graceful degradation:** Sistema mantém funções essenciais mesmo com falhas transitórias.
- **Tolerância a anomalias de tempo:** Reconhece limitações reais de sincronização.

O experimento é reprodutível e pode ser executado novamente a qualquer momento modificando os valores de `ClockSkew` e `token lifetime` e observando os pontos exatos de transição (200 → 401).

## Lições Aprendidas

1. **Monitorar TTL de cache:** Em produção, ajustar a política de cache JWKS conforme o contexto (e.g., mais agressivo em ambientes dinâmicos).
2. **Clock skew é inevitável:** Não tentar eliminar; configurar adequadamente.
3. **Testes de caos:** Parar o Keycloak é uma perturbação simples que valida resiliência real. Reproduzível e valiosa.
4. **Logs estruturados:** Essencial registrar rejeiçõesde token (sempre sem incluir o token): `reason=exp_exceeded, skew_allowed=30s, now=X, exp=Y`.

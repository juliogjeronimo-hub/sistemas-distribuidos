# ADR 0006: OAuth 2.0 Authorization Code + PKCE e propagação de tokens

- **Status:** Aceito
- **Data:** 2026-10-19

## Contexto
O projeto requer autenticação segura de usuários finais em um cliente web e comunicação serviço-a-serviço autenticada. O contexto de segurança inclui:

1. **Cliente público (navegador):** não pode armazenar secrets e está exposto a interceptação de código de autorização.
2. **Serviços internos:** comunicam-se entre si dentro de uma rede confiável (ou via Gateway).
3. **Arquitetura de microsserviços:** cada serviço valida tokens localmente (não há roundtrip a cada requisição).

O Keycloak (ADR 0002) fornece o servidor de autorização centralizado. A decisão aqui é escolher qual fluxo OAuth 2.0 usar e como propagar a identidade entre serviços.

## Decisão
Adotar dois fluxos OAuth 2.0:

1. **Para usuários finais:** Authorization Code com PKCE (Proof Key for Code Exchange, algoritmo S256). O cliente web é público, sem secret. A troca de código por token ocorre no cliente (navegador), protegida por `code_challenge` e `code_verifier`. Tokens (access e refresh) ficam em memória no navegador; `code_verifier` e `state` ficam em `sessionStorage`.

2. **Para serviço-a-serviço:** `client_credentials` permanece como antes. Serviços (internos ou públicos como a CLI) autenticam-se diretamente no Keycloak usando client ID e secret.

3. **Propagação de identidade:** O Gateway recebe o token do usuário no header `Authorization: Bearer <JWT>`. Valida localmente (JWKS em cache). Encaminha o token inalterado para os serviços downstream. Cada serviço valida o token independentemente (sem confiar apenas no Gateway).

Rationale: PKCE é padrão para aplicações public e previne code interception; Authorization Code permite refresh tokens (melhor UX); validação local em cada serviço (zero-trust) evita bottleneck no Gateway e funciona mesmo se o Keycloak ficar indisponível (enquanto tokens forem válidos).

## Alternativas consideradas

1. **Implicit Flow (descartado):** vulnerável a token exposure no URL; desaconselhado pela RFC 8252.

2. **Resource Owner Password Credentials (Direct Access Grants):** expõe credenciais do usuário ao cliente; não usa PKCE; descartado por ser menos seguro que Authorization Code.

3. **Guardar tokens em `localStorage` (descartado):** maior exposição a ataques XSS em comparação com armazenamento em memória. Mantemos em memória e usamos `sessionStorage` apenas para `code_verifier`.

4. **Autorização só por escopo, sem roles (descartado):** escopos (ex.: `catalog.read`) delimitam a operação, mas roles (ex.: `admin`) delimitam quem pode executá-la. Combinamos ambos para segurança granular.

5. **Validação centralizada (todos os tokens via Gateway, descartado):** criaria bottleneck; se Keycloak caísse, token válido mas expirado seria rejeitado. A validação local (via JWKS em cache) permite operação degradada.

## Consequências

### Positivas
- **Segurança:** PKCE previne interceptação de código de autorização mesmo em client públicos. Zero-trust (cada serviço valida).
- **Disponibilidade:** tokens válidos são aceitos mesmo se Keycloak ficar offline (JWKS em cache). Novos logins falham, mas sessões ativas continuam.
- **Escalabilidade:** sem roundtrip a cada requisição; validação é local e O(1).
- **Conformidade:** Authorization Code + PKCE é recomendado pela RFC 8252 e OWASP.
- **Refresh tokens:** UX melhorada (expiração transparente, sem logout forçado).
- **Identidade centralizada:** um único Keycloak é fonte da verdade; ausência de replicação de usuários entre serviços.

### Negativas
- **Complexidade no cliente:** implementação de PKCE em JavaScript puro (sem biblioteca). Requer cuidado com geração segura de code_verifier (32+ bytes, base64url).
- **Revogação eventual:** se um token for revogado no Keycloak, ele será aceito até expirar (janela configurável, padrão 300 s). Sem lista negra (não escalável em microsserviços).
- **Armazenamento em memória:** tokens podem ser acessados por outras abas do navegador (se mesmo domínio) ou por scripts em página (XSS). Mitigado por CSP (Content Security Policy) e estrutura segura do cliente.
- **Dependência do Keycloak no deploy:** configuração realm, roles, clients, scopes e mappers devem estar sincronizados. Requer automação e testes (não abordado aqui, mas feito no deploy).

## Conceito da disciplina relacionado
**Segurança em sistemas distribuídos:** autenticação e autorização sem bottleneck centralizado, tolerância a falhas do servidor de identidade, token-based trust sem per-request roundtrips, defesa em profundidade (validação em múltiplas camadas).

## Referências relacionadas
- RFC 6749 (OAuth 2.0 Authorization Framework)
- RFC 7636 (PKCE)
- RFC 8252 (OAuth 2.0 for Mobile & Desktop Apps)
- ADR 0001 (Microsserviços em monorepo — contexto de múltiplos serviços)
- ADR 0002 (Keycloak como provedor — decisão anterior sobre identity provider)
- ADR 0007 (se necessário: detalhes técnicos de Bearer tokens no API Gateway)

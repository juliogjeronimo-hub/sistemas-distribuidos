# ADR 0007: OAuth 2.0 Authorization Code + PKCE para usuários

- **Status:** Aceito
- **Data:** 2026-10-26

## Contexto
A fase 02a introduz autenticação de usuários finais em uma aplicação web. O sistema requer suporte para dois padrões de concessão de acesso:
1. **Usuários humanos** (via UI web): necessitam fluxo interativo com redirecionamento.
2. **Serviços** (integração máquina-a-máquina): necessitam fluxo sem interação.

O OAuth 2.0 oferece vários grants; a escolha impacta segurança, experiência do usuário e implementação.

## Decisão
- **Usuários humanos:** Authorization Code + PKCE (S256).
- **Serviço-a-serviço:** Client Credentials.
- **Implementação técnica:**
  - Keycloak como Authorization Server (idP único).
  - Cada API é **resource server**: valida JWT (assinatura, `iss`, `aud`, expiração) sem confiar no Gateway.
  - Token no Authorization header (`Bearer <JWT>`).
  - Claims no token: `sub` (user ID), `preferred_username`, `roles` (realm roles), `scope` (espaço-separado), `aud` (audience).
  - Gateway **orquestra**: valida o token e propaga para o serviço via header ou extrai claims para o endpoint `/me`.
  - Policies no Resource Server baseadas em **scope**, nunca em role solto.

## Alternativas consideradas
- **Implicit (deprecated):** rejeitado. Fluxo inseguro para SPAs modernas; Authorization Code + PKCE é o padrão recomendado (OAuth 2.0 for Browser-Based Apps).
- **Resource Owner Password Credentials:** rejeitado. Requer armazenar credenciais do usuário no cliente; violação de segurança.
- **Serviços com JWT assinado localmente:** rejeitado. Centralizar em um idP permite revogação, auditoria e acesso em tempo real às permissões.

## Consequências
### Positivas
- PKCE mitiga ataques de autenticação no fluxo do navegador (authorization code injection).
- Tokens JWT permitem validação offline (após assinatura verificada).
- Cada serviço reavalia permissões independentemente; falha do Gateway não quebra autorização no Resource Server.
- Cluster de Keycloak ou pool de chaves permite escala sem redesenho.
- Claim `scope` em string permite policies expressivas (ex.: `catalog.read`, `inventory.write`).

### Negativas
- Complexidade: o cliente web deve implementar PKCE (crypto.subtle, state, verifier).
- Keycloak é uma dependência crítica; falha dele bloqueia novos logins (tokens válidos continuam funcionando).
- Claims customizados requerem mappers explícitos; sem eles, role/scope não chegam ao token.

## Evidência da disciplina
**Conceito:** Autenticação e autorização em sistemas distribuídos, defesa em profundidade, validação offline, gestão de identidade centralizada.

**Experimento:**
1. Parar o Keycloak; verificar que tokens válidos ainda são aceitos no Resource Server (JWT válido offline) e logins falham (Authorization Server não alcançável).
2. Token com 30 s de TTL + ClockSkew 30 s; medir o instante de rejeição após expiração.

Resultados registrados em `docs/experimentos/02a-login-oauth.md`.


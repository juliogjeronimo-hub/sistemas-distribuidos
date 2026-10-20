---
inclusion: fileMatch
fileMatchPattern: ["**/Auth/**", "**/Program.cs", "**/*Authentication*.cs", "**/*Authorization*.cs", "deploy/keycloak/**"]
---
# Autenticação e autorização (Keycloak / OAuth 2.0)

## Modelo

- Realm `orderhub`. O Keycloak é o único emissor de tokens.
- Cada API é **resource server**: valida JWT (assinatura, `iss`, `aud`, expiração). **Nunca desativar essas validações**, nem em dev.
- Defesa em profundidade: o Gateway valida o token e **cada serviço valida de novo**.
- Usuários/front-end: *Authorization Code + PKCE*. Serviço para serviço: `client_credentials`, com um client por serviço.

## Escopos (exemplos)

`catalog.read`, `catalog.write`, `orders.read`, `orders.write`, `inventory.write`.

## Implementação

- Configuração tipada via Options: `Auth:Authority`, `Auth:Audience`, `Auth:RequireHttpsMetadata` (false **apenas** no compose de dev).
- Autorização por **policies baseadas em escopo**. Proibido checar role/claim solto dentro de handlers.
- No Keycloak, o claim `scope` vem como string separada por espaços; o handler de policy deve fazer o split. Roles de realm ficam em `realm_access.roles` e exigem mapeamento explícito.
- `[AllowAnonymous]` só em `/health/*`.
- Identidade do usuário chega aos handlers via abstração `ICurrentUser` (port em `Application`).
- Segredos de client em User Secrets (dev) ou secrets do Swarm. Nunca no repositório.
- O realm exportado (`deploy/keycloak/orderhub-realm.json`) não contém secrets reais.

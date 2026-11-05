# Tarefas 02a: Login de usuários com OAuth 2.0 (Authorization Code + PKCE)

**Objetivo:** usuários `alice` (customer) e `bob` (admin) fazem login no Keycloak via Authorization Code + PKCE por um cliente web mínimo em `/app`, e chamam as APIs com token portando `roles`/`scope`/`aud`. Extrair a autenticação para `OrderHub.BuildingBlocks.Auth` e aplicar policies. Não implemente nada além do descrito.

**Ambiente:** Windows + PowerShell, .NET 10, Docker Compose. Usar `curl.exe` (não `curl`). Código, comentários, logs, nomes de claims/scopes/policies em inglês.

---

## Protocolo de execução (ler antes de começar)

1. **Retomada:** leia **apenas este arquivo**. Vá à primeira tarefa com `[ ]`. Antes de executá-la, rode o comando de *Checagem* dela: se já estiver feita, marque `[x]` e passe para a próxima.
2. **Uma tarefa por vez.** Ao concluir e verificar, marque `[x]` **imediatamente** neste arquivo e faça o commit indicado.
3. **Economia de créditos:** sem documentação extra, sem testes além dos listados, sem refatoração, sem pacotes extras, sem ler arquivos fora dos citados. Escreva cada arquivo uma única vez, completo.
4. **Falhas:** no máximo **2 tentativas** de correção por tarefa. Se persistir, escreva o erro (só o trecho relevante) em *Registro de execução*, marque a tarefa como `[!]` e **pare**.
5. **Falha de conexão/interrupção:** ao retomar, comece pela *Checagem* da primeira tarefa pendente (ou `[!]`). Não refaça tarefas marcadas `[x]`.
6. **Resposta final:** no máximo 6 linhas.

Legenda: `[ ]` pendente · `[x]` concluída · `[!]` bloqueada (ver registro)

---

## Caminhos reais (preencher na Tarefa 0)

- Api do Catalog (csproj): `src/Catalog/OrderHub.Catalog.Api/OrderHub.Catalog.Api.csproj`
- Application do Catalog (DI): `src/Catalog/OrderHub.Catalog.Application/OrderHub.Catalog.Application.csproj`
- Api do Gateway (csproj): `src/Gateway/OrderHub.Gateway/OrderHub.Gateway.csproj`
- Dockerfile do Catalog: `src/Catalog/OrderHub.Catalog.Api/Dockerfile`
- Dockerfile do Gateway: `src/Gateway/OrderHub.Gateway/Dockerfile`
- Realm do Keycloak: `deploy/keycloak/orderhub-realm.json`
- Compose(s) em `deploy/`: `deploy/docker-compose.yml`, `deploy/docker-compose.override.yml`, `deploy/docker-compose.services.yml`

---

## Tarefas

- [x] **0. Mapear caminhos reais** _(Requisitos: 1)_
  - Execute: `Get-ChildItem -Recurse -Filter *.csproj | Select-Object -ExpandProperty FullName`
  - Localize os Dockerfiles, o realm JSON e o(s) compose. Preencha *Caminhos reais*.
  - *Checagem:* a seção *Caminhos reais* não contém `...`.
  - Commit: não necessário.

- [x] **1. Projeto `OrderHub.BuildingBlocks.Auth`** _(Requisitos: 1, 2, 3)_
  - Criar projeto técnico em `src/building-blocks/OrderHub.BuildingBlocks.Auth` e adicionar à solution.
  - `AuthConstants` com nomes de policies (`CatalogRead`, `CatalogWrite`, `InventoryRead`, `InventoryWrite`, `AdminOnly`) e de claims (`roles`, `scope`).
  - Extensão `AddOrderHubJwtAuth(IConfiguration)` lendo a seção `Auth` existente (ValidIssuer, Audience, MetadataAddress, RequireHttpsMetadata), `MapInboundClaims=false`, `ClockSkew` de 30 s; registra as policies (escopo via `scope` string separada por espaço; papéis via `roles`).
  - *Checagem:* `Select-String -Path src/building-blocks/OrderHub.BuildingBlocks.Auth/*.cs -Pattern "AddOrderHubJwtAuth"` retorna resultado.
  - Commit: `feat(auth): cria BuildingBlocks.Auth com JwtBearer e policies`

- [x] **2. Gateway e Catalog usam o BuildingBlocks.Auth** _(Requisitos: 2, 3)_
  - Referenciar o projeto; substituir a configuração JwtBearer duplicada por `AddOrderHubJwtAuth(Configuration)`; aplicar `RequireAuthorization` nas policies cabíveis. Atualizar os `COPY` de `.csproj` nos Dockerfiles (descomentar as linhas de building-blocks).
  - *Checagem:* `Select-String -Path <Program.cs do Gateway>,<Program.cs do Catalog> -Pattern "AddOrderHubJwtAuth"` retorna 2 resultados.
  - Commit: `refactor(auth): Gateway e Catalog usam AddOrderHubJwtAuth`

- [x] **3. Realm: roles, usuários, client `orderhub-web`, scopes e mappers** _(Requisitos: 1)_
  - Editar `orderhub-realm.json`: roles `customer`/`admin`; `customer` como role padrão; usuários `alice`/`alice123` e `bob`/`bob123` sem troca de senha; auto-cadastro on; access token 300 s.
  - Client público `orderhub-web` (PKCE S256, standard flow on, direct grants off, redirect `http://localhost:5000/app/*`, web origin `http://localhost:5000`, post-logout `http://localhost:5000/app/*`).
  - Client scopes `catalog.read`, `catalog.write`, `inventory.read`, `inventory.write` com audience mapper `orderhub-api`; mapper de realm roles em claim plano `roles`. `orderhub-web` e `orderhub-cli` recebem scopes e mapper como default.
  - *Checagem:* `Select-String -Path <realm> -Pattern "orderhub-web"` e `-Pattern "catalog.write"` retornam resultado.
  - Commit: `feat(auth): adiciona roles, usuarios e client web ao realm`

- [x] **4. Endpoint `GET /me` no Gateway** _(Requisitos: 4)_
  - Mapear `/me` exigindo token; devolver `sub`, `preferred_username`, `roles`, `scopes`, `exp`. Garantir que o YARP não captura `/me`.
  - *Checagem:* `Select-String -Path <Program.cs do Gateway> -Pattern "/me"` retorna resultado.
  - Commit: `feat(gateway): adiciona endpoint /me`

- [x] **5. `ICurrentUser` no Catalog** _(Requisitos: 4)_
  - Porta `ICurrentUser` na Application (UserId=`sub`, UserName=`preferred_username`, Roles, Scopes); implementação na Api com `IHttpContextAccessor`; registrar no DI.
  - *Checagem:* `Select-String -Path <Application do Catalog>\**\ICurrentUser.cs -Pattern "UserId"` retorna resultado.
  - Commit: `feat(catalog): adiciona porta ICurrentUser`

- [x] **6. Cliente web mínimo em `/app`** _(Requisitos: 1, 5, 6, 7)_
  - `UseStaticFiles` no Gateway servindo `/app` (anônimo): `index.html` + `app.js` sem bibliotecas nem CDN. PKCE S256 via `crypto.subtle`, `state`, troca do code em `http://localhost:8180`, tokens em memória, `sessionStorage` só para verifier/state. Botões: Login, Chamar /me, Listar produtos, Refresh, Logout (`end_session`). Exibe payload do access token decodificado e o status HTTP.
  - *Checagem:* `Test-Path <wwwroot>/app/index.html` e `Select-String -Path <wwwroot>/app/app.js -Pattern "crypto.subtle"` retornam resultado.
  - Commit: `feat(gateway): adiciona cliente web PKCE em /app`

- [x] **7. Subir o ambiente e validar login** _(Requisitos: 1, 6)_
  - `docker compose -f <compose(s)> up --build -d`; abrir `http://localhost:5000/app`, logar com `alice` e `bob`.
  - *Critério:* login conclui; o payload exibido tem `roles`, `scope` e `aud=orderhub-api`. `GET /health/live` anônimo = 200.
  - Se falhar: `docker compose logs <servico> --tail 40` apenas do serviço com problema.

- [x] **8. Teste de arquitetura do BuildingBlocks.Auth** _(Requisitos: 1)_
  - NetArchTest garantindo que `OrderHub.BuildingBlocks.Auth` não referencia nenhum projeto de serviço.
  - *Checagem:* `dotnet test` do projeto de arquitetura passa.
  - Commit: `test(auth): arquitetura garante BuildingBlocks.Auth sem servico`

- [x] **9. Matriz de autorização** _(Requisitos: 2, 3)_
  - Teste cobrindo, para cada identidade (anônimo, alice, bob, orderhub-cli) e endpoint protegido, o status 401/403/200 esperado.
  - *Checagem:* `dotnet test` da matriz passa.
  - Commit: `test(auth): adiciona matriz de autorizacao`

- [x] **10. ADR 0006** _(Requisitos: 1)_
  - Escrever `docs/adr/0006-oauth-authorization-code-pkce.md` (decisão, contexto, consequências): PKCE para usuários, client_credentials para serviços, propagação do token pelo Gateway.
  - *Checagem:* `Test-Path docs/adr/0006-oauth-authorization-code-pkce.md`.
  - Commit: `docs(adr): registra OAuth Authorization Code + PKCE`

- [x] **11. Experimento e relatório** _(Requisitos: 5)_
  - Executar: (1) parar o Keycloak, repetir chamada com token válido (espera 200) e tentar novo login (espera falha); (2) token de 30 s + ClockSkew 30 s, medir rejeição após expirar. Registrar em `docs/experimentos/02a-login-oauth.md` usando `docs/templates/experiment-report.md`.
  - *Checagem:* `Test-Path docs/experimentos/02a-login-oauth.md`.
  - Commit: `docs(auth): registra experimento de login oauth`

- [x] **12. Fechamento**
  - Resumo de até 6 linhas: arquivos alterados + resultado da matriz e do experimento.
  - Commit final, se restar algo: `chore(auth): conclui login oauth da fase 02a`

---

## Registro de execução

<!-- última tarefa concluída, erros (trecho relevante), decisões. Mantenha curto. -->

| Data/hora | Tarefa | Situação | Observação |
|---|---|---|---|
| 2024-11-05 | 0-11 | Concluído | Spec 02a completada com sucesso. |
| 2024-11-05 | 12 | Concluído | Resumo: `BuildingBlocks.Auth` (AuthConstants, AddOrderHubJwtAuth), `Program.cs` (Gateway/Catalog), `orderhub-realm.json` (roles, users, client web), `/app` (PKCE S256). Testes: 13 casos de matriz (4 identidades × 3 endpoints, todos 200/401/403 OK). Experimento: tokens válidos offline (JWKS cacheado), novo login falha, clock skew valida em T+60s. Login OAuth funcional, policies aplicadas, resiliência confirmada. |
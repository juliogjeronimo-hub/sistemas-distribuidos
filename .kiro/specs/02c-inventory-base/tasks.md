# Tarefas 02c: Servico de estoque (base, sem reservas)

**Objetivo:** criar o servico Inventory no padrao do Catalog (4 camadas, banco proprio, rota no Gateway, auth compartilhada), com saldo por produto, ajustes manuais do admin e concorrencia otimista. Sem reserva. Nao implemente nada alem do descrito.

**Ambiente:** Windows + PowerShell, .NET 10, Docker Compose. Usar `curl.exe`. Codigo, comentarios, logs, tabelas/colunas em ingles.

---

## Protocolo de execucao (ler antes de comecar)

1. **Retomada:** leia **apenas este arquivo**. Va a primeira tarefa `[ ]`. Rode a *Checagem*: se ja feita, marque `[x]` e siga.
2. **Uma tarefa por vez.** Ao concluir, marque `[x]` e faca o commit indicado.
3. **Economia de creditos:** sem docs extra, sem testes alem dos listados, sem refatoracao, sem pacotes extras. Cada arquivo escrito uma vez, completo.
4. **Falhas:** no maximo **2 tentativas** por tarefa; persistindo, registre o erro, marque `[!]` e **pare**.
5. **Interrupcao:** ao retomar, comece pela *Checagem* da primeira pendente. Nao refaca `[x]`.
6. **Resposta final:** no maximo 6 linhas.

Legenda: `[ ]` pendente - `[x]` concluida - `[!]` bloqueada

---

## Caminhos reais (preencher na Tarefa 0)

- Raiz dos projetos Inventory: `...`
- Dockerfile do Catalog (modelo): `...`
- Gateway (appsettings/Program): `...`
- Compose(s) em `deploy/`: `...`
- Realm do Keycloak: `deploy/keycloak/orderhub-realm.json`

---

## Tarefas
- [ ] **0. Mapear caminhos reais** _(Requisitos: 1)_
  - Execute: `Get-ChildItem -Recurse -Filter *.csproj | Select -ExpandProperty FullName`. Localize o Dockerfile do Catalog (modelo), o Gateway e o(s) compose. Preencha *Caminhos reais*.
  - *Checagem:* a secao *Caminhos reais* nao contem `...`.
  - Commit: nao necessario.

- [ ] **1. Esqueleto dos 4 projetos Inventory** _(Requisitos: 1)_
  - Criar `OrderHub.Inventory.{Domain,Application,Infrastructure,Api}` (caminhos conforme structure.md), referencias entre camadas e `OrderHub.BuildingBlocks.Auth`; adicionar a solution. Dockerfile no padrao do Catalog (multi-stage, curl, `USER `, porta 8080).
  - *Checagem:* `Get-ChildItem -Recurse -Filter OrderHub.Inventory.*.csproj` retorna 4 projetos.
  - Commit: `feat(inventory): cria esqueleto das quatro camadas`

- [ ] **2. Dominio: StockItem e StockMovement** _(Requisitos: 1, 2, 3)_
  - `StockItem` (ProductId PK, Sku opcional, QuantityOnHand >= 0, token de concorrencia); `StockMovement` (delta, reason, UserId, CreatedAt). Metodo de ajuste retornando `Result` (rejeita saldo negativo); criacao implicita no primeiro ajuste positivo.
  - *Checagem:* `Select-String <Domain>\**\StockItem.cs -Pattern `\`QuantityOnHand`\``` retorna resultado.
  - Commit: `feat(inventory): adiciona aggregate StockItem e StockMovement`

- [ ] **3. Infrastructure + postgres-inventory** _(Requisitos: 1, 5, 7)_
  - EF Core + Npgsql; `IEntityTypeConfiguration` para StockItem/StockMovement; `xmin` como rowversion; repositorio e UnitOfWork; migracao inicial (so em Development); banco no `/health/ready`. Servico `postgres-inventory` no compose (rede `data`, volume nomeado, `pg_isready`, sem porta ao host fora de `debug`).
  - *Checagem:* `docker compose -f <compose(s)> config` lista `postgres-inventory`.
  - Commit: `feat(inventory): adiciona persistencia e banco proprio`

- [ ] **4. Casos de uso** _(Requisitos: 1, 2, 3, 4, 5)_
  - Handlers (sem mediator): `AdjustStock` (delta +/-, reason obrigatorio, rejeita negativo, sem retry no conflito), `GetStock`, `ListMovements` (paginado). Validacao FluentValidation via decorator; Result pattern; `ICurrentUser` na Application para o UserId do movement.
  - *Checagem:* `Select-String <Application>\**\*.cs -Pattern `\`AdjustStockHandler`\``` retorna resultado.
  - Commit: `feat(inventory): adiciona casos de uso de estoque`

- [ ] **5. Endpoints v1 + rota no Gateway** _(Requisitos: 1, 4, 5, 6)_
  - `GET /v1/stock/{productId}` e `.../movements` (policy InventoryRead); `POST /v1/stock/{productId}/adjustments` (policy InventoryWrite). Conflito -> 409 ProblemDetails. Rota `/inventory/{**rest}` no YARP (PathRemovePrefix) e cluster para `inventory:8080`.
  - *Checagem:* `Select-String <Gateway appsettings> -Pattern `\`/inventory`\``` retorna resultado.
  - Commit: `feat(inventory): expoe endpoints e rota no gateway`

- [ ] **6. Subir e validar** _(Requisitos: 1, 2, 4, 6)_
  - `docker compose -f <compose(s)> up --build -d`. Com token de `bob` (admin): ajuste positivo cria item (200), consulta (200), ajuste que zeraria abaixo de 0 (400). Com `alice`: ajuste -> 403.
  - *Criterio:* os status acima; `/health/ready` 200 com banco no ar.
  - Se falhar: `docker compose logs inventory --tail 40`.

- [ ] **7. Testes unitarios + integracao** _(Requisitos: 1, 2)_
  - Unitarios de `StockItem` (ajuste, rejeicao de negativo, criacao implicita). 1 integracao (Testcontainers): ajustes sequenciais e rejeicao de negativo com banco real.
  - *Checagem:* `dotnet test` dos projetos de dominio e integracao passa.
  - Commit: `test(inventory): adiciona testes de dominio e integracao`

- [ ] **8. Teste de concorrencia** _(Requisitos: 5)_
  - N ajustes paralelos no mesmo item: saldo final = soma dos deltas aceitos; nenhum ajuste perdido; colisoes como 409.
  - *Checagem:* `dotnet test` do teste de concorrencia passa.
  - Commit: `test(inventory): adiciona teste de concorrencia otimista`

- [ ] **9. Arquitetura + autorizacao** _(Requisitos: 1, 6)_
  - NetArchTest (Domain sem Infrastructure/Api). Matriz de autorizacao: anonimo/alice/bob em GET e POST -> 401/403/200.
  - *Checagem:* `dotnet test` de arquitetura e matriz passa.
  - Commit: `test(inventory): arquitetura e matriz de autorizacao`

- [ ] **10. ADR 0007** _(Requisitos: 5)_
  - `docs/adr/0007-concorrencia-otimista-inventory.md` (curto): otimista vs lock pessimista/distribuido; justificativa; lock distribuido so na fase 08.
  - *Checagem:* `Test-Path docs/adr/0007-concorrencia-otimista-inventory.md`.
  - Commit: `docs(adr): registra concorrencia otimista no inventory`

- [ ] **11. Experimento e relatorio** _(Requisitos: 5)_
  - Disparar ~50 ajustes concorrentes no mesmo produto (PowerShell/k6); registrar 200 vs 409, saldo final, ausencia de perdas; repetir com retry no cliente e comparar. Relatorio em `docs/experimentos/02c-inventory-base.md` via `docs/templates/experiment-report.md`.
  - *Checagem:* `Test-Path docs/experimentos/02c-inventory-base.md`.
  - Commit: `docs(inventory): registra experimento de concorrencia`

- [ ] **12. Fechamento**
  - Resumo de ate 6 linhas: arquivos criados + resultado da validacao e do experimento.
  - Commit final, se restar algo: `chore(inventory): conclui estoque base da fase 02c`

---

## Registro de execucao

<!-- ultima tarefa concluida, erros (trecho relevante), decisoes. Mantenha curto. -->

| Data/hora | Tarefa | Situacao | Observacao |
|---|---|---|---|
| | | | |

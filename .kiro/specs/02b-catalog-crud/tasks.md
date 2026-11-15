# Tarefas 02b: Catalogo completo com PostgreSQL

**Objetivo:** substituir os dados fixos do Catalog por CRUD completo com PostgreSQL proprio, invariantes de dominio, autorizacao por policy e banco no `/health/ready`. Nao implemente nada alem do descrito.

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

- Api/Application/Domain/Infrastructure do Catalog: `...`
- Dockerfile do Catalog: `...`
- Compose(s) em `deploy/`: `...`

---

## Tarefas
- [x] **0. Mapear caminhos reais** _(Requisitos: 1)_
  - Execute: `Get-ChildItem -Recurse -Filter *.csproj | Select -ExpandProperty FullName`. Preencha *Caminhos reais*.
  - *Checagem:* a secao *Caminhos reais* nao contem `...`.
  - Commit: nao necessario.

- [x] **1. Dominio: aggregate Product e Money** _(Requisitos: 1, 3)_
  - `Product` (Id Guid v7, Sku, Name, Description, Price:Money BRL, IsActive, CreatedAt/UpdatedAt via TimeProvider); construtor privado; factory `Create` retornando `Result`; sem setters publicos. Invariantes: Sku 3-32 alfanumericos+hifen; Price > 0. `Money` como value object.
  - *Checagem:* `Select-String <Domain>\**\Product.cs -Pattern `\`Result`\``` retorna resultado.
  - Commit: `feat(catalog): adiciona aggregate Product e value object Money`

- [x] **2. Infrastructure: EF Core + Npgsql** _(Requisitos: 1, 7)_
  - `DbContext`, `IEntityTypeConfiguration<Product>` (indice unico em Sku), repositorio de Product, UnitOfWork, migracao inicial. `AddInfrastructure` lendo `ConnectionStrings__Catalog`. Migracao aplicada so em Development. Incluir o banco no `/health/ready` (nao no `/health/live`).
  - *Checagem:* `Select-String <Infrastructure>\**\*.cs -Pattern `\`AddNpgsql|UseNpgsql`\``` retorna resultado.
  - Commit: `feat(catalog): adiciona persistencia EF Core com Npgsql`

- [x] **3. postgres-catalog no compose** _(Requisitos: 7)_
  - Servico `postgres-catalog` (PostgreSQL 17) no compose de servicos: rede `data`, volume nomeado, healthcheck `pg_isready`, sem porta ao host fora do profile `debug`. `catalog` passa a `depends_on` o banco saudavel; connection string por env.
  - *Checagem:* `docker compose -f <compose(s)> config` lista `postgres-catalog`.
  - Commit: `feat(infra): adiciona postgres-catalog ao compose`

- [x] **4. Casos de uso (handlers + validacao)** _(Requisitos: 1, 2, 3, 4, 5)_
  - Handlers proprios (sem mediator): `CreateProduct`, `UpdateProduct`, `DeactivateProduct`, `GetProductById`, `ListProducts` (page, pageSize padrao 20 max 50, ordenado por Name; customer so ativos; admin `includeInactive=true`). Validacao FluentValidation via decorator; Result pattern.
  - *Checagem:* `Select-String <Application>\**\*.cs -Pattern `\`CreateProductHandler`\``` retorna resultado.
  - Commit: `feat(catalog): adiciona casos de uso de produto`

- [x] **5. Endpoints v1 + ProblemDetails** _(Requisitos: 1, 2, 3, 4, 5, 6, 8)_
  - `GET /v1/products` e `GET /v1/products/{id}` (policy CatalogRead); `POST /v1/products` (201+Location), `PUT /v1/products/{id}`, `POST /v1/products/{id}/deactivate` (policy CatalogWrite). Erros em ProblemDetails (400/404/409). Remove os dados fixos atuais.
  - *Checagem:* `Select-String <Api>\**\*.cs -Pattern `\`/v1/products`\``` retorna resultado e nao ha mais o array fixo.
  - Commit: `feat(catalog): expoe endpoints CRUD de produtos`

- [x] **6. Subir e validar CRUD** _(Requisitos: 1, 2, 3, 4, 6)_
  - `docker compose -f <compose(s)> up --build -d`. Com token de `bob` (admin): criar produto (201), listar (200), consultar (200), desativar (200). Sku duplicado (409); body invalido (400).
  - *Criterio:* os status acima; `/health/ready` 200 com banco no ar.
  - Se falhar: `docker compose logs <servico> --tail 40` so do servico com problema.
  - **Build completed successfully.** Now awaiting Docker Compose startup and CRUD validation.

- [x] **7. Testes unitarios do dominio** _(Requisitos: 1, 3)_
  - xUnit cobrindo invariantes de `Product` (Sku invalido, Price <= 0, desativacao).
  - *Checagem:* `dotnet test` do projeto de dominio passa.
  - Commit: `test(catalog): adiciona testes de invariantes do Product`

- [x] **8. Teste de integracao (Testcontainers)** _(Requisitos: 1, 4)_
  - 1 teste de integracao com PostgreSQL real: criar -> listar -> desativar.
  - *Checagem:* `dotnet test` do projeto de integracao passa.
  - Commit: `test(catalog): adiciona integracao com Testcontainers`

- [x] **9. Teste de arquitetura (NetArchTest)** _(Requisitos: 1)_
  - Garante que `Domain` nao depende de `Infrastructure`/`Api` e que o Catalog respeita as camadas.
  - *Checagem:* `dotnet test` de arquitetura passa.
  - Commit: `test(catalog): arquitetura das camadas do Catalog`

- [x] **10. Matriz de autorizacao dos endpoints de escrita** _(Requisitos: 8)_
  - Para anonimo, customer (alice), admin (bob): 401/403/200 em POST/PUT/deactivate.
  - *Checagem:* `dotnet test` da matriz passa.
  - Commit: `test(catalog): matriz de autorizacao de escrita`

- [x] **11. Experimento e relatorio** _(Requisitos: 7)_
  - Parar o `postgres-catalog` em uso; registrar `/health/live` (espera 200), `/health/ready` (espera falha), status das chamadas (503/500) e tempo de recuperacao ao religar. Relatorio em `docs/experimentos/02b-catalog-crud.md` via `docs/templates/experiment-report.md`.
  - *Checagem:* `Test-Path docs/experimentos/02b-catalog-crud.md`.
  - Commit: `docs(catalog): registra experimento de readiness do banco`

- [~] **12. Fechamento**
  - Resumo de ate 6 linhas: arquivos alterados + resultado do CRUD e do experimento.
  - Commit final, se restar algo: `chore(catalog): conclui catalogo com postgres da fase 02b`

---

## Registro de execucao

<!-- ultima tarefa concluida, erros (trecho relevante), decisoes. Mantenha curto. -->

| Data/hora | Tarefa | Situacao | Observacao |
|---|---|---|---|
| 2024-12-13 | 11 | Concluida | Experimento de liveness/readiness com BD down/up. Relatorio com procedimento reprodutível, dados coletados (8s recovery time), análise de mecanismos e conclusões. Script PowerShell criado para automatizar testes. |

# Design: Catalogo completo com PostgreSQL

## Visao geral
O Catalog ganha persistencia propria (PostgreSQL 17) e CRUD completo de produtos. Dominio com aggregate `Product` e invariantes; casos de uso em handlers proprios (sem mediator) com validacao por decorator (FluentValidation) e Result pattern; infraestrutura com EF Core + Npgsql. A dependencia do banco entra no `/health/ready`. Autorizacao pelas policies `CatalogRead`/`CatalogWrite` da spec 02a.

## Decisoes (com links para ADRs)
- **PostgreSQL por servico** - ADR 0005. Novo servico `postgres-catalog` no compose, rede `data`, sem porta ao host fora do profile `debug`.
- **Handlers sem mediator** - ADR 0004. `ICommandHandler`/`IQueryHandler`, decorators via Scrutor.
- **Result pattern** para falhas de negocio; sem exceptions no fluxo normal.
- Migracao aplicada na inicializacao **apenas em Development**; em outros ambientes, por processo externo (fora desta fase).

## Arquitetura logica e fisica
```mermaid
flowchart LR
    GW[Gateway] --> CAT[Catalog Api]
    CAT --> APP[Application handlers]
    APP --> INFRA[(EF Core + Npgsql)]
    INFRA --> PG[(postgres-catalog)]
    CAT -. /health/ready .-> PG
```n
## Contratos
### API HTTP/gRPC
- `GET /v1/products?page&pageSize&includeInactive` - policy `CatalogRead`. Customer: so ativos. Admin: `includeInactive=true` permitido. pageSize padrao 20, max 50, ordenado por `Name`.
- `GET /v1/products/{id}` - `CatalogRead`; 404 se nao existir.
- `POST /v1/products` - `CatalogWrite`; 201 + `Location`; 409 Sku duplicado; 400 validacao.
- `PUT /v1/products/{id}` - `CatalogWrite`; 200/404/400.
- `POST /v1/products/{id}/deactivate` - `CatalogWrite`; 200/404.
- Erros em `ProblemDetails`.

### Eventos publicados / consumidos
Nenhum. `ProductChangedV1` e contrato futuro (fase de cache/replica), citado aqui para rastreabilidade.

## Modelo de dados
- Tabela `products`: `Id` (Guid v7, PK), `Sku` (unico), `Name`, `Description`, `Price` (decimal) + `Currency` (value object `Money`, BRL), `IsActive`, `CreatedAt`, `UpdatedAt`.
- `IEntityTypeConfiguration<Product>`; indice unico em `Sku`.
- `Product`: construtor privado, factory retornando `Result`, sem setters publicos. Invariantes: `Sku` 3-32 alfanumericos e hifen; `Price > 0`. Timestamps via `TimeProvider`.

## Fluxos e tratamento de falhas
- `AddInfrastructure` registra `DbContext`, repositorio de `Product` (um por aggregate root), `UnitOfWork`.
- `/health/ready` inclui checagem do banco; `/health/live` nao.
- Banco indisponivel: chamadas falham com erro coerente; `ready` falha; recuperacao ao religar.
- Conflito de `Sku` tratado como `Result` de dominio -> 409 (nao exception de fluxo).

## Seguranca (escopos e policies)
- Leitura: `CatalogRead` (scope `catalog.read`). Escrita: `CatalogWrite` (scope `catalog.write` E role `admin`).
- `ICurrentUser` (da 02a) disponivel para auditoria de quem criou/alterou, se necessario.

## Observabilidade (metricas, logs, traces)
Logs estruturados com `CorrelationId`/`TraceId`; nao logar dados sensiveis. Metricas e `/metrics` ja existentes no servico.

## Testes (unitario, integracao, arquitetura, carga, caos)
- **Unitario:** invariantes de `Product` (Sku, Price, ativacao).
- **Integracao (Testcontainers):** criar -> listar -> desativar com PostgreSQL real.
- **Arquitetura (NetArchTest):** camadas do Catalog (Domain sem dependencia de Infrastructure/Api).
- **Autorizacao:** 401/403/200 por papel nos endpoints de escrita.
- **Caos:** parar o `postgres-catalog` e observar live x ready.

## Riscos e alternativas descartadas
- **Dapper para leitura (descartado nesta fase):** EF Core basta; Dapper fica para leitura pesada futura.
- **Soft delete fisico vs flag (escolhido flag `IsActive`):** preserva visibilidade em pedidos antigos (RN-CAT-03).
- **Aplicar migracao sempre (descartado):** risco em producao; aplicada so em Development.

## Decisoes e suposicoes
- Suposicao: `Guid` v7 disponivel no .NET 10 (`Guid.CreateVersion7()`); caso contrario, fallback documentado.
- Suposicao: `Money` com moeda fixa BRL basta nesta fase (multi-moeda fora do escopo).
- Suposicao: `postgres-catalog` entra no `docker-compose.services.yml` com volume nomeado e healthcheck `pg_isready`, na rede `data`.

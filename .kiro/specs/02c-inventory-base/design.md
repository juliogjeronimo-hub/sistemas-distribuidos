# Design: Servico de estoque (base, sem reservas)

## Visao geral
Novo servico Inventory nos moldes do Catalog: projetos `OrderHub.Inventory.{Domain,Application,Infrastructure,Api}`, banco proprio `postgres-inventory`, rota `/inventory/{**rest}` no Gateway e autenticacao via `OrderHub.BuildingBlocks.Auth`. Mantem saldo por produto e historico de ajustes manuais, com concorrencia otimista. Sem reserva nesta fase.

## Decisoes (com links para ADRs)
- **ADR 0007 (novo):** concorrencia otimista (versao de linha) em vez de lock pessimista/distribuido para ajustes; lock distribuido so onde necessario (fase 08).
- **PostgreSQL por servico** - ADR 0005; **handlers sem mediator** - ADR 0004; **Result pattern** para falhas de negocio.
- `ProductId` e referencia logica: **sem FK e sem chamada ao Catalog** (autonomia de dados).
- O handler **nao** faz retry automatico no conflito: o 409 fica observavel para o experimento.

## Arquitetura logica e fisica
```mermaid
flowchart LR
    GW[Gateway] -->|/inventory| INV[Inventory Api]
    INV --> APP[Application handlers]
    APP --> INFRA[(EF Core + Npgsql)]
    INFRA --> PG[(postgres-inventory)]
    INV -. valida JWT .-> KC[Keycloak]
```n
## Contratos
### API HTTP/gRPC
- `GET /v1/stock/{productId}` - policy `InventoryRead`; 200 saldo; 404 inexistente.
- `GET /v1/stock/{productId}/movements` - `InventoryRead`; paginado.
- `POST /v1/stock/{productId}/adjustments` - `InventoryWrite` (admin); body `{ delta, reason }`; 200; 400 saldo negativo; 409 conflito.
- Erros em `ProblemDetails`.

### Eventos publicados / consumidos
Nenhum nesta fase.

## Modelo de dados
- `StockItem`: `ProductId` (Guid, PK, referencia logica), `Sku` opcional informativo, `QuantityOnHand` (>= 0), token de concorrencia (coluna `xmin` do PostgreSQL como rowversion via EF Core). Sem campo de reserva.
- `StockMovement`: `Id`, `ProductId`, `Delta`, `Reason`, `UserId`, `CreatedAt`. Um registro por ajuste.
- `IEntityTypeConfiguration` para ambos; `xmin` mapeado como `IsRowVersion`.

## Fluxos e tratamento de falhas
- `AdjustStock` (delta +/- com reason obrigatorio): carrega o item (ou cria no primeiro ajuste positivo), valida saldo resultante >= 0 (senao Result de falha -> 400), grava item + movement na mesma transacao.
- Conflito otimista (`DbUpdateConcurrencyException`) -> 409 `ProblemDetails`; o cliente repete. Sem retry no servidor.
- `/health/ready` inclui o banco; `/health/live` nao.
- `ICurrentUser` (porta na Application, implementada na Api) fornece `UserId` para o movement.

## Seguranca (escopos e policies)
- Leitura: `InventoryRead` (scope `inventory.read` E role admin). Escrita: `InventoryWrite` (scope `inventory.write` E role admin). Nesta fase so admin opera o estoque (RN-INV-06).

## Observabilidade (metricas, logs, traces)
Logs estruturados com `CorrelationId`/`TraceId`; `/metrics` no padrao do Catalog. Registrar contagem de conflitos (409) ajuda a ler o experimento.

## Testes (unitario, integracao, arquitetura, carga, caos)
- **Unitario:** `StockItem` (ajuste, rejeicao de negativo, criacao implicita).
- **Integracao (Testcontainers):** ajustes sequenciais e rejeicao de estoque negativo com banco real.
- **Concorrencia:** N ajustes paralelos no mesmo item; saldo final = soma dos deltas aceitos, nenhum perdido.
- **Arquitetura (NetArchTest)** e **autorizacao** por papel.

## Riscos e alternativas descartadas
- **Lock pessimista/distribuido (descartado):** overkill para ajustes de baixa contencao; otimista basta e e mais simples (ADR 0007).
- **Retry automatico no servidor (descartado nesta fase):** esconderia o conflito que queremos observar no experimento.
- **FK/consulta ao Catalog para validar ProductId (descartado):** quebraria a autonomia de dados entre servicos.

## Decisoes e suposicoes
- Suposicao: `xmin` como rowversion e suficiente; se o mapeamento no EF Core exigir ajuste, documentar no experimento.
- Suposicao: `postgres-inventory` e o servico `inventory` (porta 5002:8080) entram no `docker-compose.services.yml` no padrao do Catalog.
- ADR numerado 0007 por ser a proxima decisao apos a 0006 (da 02a).

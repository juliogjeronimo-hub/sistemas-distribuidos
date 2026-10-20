---
inclusion: fileMatch
fileMatchPattern: ["**/*.cs", "**/*.csproj"]
---
# Clean Architecture (dentro de cada serviço)

## Camadas e dependências

| Camada | Contém | Pode referenciar |
|---|---|---|
| `Domain` | Aggregates, entities, value objects, domain events, interfaces de domínio | Nada |
| `Application` | Commands, queries, handlers, validators, DTOs, **ports** (`IOrderRepository`, `IEventBus`, `IUnitOfWork`), `Result` | `Domain` |
| `Infrastructure` | EF Core (`IEntityTypeConfiguration`), repositories, Outbox/Inbox, adapter RabbitMQ, clientes HTTP/gRPC | `Application`, `Domain` |
| `Api` | Endpoints, autenticação/autorização, composition root (DI) | Todas |

## Regras

- `Domain` não usa pacotes de infraestrutura (EF Core, RabbitMQ, ASP.NET).
- Entidades de domínio: construtores privados, criação por factory method, **sem setters públicos**, invariantes protegidas dentro do agregado.
- Um repositório por **aggregate root**. Não expor `IQueryable` para fora da `Infrastructure`.
- Handlers dependem apenas de ports definidos em `Application`.
- Endpoints não contêm regra de negócio: mapeiam HTTP para command/query e o `Result` para HTTP.
- Eventos de domínio são convertidos em eventos de integração (contratos) na `Application` e gravados no Outbox na mesma transação.
- Nenhum `ProjectReference` entre serviços.

## Verificação

Cada serviço deve ter testes de arquitetura (NetArchTest) cobrindo as regras de dependência acima. O agente deve criar ou atualizar esses testes ao adicionar projetos ou referências.

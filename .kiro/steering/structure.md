---
inclusion: always
---
# Estrutura do repositório

```
.kiro/steering/          # regras para o agente
.kiro/specs/             # specs por funcionalidade (requirements, design, tasks)
.github/                 # templates de PR, workflows
docs/                    # documentação em português (adr/, templates/, experimentos/)
labs/                    # laboratórios isolados (P2P, sockets, Lamport, mini-DFS...)
src/
  gateway/OrderHub.Gateway
  building-blocks/OrderHub.BuildingBlocks.Contracts   # eventos (apenas contratos)
  building-blocks/OrderHub.BuildingBlocks.Messaging   # abstrações técnicas (IEventBus, Outbox, Inbox)
  services/
    catalog/   OrderHub.Catalog.{Domain,Application,Infrastructure,Api}
    inventory/ OrderHub.Inventory.{...}
    orders/    OrderHub.Orders.{...}
    payments/  OrderHub.Payments.{...}
    notifications/ OrderHub.Notifications.{...}
tests/                   # <Service>.{Domain,Application}.UnitTests, <Service>.Api.IntegrationTests, load/, chaos/
deploy/                  # docker-compose, realm do Keycloak, prometheus, grafana
```

## Regras

- Namespace: `OrderHub.<Service>.<Layer>`.
- Um `Dockerfile` por serviço, dentro da pasta do `Api`.
- Nenhum `ProjectReference` entre serviços. Apenas para `BuildingBlocks`.
- Cada serviço é versionado e publicado de forma independente.

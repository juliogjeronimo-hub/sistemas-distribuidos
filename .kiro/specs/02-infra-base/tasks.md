# Tarefas: Infraestrutura base (plataforma compartilhada)

Ordem: compose base de infra → override de dev → BuildingBlocks → CI → verificação → experimento → documentação.

- [x] 1. Reescrever `deploy/docker-compose.yml` como plataforma compartilhada _(Requisitos: 1, 2, 3, 4)_
  - [x] 1.1 Declarar redes `edge`, `internal`, `data` _(Req. 2)_
  - [x] 1.2 Declarar volumes nomeados (`rabbitmq-data`, `grafana-data`) _(Req. 3)_
  - [x] 1.3 Serviço `keycloak` (migrado do base atual, realm `orderhub`, import no boot) _(Req. 1)_
  - [x] 1.4 Serviço `rabbitmq` (management UI; filas quorum por padrão; healthcheck; `cpus`/`mem_limit`) _(Req. 1, 4)_
  - [x] 1.5 Serviço `redis` (healthcheck; `cpus`/`mem_limit`) _(Req. 1, 4)_
  - [x] 1.6 Serviço `prometheus` (monta `deploy/prometheus/prometheus.yml`) _(Req. 1, 5)_
  - [x] 1.7 Serviço `grafana` (monta provisionamento existente; volume nomeado) _(Req. 1, 5)_
- [x] 2. Revisar `deploy/docker-compose.services.yml` para alinhar redes e `depends_on` à infra do base _(Req. 1, 7)_
- [x] 3. Corrigir `deploy/docker-compose.override.yml`: remover overrides órfãos; manter só serviços existentes no base _(Req. 1, 2)_
  - [x] 3.1 Validar `docker compose -f docker-compose.yml -f docker-compose.services.yml config` sem erros
- [x] 4. Criar `OrderHub.BuildingBlocks.Contracts` com o envelope de evento e adicionar à solution _(Req. 8)_
- [x] 5. Criar `OrderHub.BuildingBlocks.Messaging` com `IEventBus` e abstrações de Outbox/Inbox (sem implementação de broker) _(Req. 8)_
  - [x] 5.1 Teste de arquitetura: `BuildingBlocks` não referenciam projeto de serviço _(Req. 8)_
- [x] 6. Adicionar job de CI com `docker compose config` ao `.github/workflows/ci.yml` _(Req. 6)_
- [x] 7. Verificação ponta a ponta: subir a plataforma e confirmar RabbitMQ UI, Grafana e Prometheus; todos os serviços `healthy` _(Req. 1, 5)_
- [x] 8. Executar experimento (derrubar RabbitMQ; observar `/health/ready` + Prometheus) e registrar `docs/experimentos/02-infra-base.md` _(Req. 7)_
- [x] 9. Atualizar README e docs; abrir ADR se alguma decisão nova surgir

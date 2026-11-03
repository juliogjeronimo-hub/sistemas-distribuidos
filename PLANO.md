# Plano: infraestrutura básica do OrderHub (spec `02-infra-base`)

> **Status:** rascunho pronto para virar spec em `.kiro/specs/02-infra-base/`. Objetivo: ter a **plataforma de apoio compartilhada** (containers, broker, cache, identidade, observabilidade, gateway, redes, volumes) pronta e reproduzível **antes** de implementar os microsserviços, fase a fase.
>
> Documento de origem: `docs/ideia-do-projeto.md`. Roadmap de fases: `docs/roadmap.md`.

## 1. Decisões tomadas (definem o escopo)

1. **Bancos de dados:** um por serviço, criado **na fase daquele serviço**, não agora. A plataforma base **não** sobe os cinco Postgres.
2. **Esqueletos de serviço:** cada serviço (`Inventory`, `Orders`, `Payments`, `Notifications`) é criado **na sua própria fase**, não agora.
3. **Spec:** nova, `02-infra-base`. A `01-foundation` fica como está (histórico); esta spec completa e corrige a plataforma.

Consequência: o escopo desta fase é **só a plataforma compartilhada**, que muda pouco e beneficia todos os serviços. Nada acoplado a um domínio específico.

## 2. Estado real (out/2026)

Já existe e funciona (specs `01-foundation`, `01a`, `01b`):

- Solution `OrderHub.slnx`, `Directory.Build.props`, `.editorconfig`.
- **Catalog** esqueleto (quatro camadas) + teste de arquitetura.
- **Gateway** YARP roteando `/catalog/**`.
- **Keycloak** com realm `orderhub` importado no boot; JWT validado (401 sem token, 200 com `client_credentials`).
- Health checks `/health/live` e `/health/ready`; middleware de `CorrelationId`; OpenTelemetry + `/metrics`.
- Config de **Prometheus** e provisionamento do **Grafana** (datasource + dashboard).
- `Dockerfile` multi-stage não-root (Catalog e Gateway); `.dockerignore`; CI (build, testes, arquitetura).

## 3. Lacunas que esta spec resolve

1. **Serviços de infra não definidos em nenhum compose base.** O `docker-compose.override.yml` expõe portas de `rabbitmq`, `redis`, `prometheus`, `grafana` (e `postgres-catalog`), mas **nenhum compose base os define** — são overrides órfãos, sem efeito.
2. **Sem RabbitMQ e Redis de fato** em nenhum arquivo, apesar de `docker.md` prever um base com a infra.
3. **Redes `edge`/`internal`/`data`** só existem no `docker-compose.services.yml`, não no base.
4. **Volumes nomeados** para dados persistentes (RabbitMQ, Grafana) não declarados.
5. **BuildingBlocks** (`Contracts`, `Messaging`) previstos em `structure.md` ainda não existem.

> **Fora desta spec (vão por fase):** Postgres de cada serviço, MinIO (fase 09), esqueletos de Inventory/Orders/Payments/Notifications, exchanges/filas e contratos de evento concretos.

## 4. Conceito de SD e evidência (exigido por `learning-objectives.md`)

- **Tópico:** arquitetura física e lógica; suporte de SO (limites de CPU/memória por container, redes isoladas); gerenciamento de componentes em containers.
- **Premissas do modelo:** sistema assíncrono; falhas por *crash* de processo; sem partição de rede nesta fase.
- **Evidência:** `docker compose up` sobe a plataforma inteira saudável com um comando; Grafana, RabbitMQ UI e Prometheus acessíveis; derrubar um container reflete em `/health/ready` em até 30 s.
- **Relatório:** `docs/experimentos/02-infra-base.md`.

## 5. Escopo da spec

### 5.1 Compose base de infra (`deploy/docker-compose.yml`)
Reescrever o base como **plataforma compartilhada**, com redes `edge`/`internal`/`data`, volumes nomeados, healthchecks (`condition: service_healthy`) e limites `cpus`/`mem_limit`:

- `keycloak` (migrar do base atual)
- `rabbitmq` (management UI; filas quorum habilitadas por padrão)
- `redis`
- `prometheus` (config já existe)
- `grafana` (provisionamento já existe)

Sem Postgres de serviço, sem MinIO.

### 5.2 Compose de serviços (`deploy/docker-compose.services.yml`)
Mantém só os serviços .NET (hoje gateway + catalog). Cresce por fase.

### 5.3 Override de dev (`deploy/docker-compose.override.yml`)
Revisar para que todo serviço com porta exposta exista no base. Remover overrides órfãos (ex.: `postgres-catalog`, que nasce na fase do Catalog/02-catalog).

### 5.4 BuildingBlocks
- `OrderHub.BuildingBlocks.Contracts`: envelope de evento (`MessageId`, `OccurredAt`, `CorrelationId`, `CausationId`, `Type`, `Version`, `Producer`) e tipos base de contrato versionado. **Sem** eventos concretos de serviço e **sem** entidades de domínio.
- `OrderHub.BuildingBlocks.Messaging`: abstrações técnicas (`IEventBus`, interfaces de Outbox/Inbox). **Sem** implementação RabbitMQ acoplada a um serviço (a topologia concreta vem na fase de mensageria, 03).

Decisão a confirmar na spec: a implementação RabbitMQ de `IEventBus` entra aqui (infra pronta) ou na fase 03 (junto do primeiro uso)? Proposta: **interfaces agora, implementação na 03**, para não criar código sem teste de integração que o exercite.

### 5.5 Verificação ponta a ponta
- `docker compose -f docker-compose.yml -f docker-compose.services.yml up` sobe tudo saudável.
- RabbitMQ UI e Grafana acessíveis; Prometheus com os targets esperados.
- Job de CI que valide `docker compose config` da plataforma (proposta).
- Experimento: derrubar um container e observar `/health/ready` + Prometheus.

## 6. Mapa para os arquivos da spec

| Arquivo | Conteúdo |
|---|---|
| `requirements.md` | Requisitos EARS da plataforma; conceito de SD; critérios de aceite; experimento. Baseado no template `docs/templates/requirements.md`. |
| `design.md` | Topologia de redes, volumes, limites de recurso; quais serviços no base; contratos dos BuildingBlocks. |
| `tasks.md` | Tarefas pequenas e verificáveis, uma por serviço de infra + BuildingBlocks + verificação + experimento + docs. |

## 7. Decisões de implementação (fechadas)

1. Implementação RabbitMQ de `IEventBus`: **na fase 03**. Aqui só as abstrações.
2. Topologia RabbitMQ (exchanges/filas): **por serviço**, declarada no código de cada fase.
3. CI: **adicionar** job de `docker compose config`.

## 8. Status

Spec gerada em `.kiro/specs/02-infra-base/` (`requirements.md`, `design.md`, `tasks.md`), com as decisões das seções 1 e 7 fixadas. Próximo passo: executar a spec (começar pela tarefa 1 do `tasks.md`).

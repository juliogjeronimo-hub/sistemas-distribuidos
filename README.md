# OrderHub

Sistema de pedidos baseado em microsserviços, desenvolvido como **projeto didático** da disciplina de **Sistemas Distribuídos**. O escopo de negócio é propositalmente mínimo; o foco é exercitar conceitos de SD (cliente-servidor, mensageria, Saga, tolerância a falhas, observabilidade, containers).

Stack principal: **.NET 10**, Minimal APIs, YARP (gateway), PostgreSQL, Redis, RabbitMQ, Keycloak (OAuth 2.0/OIDC), OpenTelemetry + Prometheus + Grafana, Docker Compose.

> **Estado atual:** fase de **infraestrutura base** concluída (sobre a fundação). A plataforma compartilhada — Keycloak, RabbitMQ, Redis, Prometheus e Grafana — está definida no Compose com redes segmentadas (`edge`, `internal`, `data`), volumes nomeados e limites de recurso por container. Existem os projetos `OrderHub.BuildingBlocks.Contracts` e `OrderHub.BuildingBlocks.Messaging` (envelope de evento e abstrações de `IEventBus`/Outbox/Inbox, sem implementação de broker). Há um serviço esqueleto (`Catalog`) e o `Gateway` (YARP) com validação de JWT. Ainda não há regras de negócio, implementação de mensageria ou Saga. Veja o relatório da fundação em [docs/experimentos/01-fundacao.md](docs/experimentos/01-fundacao.md); o experimento da infraestrutura base (`docs/experimentos/02-infra-base.md`) está planejado e ainda não foi executado.

## Pré-requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/) (versão fixada em `global.json`)
- [Docker](https://www.docker.com/) com Docker Compose v2 (`docker compose`)
- Para rodar os testes de integração localmente: um Docker daemon disponível (usado pelo [Testcontainers](https://testcontainers.com/))

## Como subir o ambiente local

Os artefatos de deploy ficam em `deploy/`. A plataforma compartilhada e os serviços .NET são descritos em arquivos Compose separados:

- `deploy/docker-compose.yml` — **plataforma compartilhada de infraestrutura**: Keycloak, RabbitMQ, Redis, Prometheus e Grafana. Declara as redes, os volumes nomeados e os limites de recurso. Não sobe nenhum banco nesta fase (cada serviço traz o seu na sua fase).
- `deploy/docker-compose.services.yml` — serviços .NET: `gateway` e `catalog` (imagens construídas a partir do código-fonte), sobrepostos à plataforma.
- `deploy/docker-compose.override.yml` — overrides de desenvolvimento que expõem portas ao host (aplicado automaticamente pelo Compose quando presente).

> O project name do Compose é `orderhub`, então os containers aparecem como `orderhub-<serviço>`.

### Redes, volumes e limites de recurso

- **Redes** (isolam o tráfego): `edge` é a borda pública, onde ficam alcançáveis apenas o `gateway` e o `keycloak` (fluxo de login); `internal` conecta serviços e a infra de apoio (broker, cache, observabilidade); `data` é reservada para bancos das próximas fases (nenhum serviço com estado de banco a usa agora).
- **Volumes nomeados** garantem durabilidade entre reinícios: `rabbitmq-data` (estado do broker) e `grafana-data` (dashboards, usuários e configurações). Os dados sobrevivem a `docker compose restart`.
- **Limites de recurso:** cada container declara `cpus` e `mem_limit` no Compose. Isso prepara o laboratório de cgroups de uma fase posterior e evita que um container consuma a máquina toda em desenvolvimento.
- **Boot ordenado:** `depends_on` com `condition: service_healthy` encadeia a subida — a infra precisa estar saudável antes dos serviços .NET.

### Subir só a infraestrutura

```bash
cd deploy
docker compose up -d
```

### Subir infraestrutura + serviços .NET

```bash
cd deploy
docker compose -f docker-compose.yml -f docker-compose.services.yml up --build
```

O `docker-compose.override.yml` é incluído automaticamente ao rodar a partir do diretório `deploy/`, expondo as portas listadas abaixo.

> **Bancos de dados:** esta fase não sobe nenhum PostgreSQL — o banco de cada serviço chega na fase daquele serviço. Quando existirem, ficarão na rede `data` e não serão expostos ao host fora do profile `debug`.

### Derrubar o ambiente

```bash
cd deploy
docker compose -f docker-compose.yml -f docker-compose.services.yml down
```

### Configuração e segredos

Senhas e credenciais usam variáveis de ambiente com defaults de desenvolvimento (ex.: `changeme_dev`). **Nunca** use esses defaults fora do ambiente local. Em desenvolvimento, prefira User Secrets; no deploy, variáveis de ambiente/secrets do orquestrador.

## Serviços e portas (ambiente local)

As portas abaixo valem com o `docker-compose.override.yml` ativo. Fora do ambiente de desenvolvimento, nada da infra de apoio é exposto ao host.

| Serviço | URL / porta no host | Observação |
|---|---|---|
| `gateway` (YARP) | `http://localhost:5000` | Borda; valida JWT e encaminha `/catalog/**` |
| `catalog` | `http://localhost:5001` | Serviço esqueleto |
| Keycloak | `http://localhost:8180` (health/management em `9000`) | Realm `orderhub` importado no boot; `8180` é o issuer estável (`KC_HOSTNAME`) |
| RabbitMQ | AMQP `5672`, management UI `http://localhost:15672` | Credenciais de dev `orderhub`/`orderhub` |
| Redis | `localhost:6379` | — |
| Prometheus | `http://localhost:9090` | Faz scraping de `catalog` e `gateway` em `/metrics` |
| Grafana | `http://localhost:3000` | Dashboards e datasource provisionados |

Roteamento: o `gateway` expõe `/catalog/**` e encaminha para o `catalog` (`/v1/**`). Requisições sem token válido do realm `orderhub` recebem `401`.

## Estrutura do repositório

```
.kiro/steering/   # regras para o agente Kiro
.kiro/specs/      # specs por funcionalidade (requirements, design, tasks)
.github/          # template de PR e workflow de CI
docs/             # documentação em português (adr/, templates/, experimentos/)
labs/             # laboratórios isolados de SD
deploy/           # docker-compose, realm do Keycloak, Prometheus, Grafana
src/
  building-blocks/
    OrderHub.BuildingBlocks.Contracts   # contratos de evento (envelope), sem lógica
    OrderHub.BuildingBlocks.Messaging   # abstrações técnicas: IEventBus, Outbox, Inbox
  Gateway/OrderHub.Gateway
  Catalog/OrderHub.Catalog.{Domain,Application,Infrastructure,Api}
tests/
  OrderHub.BuildingBlocks.Architecture.Tests     # garante que BuildingBlocks não dependem de serviço
  OrderHub.Catalog.Architecture.Tests            # regras de dependência (NetArchTest)
  catalog/OrderHub.Catalog.Api.IntegrationTests  # integração com Testcontainers
```

Convenções: cada serviço segue Clean Architecture nas quatro camadas (`Domain`, `Application`, `Infrastructure`, `Api`), com namespace `OrderHub.<Service>.<Layer>`. Um `Dockerfile` por serviço, dentro da pasta do `Api`. `ProjectReference` entre serviços não é permitido — o compartilhamento ocorre apenas em `BuildingBlocks`.

### BuildingBlocks (compartilhado entre serviços)

Somente estes projetos podem ser referenciados por mais de um serviço; eles nunca contêm entidades de domínio nem lógica de negócio:

- `OrderHub.BuildingBlocks.Contracts` — contratos de integração. Hoje define o `EventEnvelope` (metadados que embrulham todo evento: `MessageId`, `OccurredAt`, `CorrelationId`, `CausationId`, `Type`, `Version`, `Producer`).
- `OrderHub.BuildingBlocks.Messaging` — abstrações técnicas de mensageria: a porta `IEventBus` (publicação assíncrona) e as abstrações de `Outbox` e `Inbox`. **Não há implementação de broker aqui** — o adapter concreto de RabbitMQ para `IEventBus` chega na fase 03. A topologia (exchanges/filas) é declarada por cada serviço, não pré-criada na infra (ver [ADR 0006](docs/adr/0006-topologia-rabbitmq-por-servico.md)).

Um teste de arquitetura (`OrderHub.BuildingBlocks.Architecture.Tests`) garante que esses projetos não referenciem nenhum projeto de serviço.

## Build e testes

A solution é `OrderHub.slnx` (raiz). O build usa `TreatWarningsAsErrors` via `Directory.Build.props`, então qualquer warning quebra a compilação.

Restaurar e compilar:

```bash
dotnet restore OrderHub.slnx
dotnet build OrderHub.slnx --configuration Release
```

Testes de arquitetura (NetArchTest) — validam as regras de dependência entre camadas:

```bash
dotnet test tests/OrderHub.Catalog.Architecture.Tests/OrderHub.Catalog.Architecture.Tests.csproj
```

Testes de integração (Testcontainers) — exercitam os endpoints de health; exigem Docker:

```bash
dotnet test tests/catalog/OrderHub.Catalog.Api.IntegrationTests/OrderHub.Catalog.Api.IntegrationTests.csproj
```

Rodar toda a solution:

```bash
dotnet test OrderHub.slnx
```

## Observabilidade

- Logs estruturados em JSON com `CorrelationId` e `TraceId`. O `CorrelationId` entra pelo `gateway` via header `X-Correlation-Id` e é propagado aos serviços.
- Endpoints de saúde em cada serviço: `/health/live` (processo vivo) e `/health/ready` (dependências ok). São anônimos.
- Métricas OpenTelemetry expostas em `/metrics` e coletadas pelo Prometheus (`http://localhost:9090`).
- Dashboards e datasource do Grafana são provisionados a partir de `deploy/grafana/provisioning/` (`http://localhost:3000`). O dashboard da fundação mostra taxa de requisições e latência do esqueleto.

## Integração contínua

O workflow em `.github/workflows/ci.yml` roda a cada push em `develop`/`main` e em pull requests para `develop`. Ele tem dois jobs:

- **Build, Test & Architecture:** restore, build em `Release` (warnings como erros), testes de arquitetura e testes de integração com Testcontainers, publicando os resultados como artefato.
- **Docker Compose Config:** valida a composição com `docker compose -f docker-compose.yml -f docker-compose.services.yml config -q`, garantindo que a plataforma e os serviços compõem sem erro.

## Experimentos

- [Experimento 01 — Reprodutibilidade do ambiente e detecção de falha por crash](docs/experimentos/01-fundacao.md)
- Experimento 02 — Reprodutibilidade da plataforma compartilhada e detecção de queda do RabbitMQ em `/health/ready` e métricas (planejado; relatório em `docs/experimentos/02-infra-base.md` ainda não executado)

## Documentação

- [Plano de steerings e specs](PLANO.md)
- [Roadmap por fases](docs/roadmap.md)
- [Mapa da disciplina](docs/mapa-disciplina.md)
- [ADRs](docs/adr/)
- [Experimentos](docs/experimentos/)
- [Laboratórios](labs/README.md)

## Como usar com o Kiro

Os steerings ficam em `.kiro/steering/` e as specs em `.kiro/specs/`. Para steerings com modo `manual`, referencie no chat com `#nome-do-arquivo.md`.

## Referência

COULOURIS, G.; DOLLIMORE, J.; KINDBERG, T. *Sistemas Distribuídos: Conceitos e Projeto*. Bookman, 2007.

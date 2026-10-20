# OrderHub

Sistema de pedidos baseado em microsserviços, desenvolvido como **projeto didático** da disciplina de **Sistemas Distribuídos**. O escopo de negócio é propositalmente mínimo; o foco é exercitar conceitos de SD (cliente-servidor, mensageria, Saga, tolerância a falhas, observabilidade, containers).

Stack principal: **.NET 10**, Minimal APIs, YARP (gateway), PostgreSQL, Redis, RabbitMQ, Keycloak (OAuth 2.0/OIDC), OpenTelemetry + Prometheus + Grafana, Docker Compose.

> **Estado atual:** fase de **fundação** concluída. Há um serviço esqueleto (`Catalog`), o `Gateway` (YARP) com validação de JWT, observabilidade mínima e ambiente local reproduzível via Docker Compose. Ainda não há regras de negócio, mensageria ou Saga. Veja o relatório em [docs/experimentos/01-fundacao.md](docs/experimentos/01-fundacao.md).

## Pré-requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/) (versão fixada em `global.json`)
- [Docker](https://www.docker.com/) com Docker Compose v2 (`docker compose`)
- Para rodar os testes de integração localmente: um Docker daemon disponível (usado pelo [Testcontainers](https://testcontainers.com/))

## Como subir o ambiente local

Os artefatos de deploy ficam em `deploy/`. A infraestrutura e os serviços .NET são descritos em arquivos Compose separados:

- `deploy/docker-compose.yml` — infraestrutura: Keycloak, RabbitMQ, PostgreSQL (`catalog`), Redis, Prometheus e Grafana.
- `deploy/docker-compose.services.yml` — serviços .NET: `gateway` e `catalog` (imagens construídas a partir do código-fonte).
- `deploy/docker-compose.override.yml` — overrides de desenvolvimento que expõem portas ao host (aplicado automaticamente pelo Compose quando presente).

> O project name do Compose é `orderhub`, então os containers aparecem como `orderhub-<serviço>`.

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

### Acesso direto ao PostgreSQL (profile `debug`)

O banco não é exposto ao host fora do profile `debug`:

```bash
cd deploy
docker compose --profile debug up -d
```

### Derrubar o ambiente

```bash
cd deploy
docker compose -f docker-compose.yml -f docker-compose.services.yml down
```

### Configuração e segredos

Senhas e credenciais usam variáveis de ambiente com defaults de desenvolvimento (ex.: `changeme_dev`). **Nunca** use esses defaults fora do ambiente local. Em desenvolvimento, prefira User Secrets; no deploy, variáveis de ambiente/secrets do orquestrador.

## Serviços e portas (ambiente local)

As portas abaixo valem com o `docker-compose.override.yml` ativo.

| Serviço | URL / porta no host | Observação |
|---|---|---|
| `gateway` (YARP) | `http://localhost:5000` | Borda; valida JWT e encaminha `/catalog/**` |
| `catalog` | `http://localhost:5001` | Serviço esqueleto |
| Keycloak | `http://localhost:8080` (health em `9000`) | Realm `orderhub` importado no boot |
| RabbitMQ | AMQP `5672`, management UI `http://localhost:15672` | — |
| Redis | `localhost:6379` | — |
| Prometheus | `http://localhost:9090` | Faz scraping de `catalog` e `gateway` em `/metrics` |
| Grafana | `http://localhost:3000` | Dashboards e datasource provisionados |
| PostgreSQL (`catalog`) | `localhost:5432` | Apenas com o profile `debug` |

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
  Gateway/OrderHub.Gateway
  Catalog/OrderHub.Catalog.{Domain,Application,Infrastructure,Api}
tests/
  OrderHub.Catalog.Architecture.Tests           # regras de dependência (NetArchTest)
  catalog/OrderHub.Catalog.Api.IntegrationTests  # integração com Testcontainers
```

Convenções: cada serviço segue Clean Architecture nas quatro camadas (`Domain`, `Application`, `Infrastructure`, `Api`), com namespace `OrderHub.<Service>.<Layer>`. Um `Dockerfile` por serviço, dentro da pasta do `Api`. `ProjectReference` entre serviços não é permitido — o compartilhamento ocorre apenas em `BuildingBlocks`.

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

O workflow em `.github/workflows/ci.yml` roda a cada push em `develop`/`main` e em pull requests para `develop`. Ele executa: restore, build em `Release` (warnings como erros), testes de arquitetura e testes de integração com Testcontainers, publicando os resultados como artefato.

## Experimentos

- [Experimento 01 — Reprodutibilidade do ambiente e detecção de falha por crash](docs/experimentos/01-fundacao.md)

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

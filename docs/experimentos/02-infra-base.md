# Experimento 02: Reprodutibilidade da plataforma e detecção de falha de dependência (RabbitMQ)

- **Spec relacionada:** `.kiro/specs/02-infra-base`
- **Data da execução:** 2026-10-26 (execução local via Docker)
- **Executor:** agente Kiro, com Docker disponível (Server `29.9.1`, Compose `v5.6.0`)

## Objetivo

Verificar que a plataforma compartilhada do OrderHub (Keycloak, RabbitMQ, Redis, Prometheus,
Grafana) mais os serviços .NET (`gateway`, `catalog`) sobe com um único comando em estado
saudável e que o desligamento (crash) do container do `rabbitmq` é detectado, em tempo hábil,
por um *failure detector* — tanto no health check do próprio container quanto no
`/health/ready` do `catalog` (Requisito 7) — além de avaliar o efeito observável no Prometheus.

## Conceito de SD

- **Tópico da disciplina:** fundamentos e arquitetura de sistemas distribuídos; gerenciamento
  de componentes em containers; **detecção de falhas por crash** e **tolerância a falhas**.
- **Premissas do modelo de sistema** (conforme `requirements.md`): sistema assíncrono (sem
  limite de atraso de rede), falhas por *crash* de processo, sem partição de rede nesta fase.
- A detecção de falha é feita por *failure detector* baseado em health check periódico (polling
  do Docker a cada `interval`) somado ao *scraping* do Prometheus. Nenhum dos dois é instantâneo,
  por isso a métrica de sucesso é expressa como um limite de tempo (até 30 s).

## Hipótese

1. `docker compose ... up` sobe Keycloak, RabbitMQ, Redis, Prometheus, Grafana, `gateway` e
   `catalog` em estado saudável (`Req. 1`).
2. Ao derrubar o container `orderhub-rabbitmq`, a falha é observável em até **30 s** por meio de:
   - o estado/health check do container `rabbitmq` passar a `exited`/`unhealthy`;
   - o `/health/ready` de um serviço que dependa do RabbitMQ retornar falha (`Req. 7`);
   - algum sinal correspondente no Prometheus.

## Ambiente

Valores extraídos dos artefatos reais em `deploy/`.

| Item | Valor |
|---|---|
| Orquestração local | Docker Compose (project `orderhub`) |
| Arquivos Compose | `deploy/docker-compose.yml` (infra) + `deploy/docker-compose.services.yml` (serviços .NET) + `deploy/docker-compose.override.yml` (portas em dev) |
| Redes | `edge`, `internal`, `data` (bridge) |
| Container sob teste | `rabbitmq` (`orderhub-rabbitmq`, imagem `rabbitmq:4.0-management`) |
| Serviço observado | `catalog` (`orderhub-catalog`, imagem `orderhub/catalog:1.0.0`) |
| Portas expostas (dev) | Keycloak `8180`, RabbitMQ `15672`/`5672`, Prometheus `9090`, Grafana `3000`, Redis `6379`, `gateway` `5000`, `catalog` `5001` |
| Limites de recursos (`rabbitmq`) | `cpus: "1.0"`, `mem_limit: 512m` |
| Healthcheck (`rabbitmq`) | `rabbitmq-diagnostics -q ping`, `interval: 15s`, `timeout: 5s`, `retries: 10`, `start_period: 30s` |
| Prometheus — jobs | `prometheus` (self), `catalog` (`catalog:8080/metrics`), `gateway` (`gateway:8080/metrics`); `scrape_interval` global de 15 s |
| Réplicas | 1 por serviço (sem balanceamento nesta fase) |

Endpoints relevantes do `catalog` (ver `src/Catalog/OrderHub.Catalog.Api/Program.cs`):

- `/health/live` — processo vivo, sem checagem de dependências.
- `/health/ready` — registrado com `AddHealthChecks()` **sem nenhuma checagem de dependência
  concreta**. Na fase atual, o `catalog` ainda não tem banco próprio nem cliente de RabbitMQ
  (a implementação de `IEventBus` fica para a fase 03), portanto o `/health/ready` não conhece
  o RabbitMQ.

> **Observação importante sobre o modelo de dependência:** nesta fase o `catalog` **não declara
> o RabbitMQ como dependência** — nem em `depends_on` no Compose, nem como *health check* dentro
> do serviço. Isso é relevante para interpretar os resultados abaixo.

## Procedimento (reprodutível)

Todos os comandos são executados a partir da pasta `deploy/`. O `docker-compose.override.yml`
expõe as portas ao host.

> **Nota operacional.** O comando documentado no `requirements.md`
> (`docker compose -f docker-compose.yml -f docker-compose.services.yml up`) **não publica as
> portas de host**, porque omite o `docker-compose.override.yml` (onde ficam os mapeamentos de
> porta). Para alcançar as UIs e os endpoints pelo host, rode com os três arquivos `-f` ou rode
> `docker compose up` "pelado" dentro de `deploy/` (o override de nome padrão é incluído
> automaticamente, mas aí o `docker-compose.services.yml` precisa ser adicionado explicitamente).

### 1. Subir a plataforma completa

```bash
cd deploy
docker compose -f docker-compose.yml -f docker-compose.services.yml -f docker-compose.override.yml up -d --build
```

Aguardar todos os containers ficarem saudáveis e conferir manualmente:

```bash
docker ps -a --filter name=orderhub- --format "{{.Names}} :: {{.Status}}"
curl -i http://localhost:5001/health/ready         # Catalog
```

Conferir as UIs (via portas do override): RabbitMQ `http://localhost:15672`,
Grafana `http://localhost:3000`, Prometheus `http://localhost:9090`.

### 2. Marcar o instante da falha e derrubar o RabbitMQ

```bash
# Instante t0 (referência para a métrica de 30 s)
date -u +%Y-%m-%dT%H:%M:%S.%3NZ

# Crash abrupto do broker (SIGKILL)
docker kill orderhub-rabbitmq
```

### 3. Observar a detecção da falha (dentro de 30 s)

a) **Estado/health check do container:**

```bash
docker inspect -f '{{.State.Status}} {{if .State.Health}}{{.State.Health.Status}}{{end}}' orderhub-rabbitmq
```

b) **`/health/ready` do Catalog** (polling a cada 5 s):

```bash
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5001/health/ready
```

c) **Prometheus** — série `up` dos alvos:

```promql
up
```

### 4. Restaurar o ambiente

```bash
docker start orderhub-rabbitmq
# ou derrubar tudo preservando volumes nomeados:
docker compose -f docker-compose.yml -f docker-compose.services.yml -f docker-compose.override.yml down
```

## Métrica de sucesso

Todos os serviços `healthy` após o boot; falha de dependência detectada em até **30 s** do `t0`.

## Resultados

Execução de **2026-10-26** (horários em `America/Sao_Paulo`, fuso `-03:00`). Dados extraídos do
log bruto do experimento (`deploy/exp-run.out`). **Nenhum valor foi inventado.**

### Boot

Antes do `t0`, os sete containers estavam `running (healthy)`:

```
orderhub-catalog     running  Up (healthy)
orderhub-gateway     running  Up (healthy)
orderhub-grafana     running  Up (healthy)
orderhub-keycloak    running  Up (healthy)
orderhub-prometheus  running  Up (healthy)
orderhub-rabbitmq    running  Up (healthy)
orderhub-redis       running  Up (healthy)
```

`/health/ready` e `/health/live` do `catalog`: **HTTP 200 `Healthy`**.

### Linha do tempo da falha

`t0` = instante do `docker kill orderhub-rabbitmq` (log: `22:31:40.29`).

| Sinal observado | Valor esperado | Valor medido | Tempo até detecção |
|---|---|---|---|
| Estado do container `rabbitmq` | `exited` | `exited` (saída `137`, SIGKILL) | **≤ 1 s** (já `exited` em `t+1s`) |
| Health check do container `rabbitmq` | `unhealthy` | `unhealthy` | **≤ 1 s** (já `unhealthy` em `t+1s`) |
| `/health/ready` do `catalog` | falha (`Req. 7`) | **HTTP 200 `Healthy`** durante toda a janela de 90 s | **não detectou** |
| Prometheus `up{job="prometheus"}` | `1` | `1` (antes e depois) | sem mudança |
| Prometheus `up{job="catalog"}` / `up{job="gateway"}` | — | `0` antes e depois (ver ressalva) | sem mudança atribuível ao RabbitMQ |

Amostragem do `/health/ready` do `catalog` (a cada ~5 s) ao longo dos 90 s posteriores ao `t0`:
**todas** retornaram `HTTP=200 body=Healthy` (de `t+1s` a `t+89s`). O container `rabbitmq`
permaneceu `exited`/`unhealthy` em todas as amostras.

Após o experimento, o broker foi reiniciado com `docker start orderhub-rabbitmq` para deixar a
plataforma consistente.

## Análise

- **Reprodutibilidade (parte 1 da hipótese): confirmada.** Um único comando sobe a plataforma e
  os sete containers ficam `healthy`. O desligamento do RabbitMQ é detectado **quase
  instantaneamente** (≤ 1 s) pelo *health check* do próprio container, bem dentro do limite de
  30 s. Nesse nível (orquestrador), a métrica de sucesso é atendida.

- **`/health/ready` do Catalog (parte 2 / `Req. 7`): NÃO confirmada na implementação atual.**
  O `/health/ready` do `catalog` continuou respondendo `Healthy` com o RabbitMQ derrubado. A
  causa é direta e esperada para esta fase: o `catalog` registra `AddHealthChecks()` **sem
  nenhuma checagem de dependência**, e o RabbitMQ **não é dependência** do `catalog` nesta fase
  (não há consumo/publicação de eventos; `IEventBus` só chega na fase 03 — ver `design.md` e
  ADR 0003). Ou seja, do ponto de vista do `catalog`, o RabbitMQ não é "dependência essencial",
  então o comportamento observado é **correto para o estado atual do código**, mas o Requisito 7
  **ainda não está exercitável** com o RabbitMQ — faltam serviços que de fato dependam do broker
  e que registrem essa dependência no `/health/ready`.

- **Prometheus:** o RabbitMQ **não é alvo de scraping** (não há exporter nem job `rabbitmq` em
  `prometheus.yml`), então derrubá-lo não produz transição direta de série `up`. A única série
  estável é `up{job="prometheus"}=1`.

- **Ressalva pré-existente (fora do escopo desta fase):** os alvos `catalog` e `gateway` já
  estavam com `up=0` **antes** do experimento, porque os endpoints `/metrics` desses serviços
  retornam 404 na fase atual (responsabilidade da fase 01). Essa condição não foi causada pelo
  experimento e não deve ser confundida com o efeito do crash do RabbitMQ.

## Conclusão

A hipótese é **parcialmente confirmada**:

1. **Reprodutibilidade e detecção de crash pelo orquestrador — confirmadas.** A plataforma sobe
   com um comando, todos os serviços ficam `healthy`, e o crash do RabbitMQ é detectado em ≤ 1 s
   pelo health check do container, bem abaixo do limite de 30 s.
2. **Falha de dependência refletida em `/health/ready` (`Req. 7`) — não demonstrada.** Com o
   código atual, nenhum serviço declara o RabbitMQ como dependência essencial, então o
   `/health/ready` do `catalog` não muda ao derrubar o broker. Esse é o comportamento real e
   correto para esta fase, mas significa que o `Req. 7` só poderá ser efetivamente exercitado
   com o RabbitMQ quando houver um serviço que dependa do broker e inclua essa checagem no
   `/health/ready` (fase 03 em diante).

**Encaminhamentos sugeridos (fora do escopo desta fase):**

- Fase 03: ao introduzir a implementação de `IEventBus` sobre RabbitMQ em um serviço consumidor,
  registrar uma checagem de readiness da conexão com o broker (tag `ready`) e repetir este
  experimento para fechar o `Req. 7` com o RabbitMQ.
- Fase 01 (pendência conhecida): corrigir o endpoint `/metrics` do `catalog` e do `gateway` para
  que os alvos do Prometheus fiquem `up=1`.
- Opcional: adicionar um exporter/job de RabbitMQ no Prometheus para tornar a queda do broker
  diretamente observável nas métricas.

## Artefatos

- Log bruto do experimento: `deploy/exp-run.out` (timestamps de cada amostra).
- Scripts de execução: `deploy/exp-up.ps1` (boot) e `deploy/exp-run.ps1` (kill + polling).

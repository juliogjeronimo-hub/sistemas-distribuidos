# Experimento 01: Reprodutibilidade do ambiente e detecção de falha por crash

- **Spec relacionada:** `.kiro/specs/01-foundation`
- **Data:** _a preencher no dia da execução local_

## Objetivo

Verificar que o ambiente completo do OrderHub é reproduzível com um único comando e que
o desligamento (crash) do container do `catalog` é refletido, em tempo hábil, no health
check `/health/ready`, nas métricas do Prometheus e no log do `gateway`.

## Conceito de SD

- **Tópico da disciplina:** fundamentos e arquitetura de sistemas distribuídos; gerenciamento
  de componentes e processos em containers; **detecção de falhas por crash**.
- **Premissas do modelo de sistema** (conforme `requirements.md`): sistema assíncrono (sem
  limite de atraso de rede), falhas por *crash* de processos, sem partições de rede nesta fase.
- A detecção de falha é feita por *failure detector* baseado em health check periódico
  (polling do Compose a cada `interval`) somado ao *scraping* do Prometheus. Nenhum dos dois
  é instantâneo, por isso a métrica de sucesso é expressa como um limite de tempo (até 30 s).

## Hipótese

1. `docker compose ... up` sobe Keycloak, RabbitMQ, PostgreSQL, Redis, Prometheus, Grafana,
   `gateway` e `catalog` em estado saudável (`Req. 1`).
2. Ao derrubar o container `orderhub-catalog`, a falha é observável em até **30 s** por meio de:
   - `/health/ready` do `catalog` deixa de responder (ou o estado do container vira `unhealthy`);
   - a série `up{job="catalog"}` no Prometheus passa de `1` para `0`;
   - o `gateway` responde `503` nas rotas `/catalog/**` e o log estruturado registra o erro
     com o `CorrelationId` da requisição (`Req. 7`, e o timeout de 5 s do proxy descrito no `design.md`).

## Ambiente

Valores extraídos dos artefatos reais em `deploy/`.

| Item | Valor |
|---|---|
| Orquestração local | Docker Compose (project `orderhub`) |
| Arquivos Compose | `deploy/docker-compose.yml` (infra) + `deploy/docker-compose.services.yml` (serviços .NET) + `deploy/docker-compose.override.yml` (portas em dev) |
| Redes | `edge`, `internal`, `data` (bridge) |
| Serviço sob teste | `catalog` (container `orderhub-catalog`, imagem `orderhub/catalog:1.0.0`) |
| Gateway | `gateway` (container `orderhub-gateway`, imagem `orderhub/gateway:1.0.0`, YARP) |
| Portas expostas (dev) | `gateway` `5000:8080`, `catalog` `5001:8080`, Prometheus `9090:9090`, Grafana `3000:3000` |
| Limites de recursos (`catalog`) | `cpus: "0.5"`, `mem_limit: 256m` |
| Healthcheck (`catalog` e `gateway`) | `curl -f http://localhost:8080/health/ready`, `interval: 15s`, `timeout: 5s`, `retries: 5`, `start_period: 20s` |
| Timeout do proxy (Gateway) | 5 s (ver `design.md`) |
| Prometheus — job | `catalog` em `catalog:8080`, `metrics_path: /metrics`, `scrape_interval` global de 15 s |
| Réplicas | 1 por serviço (sem balanceamento nesta fase) |

Endpoints relevantes do `catalog` (ver `src/Catalog/OrderHub.Catalog.Api/Program.cs`):

- `/health/live` — processo vivo, sem checagem de dependências.
- `/health/ready` — agrega checagens marcadas com a tag `ready` (conectividade com o PostgreSQL via `AddNpgSql`).
- `/metrics` — endpoint Prometheus (OpenTelemetry).

## Procedimento (reprodutível)

Todos os comandos são executados a partir da pasta `deploy/`. O `docker-compose.override.yml`
é aplicado automaticamente pelo Compose quando presente no mesmo diretório, expondo as portas
ao host.

### 1. Subir o ambiente completo

```bash
cd deploy
docker compose -f docker-compose.yml -f docker-compose.services.yml up -d --build
```

Aguardar todos os containers ficarem saudáveis:

```bash
docker compose -f docker-compose.yml -f docker-compose.services.yml ps
```

Confirmar manualmente os health checks (via portas expostas pelo override):

```bash
# Catalog pronto (espera-se HTTP 200)
curl -i http://localhost:5001/health/ready

# Gateway pronto (espera-se HTTP 200)
curl -i http://localhost:5000/health/ready
```

Confirmar no Prometheus que o alvo `catalog` está UP (deve retornar valor `1`):

- Abrir `http://localhost:9090` → *Status* → *Targets* (o job `catalog` deve estar `UP`), ou
- Executar a query na aba *Graph*:

  ```promql
  up{job="catalog"}
  ```

### 2. Marcar o instante da falha e derrubar o Catalog

Registrar o horário (fonte do cronômetro de 30 s) e parar o container:

```bash
# Instante t0 (referência para a métrica de 30 s)
date -u +%Y-%m-%dT%H:%M:%S.%3NZ

# Derruba o processo do Catalog (crash simulado)
docker compose -f docker-compose.yml -f docker-compose.services.yml stop catalog
```

> Alternativa mais abrupta (encerramento imediato, sem shutdown gracioso):
> `docker kill orderhub-catalog`

### 3. Observar a detecção da falha (dentro de 30 s)

a) **Health check / estado do container.** O `/health/ready` do `catalog` deixa de responder.
O Compose marca o container como `exited`/`unhealthy`:

```bash
docker compose -f docker-compose.yml -f docker-compose.services.yml ps
# e, para acompanhar em tempo real:
docker inspect -f '{{.State.Status}} {{.State.Health.Status}}' orderhub-catalog
```

b) **Prometheus.** A série deve transitar de `1` para `0` dentro de um ou dois intervalos de
scraping (15 s cada):

```promql
up{job="catalog"}
```

Para medir o atraso de detecção pelo próprio Prometheus:

```promql
time() - timestamp(up{job="catalog"} == 1)
```

c) **Gateway.** Fazer uma requisição a uma rota que depende do `catalog` e observar o `503`:

```bash
curl -i http://localhost:5000/catalog/v1/ping
```

Acompanhar o log estruturado do `gateway`, que deve registrar a falha de upstream com o
`CorrelationId` (`X-Correlation-Id`) e o `TraceId`:

```bash
docker compose -f docker-compose.yml -f docker-compose.services.yml logs -f gateway
```

### 4. Restaurar o ambiente

```bash
docker compose -f docker-compose.yml -f docker-compose.services.yml start catalog
# ou derrubar tudo:
docker compose -f docker-compose.yml -f docker-compose.services.yml down
```

## Métrica de sucesso

A falha do `catalog` deve ser detectada em **até 30 s** após o `t0` (passo 2), observável em
pelo menos um dos sinais (idealmente nos três): `/health/ready` indisponível, `up{job="catalog"}`
em `0` no Prometheus, e `503` + log com `CorrelationId` no `gateway`.

O limite de 30 s é compatível com os parâmetros configurados: `scrape_interval` de 15 s no
Prometheus e `interval: 15s` com `timeout: 5s` nos health checks do Compose. O pior caso de
detecção pelo scraping fica em torno de 2 × 15 s, portanto a confirmação deve ser feita com a
query de atraso acima para registrar o tempo real medido.

## Resultados

> **Estado desta execução:** o ambiente de execução deste agente **não possui o Docker CLI
> disponível** (`docker` não reconhecido). Portanto, o experimento **não foi executado de fato**
> aqui, e os campos numéricos abaixo são marcados como placeholders a serem preenchidos após a
> execução local, seguindo o procedimento acima. Nenhum valor foi inventado.

| Sinal observado | Valor esperado | Valor medido | Tempo até detecção |
|---|---|---|---|
| `/health/ready` do `catalog` | indisponível / `unhealthy` | _a preencher após execução local_ | _a preencher_ |
| `up{job="catalog"}` (Prometheus) | `1` → `0` | _a preencher após execução local_ | _a preencher_ |
| Resposta do `gateway` em `/catalog/**` | `503` | _a preencher após execução local_ | _a preencher_ |
| Log do `gateway` com `CorrelationId` | presente | _a preencher após execução local_ | _a preencher_ |

Artefatos a anexar na execução local (sugestão):

- Saída de `docker compose ps` antes e depois do `stop`.
- Captura de tela do Prometheus (Targets e gráfico de `up{job="catalog"}`).
- Trecho do log JSON do `gateway` mostrando o `503` com `CorrelationId` e `TraceId`.

## Análise

_A preencher após execução local._ Pontos a discutir:

- Tempo de detecção real × limite de 30 s; qual sinal detectou primeiro (proxy do Gateway com
  timeout de 5 s tende a ser mais rápido que o scraping de 15 s do Prometheus).
- Efeito do `restart: unless-stopped`: com `stop` o container permanece parado; com `docker kill`
  o Compose tentaria reiniciar, o que muda a janela de observação.
- Diferença entre falha detectada pelo health check local (dependência PostgreSQL via
  `/health/ready`) e falha detectada externamente (crash do processo observado pelo Prometheus/Gateway).

## Conclusão

_A preencher após execução local._ A hipótese é considerada confirmada se os três sinais
refletirem a queda do `catalog` dentro de 30 s do `t0`, demonstrando (1) reprodutibilidade do
ambiente com um único comando e (2) detecção de falha por crash pela malha de observabilidade.

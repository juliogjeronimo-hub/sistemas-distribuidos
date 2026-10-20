---
inclusion: always
---
# Observabilidade

- Logs estruturados (JSON) com `CorrelationId`, `TraceId`, nome do serviço e versão.
- `CorrelationId` entra pelo Gateway (`X-Correlation-Id`), é propagado em HTTP, gRPC e headers de mensagens RabbitMQ.
- OpenTelemetry para traces e métricas; exportação para Prometheus; dashboards no Grafana.
- Métricas mínimas por serviço: taxa de requisições, latência (p50/p95/p99), erros, mensagens consumidas/rejeitadas, tamanho do Outbox pendente, profundidade das filas.
- Endpoints `/health/live` (processo vivo) e `/health/ready` (dependências ok).
- Nunca registrar tokens, senhas ou dados pessoais em logs.

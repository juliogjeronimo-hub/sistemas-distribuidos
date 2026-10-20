---
inclusion: fileMatch
fileMatchPattern: ["**/*Client*.cs", "**/*Http*.cs", "**/*Grpc*.cs", "**/Resilience/**"]
---
# Resiliência em chamadas remotas

- Toda chamada remota tem **timeout explícito** e `CancellationToken`.
- Use `Microsoft.Extensions.Http.Resilience` com pipeline: timeout total → retry → circuit breaker → timeout por tentativa.
- **Retry:** backoff exponencial com *jitter*, no máximo 3 tentativas, **apenas para operações idempotentes** (GET, PUT, ou POST com `Idempotency-Key`).
- **Circuit breaker:** abrir após taxa de falhas definida na spec; registrar mudanças de estado em log e métrica.
- **Bulkhead:** limitar concorrência por dependência.
- **Fallback:** somente quando fizer sentido de negócio (ex.: cache do Catalog). Nunca esconder falha silenciosamente.
- Operações de escrita expostas via POST aceitam header `Idempotency-Key` e retornam a mesma resposta em repetições.
- Cada serviço expõe `/health/live` e `/health/ready`; `ready` falha se dependência essencial estiver indisponível.
- Documentar o modelo de falhas assumido (crash, omissão, partição) na spec.

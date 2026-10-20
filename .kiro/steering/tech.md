---
inclusion: always
---
# Stack e decisões técnicas

| Área | Decisão |
|---|---|
| Plataforma | .NET 10 (LTS), C# com `Nullable` habilitado |
| API | Minimal APIs, um projeto `Api` por serviço |
| Gateway | YARP |
| Persistência | PostgreSQL (um banco por serviço) + EF Core (Npgsql); Dapper permitido em consultas de leitura pesadas |
| Cache | Redis |
| Mensageria | RabbitMQ via `RabbitMQ.Client`, encapsulado em `IEventBus` (sem MassTransit) |
| Casos de uso | Handlers próprios (`ICommandHandler`/`IQueryHandler`), **sem biblioteca de mediator**; decorators via Scrutor |
| Validação | FluentValidation |
| Falhas de negócio | Result pattern próprio, sem exceptions para fluxo normal |
| Identidade | Keycloak (OAuth 2.0 / OIDC); APIs são *resource servers* |
| Resiliência | `Microsoft.Extensions.Http.Resilience` (Polly) |
| Observabilidade | OpenTelemetry, Prometheus, Grafana |
| Arquivos | MinIO (API compatível com S3) |
| Containers | Docker, Docker Compose (local), Docker Swarm (VMs) |
| Testes | xUnit, Testcontainers, NetArchTest, k6 (carga) |
| CI | GitHub Actions |

## Decisões estruturais

- Microsserviços em **monorepo**.
- **Cada serviço tem seu próprio banco**; nenhum serviço lê o banco de outro.
- Clean Architecture **dentro de cada serviço**.
- Compartilhamento entre serviços **somente** em `BuildingBlocks` (contratos de eventos e utilitários técnicos). Nunca compartilhar entidades de domínio.
- Comunicação síncrona: HTTP/REST e gRPC (somente onde a spec pedir). Assíncrona: RabbitMQ.
- Segredos nunca no código nem em imagens: User Secrets em dev, variáveis de ambiente/secrets no deploy.

---
inclusion: fileMatch
fileMatchPattern: ["**/Dockerfile*", "**/docker-compose*.yml", "**/docker-compose*.yaml", "deploy/**"]
---
# Docker

## Dockerfile

- Multi-stage: `sdk` para build/publish, `aspnet` (runtime) para a imagem final.
- Executar como usuário **não-root**.
- `HEALTHCHECK` ou verificação equivalente no compose usando `/health/ready`.
- Um `.dockerignore` por contexto de build.
- Sem segredos na imagem. Configuração por variáveis de ambiente.
- Tag de imagem segue SemVer do serviço: `orderhub/orders:1.3.0` (nunca depender de `latest`).

## Compose (`deploy/`)

- `docker-compose.yml` base (infra: Keycloak, RabbitMQ, PostgreSQL por serviço, Redis, MinIO, Prometheus, Grafana) e `docker-compose.services.yml` (serviços .NET).
- Redes separadas: `edge` (gateway), `internal` (serviços), `data` (bancos). Bancos não expostos ao host fora de profile `debug`.
- Limites de recursos (`cpus`, `mem_limit`) declarados por serviço. Servem ao laboratório de cgroups.
- Réplicas via `deploy.replicas` (Swarm) ou `--scale` (Compose), atrás do balanceador.
- `depends_on` com `condition: service_healthy`.
- Volumes nomeados para dados persistentes.
- Deploy em VMs: Docker Swarm (stack file em `deploy/swarm/`).

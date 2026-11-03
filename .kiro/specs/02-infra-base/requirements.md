# Requisitos: Infraestrutura base (plataforma compartilhada)

## Contexto
A fase 01 entregou Catalog, Gateway e Keycloak, mas a plataforma de apoio compartilhada está incompleta: o `docker-compose.override.yml` expõe portas de serviços (`rabbitmq`, `redis`, `prometheus`, `grafana`) que nenhum compose base define — overrides sem efeito. Esta fase completa e corrige a plataforma para que as fases seguintes apenas acrescentem serviços de negócio sobre uma base estável.

Escopo restrito à **plataforma compartilhada**: broker, cache, identidade, observabilidade, redes, volumes e os projetos `BuildingBlocks`. O banco de cada serviço, o MinIO e os esqueletos dos demais serviços são criados nas fases respectivas, não aqui.

## Conceito de SD exercitado
- **Tópico da disciplina:** fundamentos e arquitetura (física e lógica); suporte de SO (limites de CPU/memória por container, redes isoladas); gerenciamento de componentes em containers.
- **Premissas do modelo de sistema:** sistema assíncrono (sem limite de atraso de rede); falhas por *crash* de processo; sem partição de rede nesta fase.

## Requisitos (EARS)
1. QUANDO o desenvolvedor executar `docker compose -f docker-compose.yml -f docker-compose.services.yml up`, O SISTEMA DEVE subir Keycloak, RabbitMQ, Redis, Prometheus, Grafana, Gateway e Catalog em estado saudável.
2. ENQUANTO a plataforma estiver em execução, O SISTEMA DEVE isolar o tráfego em três redes (`edge`, `internal`, `data`), expondo ao host apenas o necessário para desenvolvimento.
3. QUANDO um container de infra com estado for reiniciado, O SISTEMA DEVE preservar seus dados por meio de volumes nomeados.
4. ENQUANTO um container estiver em execução, O SISTEMA DEVE respeitar os limites de `cpus` e `mem_limit` declarados para ele.
5. QUANDO o Prometheus iniciar, O SISTEMA DEVE coletar métricas dos alvos declarados, e o Grafana DEVE ter datasource e dashboard provisionados automaticamente.
6. QUANDO um pull request para `develop` for aberto, O SISTEMA DEVE validar a composição do Docker (`docker compose config`) além de build, testes e arquitetura.
7. SE uma dependência essencial de um serviço estiver indisponível, ENTÃO `/health/ready` desse serviço DEVE retornar falha.
8. QUANDO a solution for compilada, O SISTEMA DEVE incluir os projetos `OrderHub.BuildingBlocks.Contracts` e `OrderHub.BuildingBlocks.Messaging` com as abstrações de envelope de evento e `IEventBus`, sem implementação concreta de broker.

## Critérios de aceite
- [ ] `docker compose -f docker-compose.yml config` lista Keycloak, RabbitMQ, Redis, Prometheus e Grafana, todos com healthcheck e limites de recurso.
- [ ] Redes `edge`, `internal` e `data` declaradas no compose base; bancos e broker não expostos ao host fora do profile `debug`.
- [ ] Volumes nomeados para RabbitMQ e Grafana; dados sobrevivem a `docker compose restart`.
- [ ] RabbitMQ management UI, Grafana e Prometheus acessíveis em dev pelas portas do override.
- [ ] `docker-compose.override.yml` sem overrides órfãos (todo serviço com porta exposta existe no base).
- [ ] Projetos `BuildingBlocks` na solution, compilando, com teste de arquitetura garantindo que não dependem de serviço algum.
- [ ] Job de CI de `docker compose config` verde.

## Evidência / experimento
- **Hipótese:** a plataforma inteira é reproduzível com um comando e o desligamento de um container é detectado em health check e métricas em até 30 s.
- **Procedimento:** subir a plataforma; conferir RabbitMQ UI, Grafana e Prometheus; derrubar o container do RabbitMQ; observar `/health/ready` do Catalog e as métricas no Prometheus.
- **Métrica de sucesso:** todos os serviços `healthy` após o boot; falha de dependência detectada em até 30 s.
- **Relatório:** `docs/experimentos/02-infra-base.md`

## Fora de escopo
- Postgres de cada serviço (criado na fase do serviço).
- MinIO (fase 09).
- Esqueletos de Inventory, Orders, Payments, Notifications (cada um na sua fase).
- Implementação RabbitMQ de `IEventBus` e topologia de exchanges/filas (fase 03).

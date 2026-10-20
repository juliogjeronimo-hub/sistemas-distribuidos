# Mapa: tópicos da disciplina → projeto

## Exercitados no sistema

| Tópico | Como é exercitado | Fase |
|---|---|---|
| Fundamentos e arquitetura de SD | ADRs, modelo de sistema (falhas, tempo), diagramas C4 | 1 |
| Processos e threads | `async/await`, `BackgroundService`, container como processo isolado | 3, 8 |
| Modelos de computação distribuída | Comparação síncrono × assíncrono | 3 |
| Troca de mensagens | RabbitMQ: ack, DLQ, semânticas de entrega | 3 |
| Cliente-servidor | Gateway + REST, timeouts | 2 |
| Objetos distribuídos e invocação remota | gRPC × REST | 7 |
| Suporte de SO | Limites de CPU/memória (cgroups) em containers | 8 |
| Consistência e replicação | Réplica de leitura, cache, consistência eventual | 5 |
| Segurança | Keycloak, OAuth 2.0, TLS/mTLS | 2 |
| Tolerância a falhas | Retry, circuit breaker, testes de caos | 4, 6 |
| Transações distribuídas e concorrência | Saga, Outbox, concorrência otimista | 4 |
| Middleware | RabbitMQ, gateway | 3 |
| Alta disponibilidade e balanceamento | Réplicas, failover, balanceador | 6 |
| VMs e gerenciamento | Swarm em VMs, Prometheus, Grafana, k6 | 1, 6, 10 |

## Parcialmente no sistema

| Tópico | Cobertura | Complemento |
|---|---|---|
| Comunicação em grupo | Pub/sub | Lab de multicast ordenado |
| Arquivos distribuídos | MinIO | Lab mini-DFS |
| Sincronização | Lock distribuído | Lab de relógios e eleição |

## Laboratórios (`labs/`)

| Tópico | Laboratório |
|---|---|
| Peer-to-peer | Gossip de descoberta de membros |
| Comunicação em grupo | Multicast com ordenação FIFO/causal |
| IPC de baixo nível | Sockets TCP/UDP, JSON × Protobuf |
| Sincronização | Lamport, vetorial, Bully/Ring, Cristian/Berkeley |
| Arquivos distribuídos | Mini-DFS com replicação de blocos |
| Grades computacionais | Coordenador + workers (Monte Carlo) |
| Invocação remota | Mini-RPC (stub/skeleton sobre sockets) |

# Roadmap

Cada fase corresponde a uma pasta em `.kiro/specs/`. Toda spec tem as seções **Conceito de SD exercitado** e **Evidência / experimento**.

| # | Spec | Entregas | Conceito | Evidência |
|---|---|---|---|---|
| 1 | `01-foundation` | Solution, Compose, Keycloak, Gateway, CI, observabilidade | Arquitetura de SD, gerenciamento | Ambiente sobe com um comando; dashboards ativos |
| 2 | `02-catalog-auth` | Catalog com OAuth, policies, escopos | Cliente-servidor, segurança | Testes 401/403/200; relatório de modelo de ameaças |
| 3 | `03-messaging` | Orders → Inventory, Outbox/Inbox, DLQ | Troca de mensagens, middleware | Duplicar mensagem e mostrar efeito único |
| 4 | `04-order-saga` | Saga com Payments e compensação | Transações distribuídas, tolerância a falhas | Derrubar Payments no meio do fluxo; sem estoque preso |
| 5 | `05-replication-consistency` | Réplica PostgreSQL, cache, read models | Replicação, consistência | Medir janela de inconsistência |
| 6 | `06-high-availability` | Réplicas, balanceador, failover, carga e caos | Alta disponibilidade, balanceamento | k6 antes/depois; derrubar réplica |
| 7 | `07-grpc` | gRPC entre dois serviços | Invocação remota | Comparativo de latência e payload vs REST |
| 8 | `08-sync-and-os` | Lamport nos eventos, lock distribuído, cgroups | Sincronização, suporte de SO | Ordenação de eventos; efeito de limites de CPU |
| 9 | `09-distributed-files` | MinIO | Arquivos distribuídos | Upload/download com nó indisponível |
| 10 | `10-vms-swarm` | Deploy em 2–3 VMs com Swarm | Arquitetura física | Failover entre nós |
| 11 | `labs/*` | Laboratórios avulsos | P2P, grupo, IPC, grades | Relatórios curtos |

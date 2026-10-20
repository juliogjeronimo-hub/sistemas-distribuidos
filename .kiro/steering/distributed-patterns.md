---
inclusion: manual
---
# Padrões de sistemas distribuídos (referência)

Incluir no chat com `#distributed-patterns.md` ao projetar fluxos distribuídos.

| Padrão | Uso no OrderHub | Conceito da disciplina |
|---|---|---|
| Transactional Outbox | Toda publicação de evento | Troca de mensagens, confiabilidade |
| Inbox / consumer idempotente | Todo consumer | Semânticas de entrega |
| Saga orquestrada | `Orders` coordena Inventory e Payments; estado persistido; compensações explícitas | Transações distribuídas |
| Controle de concorrência otimista | `rowversion`/`xmin` em Inventory | Controle de concorrência |
| Lock distribuído (Redis) | Seções críticas pontuais, com TTL e *fencing token* | Sincronização, exclusão mútua |
| Réplica de leitura + cache | Catalog | Replicação, consistência |
| Consistência eventual com read models | Consultas agregadas | Consistência |
| Retry + Circuit Breaker + Timeout | Chamadas remotas | Tolerância a falhas |
| Pub/sub (topic exchange) | Notifications | Comunicação em grupo |
| Competing consumers | Workers | Middleware, balanceamento |
| API Gateway | Entrada única, validação de token, balanceamento | Cliente-servidor |
| Relógio lógico (Lamport) | Envelope de eventos (spec dedicada) | Sincronização |

## Decisões

- **Evitar 2PC/transações XA.** Justificar na spec e preferir Saga.
- A Saga não é ACID: documentar estados intermediários visíveis e invariantes ao final.
- Toda Saga tem diagrama de estados, timeouts e caminho de compensação testado.

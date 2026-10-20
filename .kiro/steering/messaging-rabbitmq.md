---
inclusion: fileMatch
fileMatchPattern: ["**/Messaging/**", "**/*Consumer*.cs", "**/*Publisher*.cs", "**/*Outbox*.cs", "**/*Inbox*.cs", "src/building-blocks/**"]
---
# Mensageria com RabbitMQ

## Nomenclatura

- Exchange de eventos: `orderhub.events` (tipo `topic`, durável).
- Routing key: `<service>.<event-kebab>.v<N>` → `orders.order-placed.v1`.
- Fila do consumidor: `<consumer>.<event-kebab>.v<N>` → `inventory.order-placed.v1`.
- Dead-letter: `<fila>.dlq`.
- Eventos: passado, sufixo de versão: `OrderPlacedV1`. Comandos entre serviços usam sufixo `Command` e fila dedicada.

## Envelope obrigatório (headers/propriedades)

`MessageId` (Guid), `OccurredAt` (UTC), `CorrelationId`, `CausationId`, `Type`, `Version`, `Producer`, e (quando habilitado pela spec) `LamportTimestamp`.

## Garantias

- **Publicação:** somente via **Outbox** (tabela `outbox_messages` gravada na mesma transação do agregado). Publisher é um `BackgroundService` com *publisher confirms*, lendo com `FOR UPDATE SKIP LOCKED`.
- **Consumo:** semântica *at-least-once* → **consumer idempotente** via **Inbox** (`inbox_messages` com `MessageId` + `Consumer`).
- Ack manual, após commit do efeito e do registro no Inbox. `prefetch` configurado por consumer.
- Filas *quorum* para dados que não podem se perder. Mensagens persistentes.
- Falha transitória: reprocessar até 3 vezes com backoff; depois, DLQ. Falha de negócio permanente vai direto à DLQ com motivo.

## Evolução de contratos

- Em `V1`: só adicionar campos **opcionais**. Mudança incompatível cria `V2` e ambas convivem até migrar os consumidores.
- Contratos ficam em `OrderHub.BuildingBlocks.Contracts` (records imutáveis, sem lógica).
- Consumers ignoram campos desconhecidos.

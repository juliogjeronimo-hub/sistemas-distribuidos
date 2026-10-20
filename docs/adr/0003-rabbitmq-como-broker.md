# ADR 0003: RabbitMQ como broker de mensagens

- **Status:** Aceito
- **Data:** 2026-10-19

## Contexto
Comunicação assíncrona entre serviços exige um middleware orientado a mensagens.

## Decisão
Adotar RabbitMQ com exchange topic, filas quorum, DLQ, Outbox e Inbox, usando RabbitMQ.Client diretamente.

## Consequências
Simples de operar e didático; sem replay nativo de eventos como o Kafka. Abstração própria evita dependência de bibliotecas com licença comercial.

## Conceito da disciplina relacionado
Troca de mensagens; middleware; semânticas de entrega.

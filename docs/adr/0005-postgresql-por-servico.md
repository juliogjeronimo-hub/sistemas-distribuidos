# ADR 0005: PostgreSQL, um banco por serviço

- **Status:** Aceito
- **Data:** 2026-10-19

## Contexto
Serviços devem ter autonomia de dados e evoluir independentemente.

## Decisão
PostgreSQL com EF Core; cada serviço com seu banco; nenhum acesso cruzado. Dapper permitido em leituras pesadas.

## Consequências
Sem joins entre serviços; consistência entre serviços é eventual, via eventos e Saga.

## Conceito da disciplina relacionado
Consistência e replicação; transações distribuídas.

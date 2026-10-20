# ADR 0001: Microsserviços em monorepo

- **Status:** Aceito
- **Data:** 2026-10-19

## Contexto
O projeto precisa exercitar conceitos de sistemas distribuídos em múltiplos processos independentes.

## Decisão
Adotar microsserviços (Catalog, Inventory, Orders, Payments, Notifications) em um único repositório, com Clean Architecture dentro de cada serviço e um banco por serviço.

## Consequências
Mais complexidade operacional que um monolito, em troca de fronteiras reais, falhas parciais e comunicação remota para estudar. O monorepo simplifica CI, steerings e contratos compartilhados.

## Conceito da disciplina relacionado
Arquitetura de sistemas distribuídos; modelo de falhas.

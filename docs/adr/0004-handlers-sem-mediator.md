# ADR 0004: Casos de uso com handlers próprios, sem mediator

- **Status:** Aceito
- **Data:** 2026-10-19

## Contexto
É preciso despachar commands e queries sem acoplar endpoints à implementação.

## Decisão
Handlers injetados diretamente (ICommandHandler/IQueryHandler), com decorators (Scrutor) para validação, log e métricas.

## Consequências
Código rastreável e sem dependência externa; um pouco mais de registro manual no composition root. Migrar para mediator depois é simples.

## Conceito da disciplina relacionado
Modelos de computação; separação de responsabilidades.

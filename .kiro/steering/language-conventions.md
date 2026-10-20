---
inclusion: always
---
# Convenções de idioma

## Em inglês
- Todo código: classes, métodos, variáveis, namespaces, nomes de tabelas e colunas.
- Comentários e XML docs no código.
- Mensagens de log e de exceptions.
- Nomes de eventos, filas, exchanges, rotas e headers.
- Nomes de branches (`feature/add-order-saga`).

## Em português
- Mensagens de commit (descrição).
- README, docs, ADRs, specs (`requirements.md`, `design.md`, `tasks.md`), relatórios de experimento e changelog.
- Respostas do agente ao usuário.

## Mensagens de API ao cliente
- `ProblemDetails.Title` e `Detail` em inglês (são contrato técnico).

## Regra de ouro
Identificadores técnicos citados em documentação em português permanecem em inglês e em `code format` (ex.: "o `OrderPlacedV1` é publicado pelo Outbox").

---
inclusion: manual
---
# ADRs e relatórios de experimento

## ADR (`docs/adr/NNNN-titulo-em-kebab.md`)

Criar um ADR para toda decisão arquitetural relevante (broker, banco, padrão de consistência, estratégia de deploy). Usar `docs/templates/adr.md`. Numeração sequencial, status `Proposto | Aceito | Substituído`. ADRs aceitos não são editados; são substituídos por outro.
Todo ADR cita o **conceito da disciplina** relacionado.

## Relatório de experimento (`docs/experimentos/NN-titulo.md`)

Usar `docs/templates/experiment-report.md`. Estrutura: objetivo, conceito, hipótese, ambiente, procedimento reproduzível, resultados (métricas/logs), análise, conclusão. Todo experimento deve poder ser repetido com um comando documentado.

## Laboratórios (`labs/<nome>/`)

Cada lab tem `README.md` (em português), código mínimo isolado do domínio e um relatório em `docs/experimentos/`.

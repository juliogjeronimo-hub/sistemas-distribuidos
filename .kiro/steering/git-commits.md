---
inclusion: always
---
# Convenção de commits (Conventional Commits em pt-br)

Todo commit deve seguir o padrão [Conventional Commits 1.0.0](https://www.conventionalcommits.org/en/v1.0.0/), com a **descrição em português**. Identificadores técnicos (tipo, escopo, rodapés como `BREAKING CHANGE`) permanecem em inglês, conforme `language-conventions.md`.

## Formato

```
<tipo>[escopo opcional][!]: <descrição em pt-br, no imperativo>

[corpo opcional, em pt-br]

[rodapé(s) opcional(is)]
```

Regras obrigatórias:

- O commit **DEVE** começar com um tipo, seguido de escopo opcional, `!` opcional, e `:` + espaço.
- A **descrição DEVE** seguir imediatamente o `: ` e ser curta, no modo imperativo, em português e sem ponto final (ex.: "adiciona", não "adicionado"/"adicionando").
- O **corpo**, quando existir, começa uma linha em branco após a descrição e é livre, em português.
- **Rodapés**, quando existirem, vêm uma linha em branco após o corpo. O token usa `-` no lugar de espaços (ex.: `Reviewed-by`, `Refs`), exceto `BREAKING CHANGE`.

## Tipos permitidos

| Tipo | Uso | Efeito em SemVer |
|---|---|---|
| `feat` | Nova funcionalidade | MINOR |
| `fix` | Correção de bug | PATCH |
| `docs` | Documentação (README, ADR, spec, changelog) | — |
| `refactor` | Mudança que não altera comportamento externo | — |
| `test` | Adiciona ou ajusta testes | — |
| `perf` | Melhoria de desempenho | — |
| `chore` | Tarefas de manutenção, dependências | — |
| `ci` | Pipeline e automação de CI | — |
| `build` | Build, empacotamento, Dockerfile | — |
| `revert` | Reverte um commit anterior | — |

Só use estes tipos. Se um commit se encaixa em mais de um tipo, divida em commits separados.

## Escopos permitidos

Serviços e áreas do monorepo: `catalog`, `inventory`, `orders`, `payments`, `notifications`, `gateway`, `contracts`, `infra`, `auth`, `docs`, `labs`.

O escopo é um substantivo entre parênteses que indica a parte do código afetada. Use o escopo sempre que a mudança for localizada em uma dessas áreas.

## Quebras de compatibilidade (BREAKING CHANGE)

Uma quebra de compatibilidade **DEVE** ser sinalizada de uma das formas:

- `!` antes dos dois-pontos no prefixo: `feat(orders)!: ...`; ou
- rodapé em maiúsculas `BREAKING CHANGE: <descrição>`.

Se usar `!` no prefixo, o rodapé `BREAKING CHANGE:` pode ser omitido e a própria descrição explica a quebra. `BREAKING CHANGE` pode acompanhar qualquer tipo e corresponde a um bump MAJOR.

## Exemplos

Commit simples com escopo:
```
feat(orders): adiciona orquestração da saga de pedido
```

Correção com escopo:
```
fix(auth): corrige validação da audience do token
```

Documentação:
```
docs(adr): registra decisão sobre handlers sem mediator
```

Quebra de compatibilidade com `!`:
```
feat(contracts)!: renomeia OrderPlaced para OrderPlacedV2
```

Quebra de compatibilidade por rodapé:
```
feat(gateway): remove rota legada de catálogo

BREAKING CHANGE: a rota `/catalog-legacy` foi removida; use `/catalog`.
```

Corpo com múltiplos parágrafos e rodapés:
```
fix(inventory): previne corrida na reserva de estoque

Introduz um identificador de requisição e descarta respostas que não
sejam da requisição mais recente.

Reviewed-by: Maria
Refs: #123
```

Reversão:
```
revert: desfaz "feat(payments): injeta falhas simuladas"

Refs: 676104e
```

## Casing

A convenção não é sensível a maiúsculas/minúsculas, **exceto** `BREAKING CHANGE`, que **DEVE** ser em maiúsculas. Mantenha tipos e escopos sempre em minúsculas para consistência.

## Relação com versionamento

- `fix` → PATCH, `feat` → MINOR, `BREAKING CHANGE` → MAJOR.
- Versão e `CHANGELOG.md` (em português) são calculados por **release-please** em modo monorepo a partir destes commits. Commits fora do padrão são ignorados pelas ferramentas.

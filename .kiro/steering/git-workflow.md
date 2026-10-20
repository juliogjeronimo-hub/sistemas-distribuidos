---
inclusion: manual
---
# Git e GitHub

## Gitflow

- `main`: produção, sempre estável; recebe apenas `release/*` e `hotfix/*`. Tags de versão aqui.
- `develop`: integração.
- `feature/<english-kebab>`: parte de `develop`, volta por PR.
- `release/<service>-<x.y.z>`: estabilização.
- `hotfix/<english-kebab>`: parte de `main`, volta para `main` e `develop`.
- `main` e `develop` protegidas: PR obrigatório, revisão, CI verde.

## Commits (Conventional Commits, descrição em português)

Formato: `tipo(escopo): descrição no imperativo, em português`

Tipos: `feat`, `fix`, `docs`, `refactor`, `test`, `chore`, `ci`, `perf`. Quebra de compatibilidade: `feat!:` ou rodapé `BREAKING CHANGE:`.
Escopos: `catalog`, `inventory`, `orders`, `payments`, `notifications`, `gateway`, `contracts`, `infra`, `auth`, `docs`, `labs`.

Exemplos:
```
feat(orders): adiciona orquestração da saga de pedido
fix(auth): corrige validação da audience do token
docs(adr): registra decisão sobre handlers sem mediator
```

## Versionamento

- SemVer **por serviço**. Tag: `<service>/vX.Y.Z` (ex.: `orders/v1.2.0`), igual à tag da imagem Docker.
- Versão e `CHANGELOG.md` (em português) calculados por **release-please** em modo monorepo, a partir dos commits.
- Contratos: HTTP em `/v1/...`; eventos com sufixo `V1`, `V2`.

## Pull Requests

Usar `.github/pull_request_template.md`. PR pequeno, ligado a uma spec, com testes e, quando a spec pedir, link para o relatório do experimento.

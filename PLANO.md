# Plano de steerings, specs e docs

## Estrutura entregue

```
.kiro/
  steering/
    product.md                 always
    tech.md                    always
    structure.md               always
    language-conventions.md    always
    learning-objectives.md     always
    observability.md           always
    clean-architecture.md      fileMatch  **/*.cs, **/*.csproj
    coding-standards.md        fileMatch  **/*.cs
    testing.md                 fileMatch  tests
    docker.md                  fileMatch  Dockerfile*, compose, deploy/**
    keycloak-auth.md           fileMatch  Auth, Program.cs, deploy/keycloak
    messaging-rabbitmq.md      fileMatch  Messaging, consumers, Outbox/Inbox
    resilience.md              fileMatch  clientes HTTP/gRPC
    distributed-patterns.md    manual     #distributed-patterns.md
    git-workflow.md            manual     #git-workflow.md
    adr-and-lab-docs.md        manual     #adr-and-lab-docs.md
  specs/
    01-foundation/             requirements.md, design.md, tasks.md (exemplo preenchido)
.github/pull_request_template.md
.editorconfig
docs/
  roadmap.md, mapa-disciplina.md
  adr/0001..0005
  templates/ requirements, design, tasks, adr, experiment-report
  experimentos/
labs/README.md
```

## Ordem de uso

1. Copiar o conteúdo para a raiz do repositório.
2. Revisar `tech.md` (versões) e `product.md` (domínio).
3. Executar a spec `01-foundation` no Kiro.
4. Para cada fase seguinte, criar `.kiro/specs/NN-nome/` a partir de `docs/templates/` seguindo `docs/roadmap.md`.
5. Ao concluir cada fase: relatório em `docs/experimentos/`, ADR se houve decisão nova, PR para `develop`.

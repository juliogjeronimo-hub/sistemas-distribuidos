# Tarefas: Fundação da plataforma

- [-] 1. Criar estrutura de pastas, `.editorconfig`, `Directory.Build.props` (Nullable, TreatWarningsAsErrors) _(Req. 6)_
- [ ] 2. Criar projetos `OrderHub.Catalog.{Domain,Application,Infrastructure,Api}` com referências corretas
  - [~] 2.1 Testes de arquitetura (NetArchTest) para as regras de dependência
- [~] 3. Implementar composition root, `Result`, `ICommandHandler`/`IQueryHandler` e decorators base
- [ ] 4. Implementar health checks `/health/live` e `/health/ready` _(Req. 5, 7)_
  - [~] 4.1 Teste de integração com Testcontainers
- [~] 5. Configurar logging estruturado e `CorrelationId` (middleware) _(Req. 4)_
- [~] 6. Configurar OpenTelemetry e endpoint de métricas _(Req. 5)_
- [~] 7. Escrever `Dockerfile` do Catalog (multi-stage, não-root)
- [~] 8. Criar `deploy/docker-compose*.yml` com redes, healthchecks e limites de recursos _(Req. 1)_
- [~] 9. Exportar realm `orderhub` para `deploy/keycloak/` e importar no boot _(Req. 2, 3)_
- [~] 10. Criar Gateway (YARP) com validação de JWT e propagação de `X-Correlation-Id` _(Req. 2, 3)_
- [~] 11. Criar dashboard do Grafana e config do Prometheus
- [~] 12. Criar workflow do GitHub Actions (build, testes, arquitetura) _(Req. 6)_
- [~] 13. Executar o experimento e registrar `docs/experimentos/01-fundacao.md`
- [~] 14. Atualizar README e docs

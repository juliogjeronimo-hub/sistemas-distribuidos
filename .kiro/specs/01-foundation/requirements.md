# Requisitos: Fundação da plataforma

## Contexto
Criar a base do OrderHub: estrutura da solution, ambiente local reproduzível com Docker, identidade com Keycloak, gateway, integração contínua e observabilidade mínima. Nenhuma regra de negócio ainda.

## Conceito de SD exercitado
- **Tópico da disciplina:** fundamentos e arquitetura de sistemas distribuídos; gerenciamento de componentes e processos em containers.
- **Premissas do modelo de sistema:** sistema assíncrono (sem limite de atraso de rede); falhas por *crash* de processos; sem partições de rede nesta fase.

## Requisitos (EARS)
1. QUANDO o desenvolvedor executar `docker compose up`, O SISTEMA DEVE subir Keycloak, RabbitMQ, PostgreSQL, Redis, Prometheus, Grafana e o Gateway em estado saudável.
2. QUANDO uma requisição chegar ao Gateway sem token, O SISTEMA DEVE responder 401.
3. QUANDO uma requisição chegar ao Gateway com token válido do realm `orderhub`, O SISTEMA DEVE encaminhá-la ao serviço de destino.
4. QUANDO um serviço receber uma requisição, O SISTEMA DEVE registrar log estruturado com `CorrelationId` e `TraceId`.
5. ENQUANTO um serviço estiver em execução, O SISTEMA DEVE expor `/health/live`, `/health/ready` e métricas para o Prometheus.
6. QUANDO um pull request for aberto para `develop`, O SISTEMA DEVE executar build, testes e testes de arquitetura no GitHub Actions.
7. SE uma dependência essencial estiver indisponível, ENTÃO `/health/ready` DEVE retornar falha.

## Critérios de aceite
- [ ] Estrutura de pastas e solution conforme `structure.md`
- [ ] Um serviço "esqueleto" (`Catalog`) com as quatro camadas e teste de arquitetura passando
- [ ] Realm `orderhub` importado automaticamente com um client de teste
- [ ] Dashboard do Grafana mostrando requisições e latência do esqueleto
- [ ] Pipeline de CI verde

## Evidência / experimento
- **Hipótese:** o ambiente completo é reproduzível com um comando e o desligamento de um container é refletido em health check e métricas.
- **Procedimento:** subir o ambiente, derrubar o container do Catalog, observar `/health/ready`, o Prometheus e o log do Gateway.
- **Métrica de sucesso:** falha detectada em até 30 s.
- **Relatório:** `docs/experimentos/01-fundacao.md`

## Fora de escopo
Regras de negócio, mensageria, Saga, múltiplas réplicas.

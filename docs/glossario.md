# Glossário do OrderHub

Termos usados no projeto, com explicação breve e o motivo de existirem aqui. Identificadores técnicos permanecem em inglês e em `code format`, conforme as convenções de idioma.

---

## Domínio (negócio)

| Termo | O que é | Por que é necessário |
|---|---|---|
| **Order** | Pedido feito por um cliente; agrega um ou mais itens. | É o agregado central do fluxo; dispara a Saga. |
| **OrderItem** | Item individual dentro de um pedido (produto + quantidade + preço). | Compõe o pedido e define o que será reservado e cobrado. |
| **Product** | Produto do catálogo (nome, preço). | Objeto de consulta do serviço `Catalog`. |
| **Reservation** | Reserva temporária de estoque para um pedido. | Garante que o estoque não seja vendido duas vezes durante a Saga. |
| **Payment** | Tentativa de cobrança de um pedido. | Etapa que pode falhar e exigir compensação. |
| **Saga** | Sequência de transações locais, cada uma com uma compensação. | Mantém consistência entre serviços sem transação distribuída global (2PC). |
| **Compensação** | Ação que desfaz uma etapa anterior da Saga (liberar estoque, cancelar pedido). | Recupera a consistência quando uma etapa falha. |
| **Money** | Value object com valor `decimal` + moeda explícita. | Evita erros de arredondamento e ambiguidade de moeda. |

---

## Arquitetura e padrões

| Termo | O que é | Por que é necessário |
|---|---|---|
| **Microsserviço** | Serviço autônomo, com seu próprio banco e ciclo de deploy. | Isola domínios e permite escalar/implantar de forma independente. |
| **Monorepo** | Todos os serviços num único repositório Git. | Facilita padronização e versionamento conjunto no contexto didático. |
| **Clean Architecture** | Camadas `Domain`, `Application`, `Infrastructure`, `Api` com dependências apontando para dentro. | Mantém o domínio livre de detalhes de infraestrutura e testável. |
| **Domain** | Camada de agregados, entidades, value objects e eventos de domínio. | Concentra as regras de negócio sem dependências externas. |
| **Application** | Camada de commands, queries, handlers, validators e ports. | Orquestra casos de uso dependendo só de abstrações. |
| **Infrastructure** | Implementações concretas (EF Core, RabbitMQ, clientes HTTP/gRPC). | Conecta a aplicação ao mundo real sem poluir o domínio. |
| **Api** | Endpoints, autenticação e composition root (DI). | Exposição HTTP e montagem das dependências. |
| **Aggregate root** | Entidade raiz que controla o acesso ao agregado. | Protege invariantes e define a fronteira de consistência. |
| **Port** | Interface definida em `Application` (ex.: `IOrderRepository`, `IEventBus`). | Inverte a dependência: a aplicação não conhece a infraestrutura. |
| **Command / Query** | Mensagem de escrita (`Command`) ou leitura (`Query`). | Separa intenções de mudança das de consulta (CQRS leve). |
| **Handler** | Classe que executa um caso de uso (`ICommandHandler`/`IQueryHandler`). | Um caso de uso por classe, sem biblioteca de mediator. |
| **Decorator** | Camada que envolve um handler (validação, logging, métricas) via Scrutor. | Adiciona comportamento transversal sem alterar o handler. |
| **Result pattern** | Tipo `Result` que representa sucesso ou falha de negócio. | Evita usar exceptions para fluxo normal. |
| **BuildingBlocks** | Projetos compartilhados: contratos de eventos e utilitários técnicos. | Único ponto de compartilhamento entre serviços; nunca entidades de domínio. |

---

## Comunicação e mensageria

| Termo | O que é | Por que é necessário |
|---|---|---|
| **Gateway (YARP)** | Proxy reverso que encaminha requisições aos serviços. | Ponto único de entrada; valida token e injeta `CorrelationId`. |
| **REST** | Comunicação síncrona sobre HTTP. | Chamadas cliente-servidor e entre serviços quando resposta imediata é necessária. |
| **gRPC** | Invocação remota sobre HTTP/2 com Protobuf. | Comunicação síncrona eficiente onde a spec pedir. |
| **RabbitMQ** | Broker de mensagens para comunicação assíncrona. | Desacopla serviços e habilita pub/sub e a Saga. |
| **IEventBus** | Abstração própria sobre o RabbitMQ. | Esconde detalhes do broker dos serviços (sem MassTransit). |
| **Exchange / Queue / Routing** | Elementos do RabbitMQ para rotear e armazenar mensagens. | Definem como eventos chegam aos consumidores. |
| **Pub/Sub** | Publicação de eventos consumidos por vários assinantes. | Base do `Notifications` e da comunicação em grupo. |
| **Evento de integração** | Contrato publicado entre serviços (ex.: `OrderPlacedV1`). | Comunica fatos entre serviços sem acoplar seus modelos internos. |
| **Outbox** | Tabela onde o evento é gravado na mesma transação do dado. | Garante que o evento só seja publicado se a transação persistir. |
| **Inbox** | Registro de mensagens já processadas pelo consumidor. | Garante idempotência (evita processar a mesma mensagem duas vezes). |
| **DLQ (Dead Letter Queue)** | Fila para mensagens que falharam repetidamente. | Isola mensagens problemáticas sem travar o consumo. |
| **Consistência eventual** | Estado converge entre serviços após algum tempo. | Consequência de usar mensageria em vez de transação global. |

---

## Segurança e identidade

| Termo | O que é | Por que é necessário |
|---|---|---|
| **Keycloak** | Servidor de identidade (OAuth 2.0 / OIDC). | Emissor único de tokens do sistema. |
| **OAuth 2.0 / OIDC** | Protocolos de autorização e autenticação. | Padrão para proteger APIs sem compartilhar senhas. |
| **Realm** | Espaço isolado de configuração no Keycloak (`orderhub`). | Agrupa clients, scopes e usuários do projeto. |
| **Resource server** | API que valida tokens mas não os emite. | Cada serviço valida o JWT por conta própria (defesa em profundidade). |
| **JWT** | Token assinado com claims (`iss`, `aud`, `exp`, `scope`). | Carrega a identidade/autorização de forma verificável. |
| **client_credentials** | Fluxo OAuth máquina-a-máquina (sem usuário). | Usado por serviços e pelo `orderhub-cli` para obter token. |
| **Authorization Code + PKCE** | Fluxo OAuth para usuários/front-end. | Login seguro de pessoas (quando houver UI). |
| **Scope** | Permissão no token (ex.: `catalog.read`). | Base das policies de autorização. |
| **Policy baseada em escopo** | Regra de autorização que checa o scope. | Proibido checar claim solto dentro dos handlers. |
| **Audience (`aud`)** | Destinatário esperado do token (`orderhub-api`). | Impede usar um token emitido para outro serviço. |
| **Issuer (`iss`)** | Emissor do token (URL do realm). | Garante que o token veio do Keycloak esperado. |
| **ICurrentUser** | Abstração que leva a identidade aos handlers. | Isola a `Application` dos detalhes do token. |

---

## Dados e armazenamento

| Termo | O que é | Por que é necessário |
|---|---|---|
| **PostgreSQL** | Banco relacional, um por serviço. | Persistência isolada; nenhum serviço lê o banco de outro. |
| **EF Core (Npgsql)** | ORM usado para acesso a dados. | Mapeia entidades e gerencia transações. |
| **Dapper** | Micro-ORM para leituras pesadas. | Consultas de leitura mais rápidas quando necessário. |
| **Redis** | Cache em memória. | Réplica de leitura e redução de carga no banco. |
| **MinIO** | Armazenamento de objetos compatível com S3. | Arquivos distribuídos sem depender de nuvem. |
| **Concorrência otimista** | Controle de versão de linha para evitar escrita conflitante. | Protege a reserva de estoque contra corridas. |

---

## Infraestrutura e operação

| Termo | O que é | Por que é necessário |
|---|---|---|
| **Docker** | Empacota cada serviço em container. | Ambiente reproduzível e isolado (processo). |
| **Docker Compose** | Orquestra containers localmente. | Sobe o stack de desenvolvimento de uma vez. |
| **Docker Swarm** | Orquestra containers em várias VMs. | Deploy com réplicas e failover no laboratório. |
| **cgroups** | Limites de CPU/memória do SO. | Exercita o suporte de SO a processos isolados. |
| **Health check** | Endpoints `/health/live` e `/health/ready`. | Informam se o processo está vivo e se as dependências estão ok. |
| **OpenTelemetry** | Coleta de traces e métricas. | Observabilidade padronizada entre serviços. |
| **Prometheus** | Coletor de métricas. | Armazena séries temporais para análise e alertas. |
| **Grafana** | Dashboards sobre as métricas. | Visualiza latência, erros e filas. |
| **CorrelationId** | Identificador propagado em toda a requisição. | Rastreia um fluxo que cruza vários serviços. |
| **Resiliência (Polly)** | Retry, timeout e circuit breaker. | Tolerância a falhas em chamadas entre serviços. |
| **Circuit breaker** | Interrompe chamadas a um serviço em falha. | Evita efeito cascata e dá tempo de recuperação. |

---

## Testes e qualidade

| Termo | O que é | Por que é necessário |
|---|---|---|
| **xUnit** | Framework de testes. | Base dos testes unitários e de integração. |
| **Testcontainers** | Sobe dependências reais (banco, broker) em containers nos testes. | Testes de integração fiéis ao ambiente real. |
| **NetArchTest** | Valida regras de dependência entre camadas. | Garante que a Clean Architecture não seja violada. |
| **k6** | Ferramenta de teste de carga. | Mede desempenho e capacidade. |
| **Teste de caos** | Injeta falhas propositais. | Valida tolerância a falhas e compensações. |

---

## Processo e convenções

| Termo | O que é | Por que é necessário |
|---|---|---|
| **Spec** | Conjunto `requirements.md`, `design.md`, `tasks.md` por funcionalidade. | Planeja e rastreia cada entrega. |
| **ADR** | Registro de decisão de arquitetura. | Documenta o porquê de escolhas técnicas. |
| **Conventional Commits** | Padrão de mensagem de commit (tipo + escopo + descrição). | Permite versionamento e changelog automáticos. |
| **release-please** | Ferramenta que calcula versão e changelog a partir dos commits. | Automatiza releases em modo monorepo. |
| **GitHub Actions** | Automação de CI. | Build e testes a cada mudança. |

---

## Conceitos da disciplina (Sistemas Distribuídos)

| Termo | O que é | Por que aparece no projeto |
|---|---|---|
| **Cliente-servidor** | Modelo requisição/resposta. | Gateway + REST no `Catalog`. |
| **Transação distribuída** | Operação que abrange vários serviços. | Resolvida via Saga em vez de 2PC. |
| **Replicação** | Cópias de dados para leitura/disponibilidade. | Réplica de leitura e cache no `Catalog`. |
| **Lock distribuído** | Exclusão mútua entre processos distintos. | Controle de concorrência no `Inventory`. |
| **Alta disponibilidade** | Sistema tolera falha de instâncias. | Réplicas e failover no Swarm. |
| **Balanceamento de carga** | Distribui requisições entre réplicas. | Atrás do gateway/balanceador. |
| **Comunicação em grupo** | Entrega de mensagens a múltiplos membros. | Pub/sub no `Notifications`; labs de multicast. |

---

> Este glossário é vivo: ao introduzir um termo novo em uma spec ou ADR, acrescente-o aqui com uma linha de explicação e o motivo.

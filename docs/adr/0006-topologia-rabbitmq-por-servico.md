# ADR 0006: Topologia do RabbitMQ declarada por cada serviço

- **Status:** Aceito
- **Data:** 2026-10-26

## Contexto
A fase de infraestrutura base (`02-infra-base`) sobe um RabbitMQ compartilhado por todos os serviços. Era preciso decidir onde as `exchanges`, filas e bindings seriam criados: pré-provisionados no compose/infra (por exemplo, via `definitions.json` do broker) ou declarados em tempo de execução pelo código de cada serviço.

A pré-criação centralizada acoplaria a topologia de mensageria à infra compartilhada: cada novo evento ou consumidor exigiria editar um artefato comum, fora do serviço dono daquele contrato. Isso contraria o princípio de que nenhum serviço conhece os detalhes internos de outro e dilui a propriedade dos contratos, que vivem em `OrderHub.BuildingBlocks.Contracts`.

## Decisão
A plataforma compartilhada provê apenas o broker (RabbitMQ com o plugin de management e filas `quorum` por padrão). **Nenhuma `exchange`, fila ou binding é pré-criada na infra.**

Cada serviço declara a própria topologia em tempo de execução (idempotente, no startup do produtor/consumidor), seguindo a nomenclatura de `messaging-rabbitmq`: `exchange` `orderhub.events` (`topic`, durável), routing keys `<service>.<event-kebab>.v<N>`, filas `<consumer>.<event-kebab>.v<N>` e dead-letter `<fila>.dlq`.

A implementação concreta de `IEventBus` sobre RabbitMQ, que fará essa declaração, chega na fase 03; esta fase define apenas as abstrações em `OrderHub.BuildingBlocks.Messaging`.

## Alternativas consideradas
- **Pré-criar `exchanges`/filas na infra (via `definitions.json`):** descartada. Acopla a topologia ao artefato de infra compartilhado, exige mudança centralizada a cada novo evento e quebra a propriedade do contrato pelo serviço dono.
- **Serviço de provisionamento dedicado:** descartada por excesso de complexidade para um projeto didático; o `IEventBus` idempotente no startup resolve o mesmo problema.

## Consequências
### Positivas
- Cada serviço é dono completo da sua topologia e dos seus contratos; evoluir um evento não toca na infra nem em outro serviço.
- A infra compartilhada permanece estável e agnóstica de domínio entre as fases.
- Declaração idempotente no startup torna o broker reproduzível sem estado pré-carregado.

### Negativas
- A topologia só existe depois que o serviço sobe; o broker recém-iniciado aparece "vazio" até o primeiro produtor/consumidor declarar seus recursos.
- Risco de divergência de nomenclatura entre serviços, mitigado pela convenção em `messaging-rabbitmq` e por testes.

## Conceito da disciplina relacionado
Troca de mensagens; middleware; autonomia e acoplamento entre componentes de um sistema distribuído.

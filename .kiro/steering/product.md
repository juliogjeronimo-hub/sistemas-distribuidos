---
inclusion: always
---
# Produto: OrderHub

Sistema de pedidos baseado em microsserviços. É um **projeto didático** da disciplina de Sistemas Distribuídos (bibliografia: Coulouris, Dollimore e Kindberg, *Sistemas Distribuídos: Conceitos e Projeto*, Bookman, 2007). O objetivo principal é demonstrar conceitos de sistemas distribuídos; o escopo de negócio é propositalmente mínimo.

## Serviços

| Serviço | Responsabilidade | Conceito principal |
|---|---|---|
| Catalog | Consulta e cadastro de produtos | Cliente-servidor, cache, réplica de leitura |
| Inventory | Reserva e liberação de estoque | Controle de concorrência, locks |
| Orders | Criação do pedido e orquestração da Saga | Transações distribuídas, consistência eventual |
| Payments | Pagamento simulado, com falhas injetáveis | Tolerância a falhas, compensação |
| Notifications | Consome eventos e notifica | Pub/sub, comunicação em grupo |

Infraestrutura de apoio: Gateway (YARP), Keycloak, RabbitMQ, um PostgreSQL por serviço, Redis, MinIO, Prometheus, Grafana.

## Fluxo central (Saga)

`Orders` cria pedido → `Inventory` reserva estoque → `Payments` cobra → `Orders` confirma.
Em qualquer falha, as etapas anteriores são compensadas (estoque liberado, pedido cancelado).

## Glossário (termos de domínio em inglês no código)

- **Order**: pedido. **OrderItem**: item do pedido.
- **Reservation**: reserva temporária de estoque.
- **Payment**: tentativa de cobrança.
- **Saga**: sequência de transações locais com compensações.

## Regras para o agente

- Antes de propor uma funcionalidade, identificar qual conceito da disciplina ela exercita (ver `learning-objectives.md`).
- Não adicionar funcionalidades de negócio além do necessário para exercitar o conceito.

# Requisitos: Servico de estoque (base, sem reservas)

> **Pre-requisito:** specs `02a-login-oauth` (policies `InventoryRead`/`InventoryWrite` e `OrderHub.BuildingBlocks.Auth`) e `02b-catalog-crud` (padrao de servico com PostgreSQL) concluidas.

## Contexto
O Inventory e criado do zero no mesmo padrao do Catalog: quatro camadas, banco proprio `postgres-inventory`, rota no Gateway e autenticacao compartilhada. Nesta fase o servico apenas mantem o saldo por produto e registra ajustes manuais (admin), com controle de concorrencia otimista. Reserva de estoque para pedidos fica para a fase 03b.

## Conceito de SD exercitado
- **Topico da disciplina:** autonomia de dados (`ProductId` como referencia logica, sem FK nem chamada ao Catalog); controle de concorrencia otimista (versao de linha); tratamento de conflitos em escritas concorrentes.
- **Premissas do modelo de sistema:** sistema assincrono; falhas por *crash*; escritas concorrentes no mesmo item sao esperadas; sem partição de rede nesta fase.

## Requisitos (EARS)
1. QUANDO um admin enviar `POST /v1/stock/{productId}/adjustments` com delta e motivo, O SISTEMA DEVE aplicar o ajuste e registrar um `StockMovement`.
2. SE o ajuste resultar em saldo negativo, ENTAO O SISTEMA DEVE rejeitar com `400` sem alterar o saldo (RN-INV-01).
3. QUANDO o primeiro ajuste positivo de um `productId` chegar, O SISTEMA DEVE criar o `StockItem` implicitamente.
4. QUANDO `GET /v1/stock/{productId}` for chamado, O SISTEMA DEVE retornar o saldo atual; `404` se nunca houve ajuste.
5. SE duas escritas concorrentes colidirem no mesmo item, ENTAO O SISTEMA DEVE detectar o conflito e responder `409` (sem retry automatico nesta fase).
6. QUANDO um nao-admin tentar ajustar estoque, O SISTEMA DEVE responder `403` (RN-INV-06).
7. ENQUANTO a plataforma estiver no ar, `/health/ready` do Inventory DEVE refletir o `postgres-inventory`.

## Criterios de aceite
- [ ] Ajuste positivo cria o item; ajustes somam/subtraem; saldo negativo rejeitado (400).
- [ ] `GET /v1/stock/{productId}` e `.../movements` retornam dados coerentes; 404 quando nao existe.
- [ ] Conflito de concorrencia devolve 409; nenhum ajuste perdido em escritas concorrentes.
- [ ] 401 sem token; 403 para nao-admin; 200 para admin.
- [ ] Rota `/inventory/{**rest}` no Gateway; `/health/ready` reflete o banco.

## Evidencia / experimento
- **Hipotese:** sob ajustes concorrentes no mesmo item, a concorrencia otimista garante saldo final = soma dos deltas aceitos, sem perda, convertendo colisoes em 409.
- **Procedimento:** disparar N ajustes paralelos (ex.: 50 via PowerShell/k6) no mesmo produto; contar 200 vs 409 e conferir o saldo final; repetir com retry no cliente e comparar.
- **Metrica de sucesso:** saldo final correto (= soma dos deltas que retornaram 200); nenhum ajuste perdido; colisoes observaveis como 409.
- **Relatorio:** `docs/experimentos/02c-inventory-base.md`

## Fora de escopo
- Reserva por pedido, expiracao, idempotencia por pedido, baixa definitiva (RN-INV-02..05, 07) - fase 03b.
- Lock distribuido - fase 08.
- Eventos/consumo de mensagens - fase 03.

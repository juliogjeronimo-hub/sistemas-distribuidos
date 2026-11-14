# Requisitos: Catálogo completo com PostgreSQL

> **Pré-requisito:** spec `02a-login-oauth` concluída — este serviço usa as policies `CatalogRead`/`CatalogWrite` e o projeto `OrderHub.BuildingBlocks.Auth`.

## Contexto
O Catalog hoje responde `GET /v1/products` com dados fixos. Esta fase entrega o catálogo completo com persistência própria em PostgreSQL: CRUD de produtos com invariantes de domínio, autorização por papel/escopo e dependência de banco refletida no `/health/ready`. Reforça a autonomia de dados (um banco por serviço) definida no projeto.

## Conceito de SD exercitado
- **Tópico da disciplina:** cliente-servidor com estado; autonomia de dados (um banco por serviço, sem acoplamento a outro); distinção liveness × readiness (dependência de dados no health check).
- **Premissas do modelo de sistema:** sistema assíncrono; falhas por *crash* (o banco pode cair); sem partição de rede nesta fase.

## Requisitos (EARS)
1. QUANDO um admin enviar `POST /v1/products` válido, O SISTEMA DEVE persistir o produto e responder `201` com `Location`.
2. QUANDO um `Sku` já existente for enviado na criação, O SISTEMA DEVE responder `409`.
3. SE o corpo violar uma invariante (Sku inválido, Price <= 0), ENTÃO O SISTEMA DEVE responder `400` com `ProblemDetails`.
4. QUANDO um customer listar produtos, O SISTEMA DEVE retornar apenas produtos ativos, paginados (page, pageSize padrão 20, máximo 50), ordenados por `Name`.
5. QUANDO um admin listar com `includeInactive=true`, O SISTEMA DEVE incluir produtos inativos.
6. QUANDO um produto inexistente for consultado ou alterado, O SISTEMA DEVE responder `404`.
7. SE o PostgreSQL estiver indisponível, ENTÃO `/health/ready` DEVE falhar enquanto `/health/live` continua `200`.
8. QUANDO um customer tentar criar, alterar ou desativar produto, O SISTEMA DEVE responder `403` (apenas admin escreve).

## Critérios de aceite
- [ ] CRUD completo: criar (201), consultar (200/404), atualizar (200), desativar (200) com banco real.
- [ ] `Sku` duplicado → 409; validação inválida → 400 `ProblemDetails`.
- [ ] Listagem paginada: customer só ativos; admin com `includeInactive=true`.
- [ ] 401 sem token; 403 para customer em endpoints de escrita; 200 para admin.
- [ ] `/health/ready` reflete o banco; `/health/live` não depende dele.
- [ ] Migração inicial aplicada apenas em Development; connection string por variável de ambiente.

## Evidência / experimento
- **Hipótese:** parar o banco derruba a prontidão do serviço (ready falha) sem matar o processo (live segue), e o serviço se recupera ao religar o banco.
- **Procedimento:** com o serviço no ar, parar o `postgres-catalog`, observar `/health/live` (espera 200) e `/health/ready` (espera falha), e o status das chamadas de negócio; religar e medir o tempo de recuperação.
- **Métrica de sucesso:** `/health/live` segue 200; `/health/ready` falha; chamadas devolvem erro coerente (503/500); recuperação automática ao religar.
- **Relatório:** `docs/experimentos/02b-catalog-crud.md`

## Fora de escopo
- Cache (Redis), réplica de leitura, eventos `ProductChangedV1`, imagens (MinIO) — fases posteriores.
- RN-CAT-04 (congelar preço no pedido) — citada no design como contrato futuro, usada na fase de pedidos.
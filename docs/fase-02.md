OBJETIVO
Criar as SPECS (somente documentos) da fase 02 do OrderHub, em três pastas:
  .kiro/specs/02a-login-oauth/
  .kiro/specs/02b-catalog-crud/
  .kiro/specs/02c-inventory-base/
Cada pasta terá 3 arquivos: requirements.md, design.md, tasks.md (9 arquivos no total).
NÃO IMPLEMENTE CÓDIGO. NÃO altere nenhum outro arquivo do repositório.

REGRAS (economia de créditos)
1. Gere os 9 arquivos de forma direta, em sequência, sem pedir aprovação intermediária e sem perguntas. Se faltar informação, decida pela opção mais simples e registre a suposição em "Decisões e suposições" do design.md.
2. Leia SOMENTE estes arquivos: docs/ideia-do-projeto.md, docs/templates/requirements.md, docs/templates/design.md, docs/templates/tasks.md, .kiro/specs/01a-hello-gateway-catalog/tasks.md (como modelo de formato de tasks) e os steerings já carregados. Não explore o código nem o restante do repositório.
3. Escreva cada arquivo UMA vez, completo. Nada de revisões ou reescritas.
4. Tamanho: requirements.md até ~60 linhas; design.md até ~90 linhas; tasks.md com 10 a 15 tarefas. Seja objetivo; sem texto de enchimento.
5. Idioma: specs em português; identificadores técnicos, código, rotas, nomes de claims e de eventos em inglês (conforme language-conventions.md).
6. Resposta final: no máximo 6 linhas (arquivos criados + decisões em aberto, se houver).

FORMATO OBRIGATÓRIO
- requirements.md: seguir docs/templates/requirements.md (requisitos em EARS, critérios de aceite testáveis, seções "Conceito de SD exercitado" e "Evidência / experimento", "Fora de escopo").
- design.md: seguir docs/templates/design.md, mais a seção "Decisões e suposições". Inclua diagrama Mermaid quando ajudar (máx. 1 por spec).
- tasks.md: seguir o formato do 01a (cabeçalho com objetivo e ambiente; "Protocolo de execução" idêntico ao do 01a; legenda [ ] [x] [!]; seção "Caminhos reais" a preencher na Tarefa 0; tarefas numeradas, cada uma com "Checagem" (comando PowerShell curto, ex.: Select-String/Test-Path) e "Commit" (Conventional Commits, descrição em português); tarefa final de fechamento; seção "Registro de execução" com tabela vazia). Ambiente: Windows + PowerShell, .NET 10, Docker Compose, curl.exe. Cada tarefa deve caber em uma única execução pequena. As últimas tarefas de cada spec: escrever o ADR (quando indicado), executar o experimento e registrar o relatório em docs/experimentos/ usando docs/templates/experiment-report.md.
- Testes: manter o mínimo exigido por testing.md, em tarefas enxutas (testes unitários do domínio, 1 teste de integração por serviço com Testcontainers, testes de arquitetura NetArchTest, matriz de autorização). Sem testes extras.

CONTEXTO JÁ EXISTENTE (não recriar)
Gateway (YARP) e Catalog (esqueleto) rodam em containers; Keycloak 26 com realm `orderhub` importado de deploy/keycloak/orderhub-realm.json; client `orderhub-cli` (client_credentials) e scope `catalog.read` com audience mapper `orderhub-api`; Gateway e Catalog validam JWT (JwtBearer, MapInboundClaims=false, ValidIssuer http://localhost:8180/realms/orderhub, audience orderhub-api, MetadataAddress http://keycloak:8080/...); GET /catalog/v1/products responde com dados fixos; compose em deploy/; portas: gateway 5000, catalog 5001, keycloak 8180.

====================================================================
SPEC 02a-login-oauth — Login de usuários com OAuth 2.0 (Authorization Code + PKCE)
====================================================================
Escopo e decisões (já tomadas):
- O login acontece no Keycloak; as APIs continuam apenas validando JWT (resource servers).
- Realm: roles `customer` (role padrão de novos usuários) e `admin`; usuários de teste dev: `alice` (customer, senha alice123) e `bob` (admin, senha bob123), sem troca obrigatória de senha; auto-cadastro habilitado; access token com 300 s de vida; refresh token habilitado.
- Client público `orderhub-web`: Authorization Code + PKCE S256, sem secret, standard flow ligado, direct access grants desligado, redirect URI `http://localhost:5000/app/*`, web origin `http://localhost:5000`, post-logout redirect `http://localhost:5000/app/*`. O `orderhub-cli` continua existindo para serviços.
- Client scopes (com audience mapper `orderhub-api` em todos): `catalog.read`, `catalog.write`, `inventory.read`, `inventory.write`. Mapper que publica as realm roles em claim plano `roles` (lista, no access token). Os dois clients recebem esses scopes e o mapper como default.
- Autorização = escopo E papel: policies `CatalogRead` (scope catalog.read), `CatalogWrite` (scope catalog.write E role admin), `InventoryRead` (scope inventory.read E role admin), `InventoryWrite` (scope inventory.write E role admin), `AdminOnly` (role admin). O claim `scope` do Keycloak é uma string separada por espaços; `roles` é lista.
- Criar o projeto técnico `OrderHub.BuildingBlocks.Auth` (somente utilitários técnicos, sem domínio) com: extensão `AddOrderHubJwtAuth(IConfiguration)` (a configuração JwtBearer hoje duplicada em Gateway e Catalog, lendo a seção `Auth` já existente), constantes de nomes de policies/claims e registro das policies acima. Gateway e Catalog passam a usá-lo (remover a configuração duplicada). Atualizar os `COPY` de .csproj nos Dockerfiles (as linhas de building-blocks estão comentadas hoje).
- Identidade no backend: interface `ICurrentUser` (UserId a partir de `sub`, UserName a partir de `preferred_username`, Roles, Scopes) definida na camada Application do Catalog (porta pequena), implementada na camada Api com `IHttpContextAccessor`. (O Inventory repetirá o padrão na 02c.)
- Cliente de login mínimo, servido pelo Gateway em `/app` (UseStaticFiles; anônimo): `index.html` + `app.js` sem bibliotecas nem CDN. Fluxo PKCE em JavaScript puro (code_verifier/challenge S256 via crypto.subtle, `state`, troca do code no endpoint de token do Keycloak em http://localhost:8180, tokens mantidos em memória; `sessionStorage` só para verifier/state). Botões: Login, Chamar /me, Listar produtos (`/catalog/v1/products`), Refresh, Logout (end_session). Exibe o payload do access token decodificado e o status HTTP das chamadas.
- Endpoint `GET /me` no Gateway (exige token): devolve sub, preferred_username, roles, scopes e exp.
- Rotas do YARP não devem capturar `/app` nem `/me`.
- ADR 0006: Authorization Code + PKCE para usuários; identidade chega aos serviços propagando o token do usuário (o Gateway repassa o header Authorization); client_credentials continua para chamadas serviço→serviço.
Requisitos a cobrir (EARS): login com alice e bob; token com claims roles/scope/aud; 401 sem token; 403 sem papel ou escopo; /me; refresh; logout; token expirado → 401 e cliente reinicia o login; health e /app anônimos.
Conceito de SD: segurança distribuída (autenticação centralizada, validação local do JWT), tempo (expiração e clock skew), propagação de identidade.
Experimentos (relatório docs/experimentos/02a-login-oauth.md): (1) parar o Keycloak e observar: tokens já emitidos continuam válidos nas APIs (chaves em cache) e novos logins falham; (2) simular diferença de relógio (ex.: Tarefa que altera o tempo de vida do token para 30 s e mede a rejeição após a expiração, incluindo o efeito do ClockSkew padrão de 5 min do JwtBearer; documentar a decisão de reduzi-lo para 30 s).
Matriz de autorização (tarefa de teste): para cada endpoint e cada identidade (anônimo, alice, bob, orderhub-cli) o status esperado 401/403/200.

====================================================================
SPEC 02b-catalog-crud — Catálogo completo com PostgreSQL
====================================================================
Escopo e decisões:
- Novo serviço de banco `postgres-catalog` (PostgreSQL 17) no compose, com volume nomeado, healthcheck com pg_isready, rede interna, porta não exposta ao host (exceto profile `debug`).
- Catalog.Infrastructure: EF Core + Npgsql, `IEntityTypeConfiguration`, repositório de Product (um por aggregate root), UnitOfWork, migração inicial; migrações aplicadas na inicialização apenas em ambiente Development. Connection string por variável de ambiente (`ConnectionStrings__Catalog`). Chamar AddInfrastructure e incluir o banco no `/health/ready` (o `/health/live` não depende do banco).
- Domínio: aggregate `Product` (Id Guid v7, Sku único, Name, Description, Price como value object Money com moeda BRL, IsActive, CreatedAt/UpdatedAt via TimeProvider); construtor privado, factory method retornando Result, sem setters públicos; invariantes: Sku obrigatório (formato simples, ex.: 3–32 caracteres alfanuméricos e hífen), Price > 0.
- Casos de uso (handlers próprios, sem mediator; validação com FluentValidation via decorator; Result pattern): CreateProduct, UpdateProduct, DeactivateProduct, GetProductById, ListProducts (paginado: page, pageSize padrão 20 e máximo 50, ordenado por Name; customer vê só produtos ativos; admin pode pedir `includeInactive=true`).
- Endpoints (v1): GET /v1/products, GET /v1/products/{id} (policy CatalogRead); POST /v1/products (201 + Location), PUT /v1/products/{id}, POST /v1/products/{id}/deactivate (policy CatalogWrite). Erros em ProblemDetails (400 validação, 404, 409 Sku duplicado).
- Substituir os dados fixos do endpoint atual. O Gateway não muda (rota /catalog já existe).
- Ainda NÃO implementar: cache, réplica, eventos (ProductChanged), imagens. Registrar como fora de escopo.
- RNs do docs/ideia-do-projeto.md a cobrir: RN-CAT-01, 02, 03, 05, 06 (RN-CAT-04 será usada na fase de pedidos; citar no design como contrato futuro).
- Testes mínimos: unitários do Product (invariantes), 1 integração com Testcontainers (criar → listar → desativar com banco real), arquitetura (NetArchTest) do Catalog, e checagem 401/403/200 por papel nos endpoints de escrita.
Conceito de SD: cliente-servidor com estado; autonomia de dados (um banco por serviço); dependência de dados no health check (liveness × readiness).
Experimento (docs/experimentos/02b-catalog-crud.md): parar o postgres-catalog durante o uso e registrar: /health/live continua 200, /health/ready falha, comportamento das chamadas (503 vs 500), tempo até a recuperação ao religar; anotar o efeito de timeouts de conexão.

====================================================================
SPEC 02c-inventory-base — Serviço de estoque (base, sem reservas)
====================================================================
Escopo e decisões:
- Criar o serviço Inventory completo no mesmo padrão do Catalog: projetos OrderHub.Inventory.{Domain,Application,Infrastructure,Api} (caminhos conforme structure.md/estrutura real), Dockerfile (mesmo padrão do Catalog: multi-stage, curl, USER $APP_UID, porta 8080), banco `postgres-inventory`, serviço no compose (porta 5002:8080, healthchecks, depends_on do banco saudável), rota `/inventory/{**rest}` no YARP do Gateway (PathRemovePrefix), mesma autenticação via OrderHub.BuildingBlocks.Auth, ICurrentUser próprio na Application.
- Domínio: aggregate `StockItem` (ProductId Guid como referência lógica SEM chave estrangeira nem chamada ao Catalog; Sku opcional informativo; QuantityOnHand >= 0; token de concorrência otimista: coluna `xmin` do PostgreSQL ou `rowversion` equivalente via EF Core) e entidade `StockMovement` (registro de cada ajuste: delta, motivo, UserId, data). Não criar campo de reserva nesta fase.
- Casos de uso: AdjustStock (delta positivo ou negativo com motivo obrigatório; rejeita resultado negativo → RN-INV-01), GetStock, ListMovements (paginado). Criação implícita do StockItem no primeiro ajuste positivo.
- Endpoints: GET /v1/stock/{productId} (policy InventoryRead), GET /v1/stock/{productId}/movements, POST /v1/stock/{productId}/adjustments (policy InventoryWrite; somente admin, RN-INV-06). Conflito de concorrência → 409 ProblemDetails e o cliente repete a operação; o handler NÃO faz retry automático nesta fase (para tornar o conflito observável).
- RNs a cobrir: RN-INV-01 e RN-INV-06. As demais (reserva, expiração, idempotência por pedido) ficam para a spec 03b; citar no design como evolução prevista.
- Testes mínimos: unitários do StockItem, 1 integração com Testcontainers (ajustes sequenciais e rejeição de estoque negativo), teste de concorrência (N ajustes paralelos no mesmo item: nenhum ajuste perdido e saldo final = soma dos deltas aceitos), arquitetura (NetArchTest), autorização por papel.
- ADR 0007 (curto): concorrência otimista em vez de lock pessimista/distribuído para ajustes de estoque, com a justificativa e o plano de usar lock distribuído só onde necessário (fase 08).
Conceito de SD: autonomia de dados (sem FK entre serviços), controle de concorrência otimista, tratamento de conflitos em escritas concorrentes.
Experimento (docs/experimentos/02c-inventory-base.md): disparar ajustes concorrentes no mesmo produto (ex.: 50 requisições paralelas via PowerShell ou k6) e registrar: quantidade de 200 vs 409, saldo final, ausência de ajustes perdidos; repetir com política de repetição no cliente e comparar.

ORDEM E DEPENDÊNCIAS ENTRE AS SPECS
02a → 02b → 02c. A 02b e a 02c dependem das policies e do projeto BuildingBlocks.Auth criados na 02a; a 02c reutiliza o padrão do Catalog. Registre essa dependência no início de cada requirements.md ("Pré-requisito").

ENTREGA
Resposta final de até 6 linhas: lista dos 9 arquivos criados e quaisquer decisões em aberto que exijam minha confirmação.
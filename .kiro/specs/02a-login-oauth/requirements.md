# Requisitos: Login de usuários com OAuth 2.0 (Authorization Code + PKCE)

> **Pré-requisito:** nenhuma spec da fase 02 anterior. Esta é a primeira; a `02b` e a `02c` dependem das policies e do projeto `OrderHub.BuildingBlocks.Auth` criados aqui.

## Contexto
A fase 01 entregou Gateway (YARP), Catalog esqueleto e Keycloak 26 com o realm `orderhub` e o client `orderhub-cli` (`client_credentials`). Falta o login de **usuários finais**: `customer` e `admin` precisam autenticar no Keycloak e chamar as APIs com um token que carregue papéis e escopos. As APIs permanecem *resource servers* (só validam JWT). Também há configuração de JwtBearer duplicada em Gateway e Catalog, a ser extraída para um bloco técnico comum.

## Conceito de SD exercitado
- **Tópico da disciplina:** segurança distribuída — autenticação centralizada no Keycloak com validação local do JWT em cada serviço (confiança sem consulta central por requisição); propagação de identidade entre Gateway e serviços.
- **Premissas do modelo de sistema:** sistema assíncrono; falhas por *crash* (Keycloak pode cair); tempo não confiável entre nós (clock skew), relevante para expiração de token.

## Requisitos (EARS)
1. QUANDO `alice` (customer) ou `bob` (admin) fizer login via Authorization Code + PKCE, O SISTEMA DEVE emitir um access token com as claims `roles` (lista) e `scope` (string) e `aud` = `orderhub-api`.
2. QUANDO uma requisição chegar a um endpoint protegido sem token válido, O SISTEMA DEVE responder `401`.
3. SE o token não tiver o papel ou o escopo exigidos pela policy, ENTÃO O SISTEMA DEVE responder `403`.
4. QUANDO o cliente chamar `GET /me` com token válido, O SISTEMA DEVE devolver `sub`, `preferred_username`, `roles`, `scopes` e `exp`.
5. QUANDO o access token expirar, O SISTEMA DEVE responder `401` e o cliente DEVE reiniciar o fluxo de login (ou usar refresh token).
6. ENQUANTO a plataforma estiver no ar, O SISTEMA DEVE manter `/health/*` e `/app` acessíveis de forma anônima.
7. QUANDO o usuário acionar logout, O SISTEMA DEVE encerrar a sessão no Keycloak (`end_session`) e redirecionar para `/app`.

## Critérios de aceite
- [ ] Login de `alice` e `bob` conclui e retorna token com `roles`, `scope` e `aud=orderhub-api`.
- [ ] Matriz de autorização (anônimo, alice, bob, orderhub-cli) × endpoints devolve os status 401/403/200 esperados.
- [ ] `GET /me` devolve os campos exigidos; sem token devolve `401`.
- [ ] Refresh renova o access token; logout encerra a sessão.
- [ ] Projeto `OrderHub.BuildingBlocks.Auth` compila e é usado por Gateway e Catalog (sem configuração JwtBearer duplicada).
- [ ] As rotas do YARP não capturam `/app` nem `/me`.

## Evidência / experimento
- **Hipótese:** tokens já emitidos continuam válidos nas APIs mesmo com o Keycloak fora do ar (chaves públicas em cache), enquanto novos logins falham; e a expiração é respeitada considerando o `ClockSkew`.
- **Procedimento:** (1) parar o Keycloak e repetir chamadas com um token ainda válido e tentar um novo login; (2) reduzir a vida do access token para 30 s e o `ClockSkew` para 30 s, medir o tempo até a rejeição após expirar.
- **Métrica de sucesso:** chamadas com token em cache seguem 200 com Keycloak parado; novo login falha; após expiração + skew, a API passa a responder 401.
- **Relatório:** `docs/experimentos/02a-login-oauth.md`

## Fora de escopo
- CRUD do Catalog com banco (fase 02b) e serviço Inventory (fase 02c).
- mTLS entre serviços, rotação de segredos, front-end elaborado.
- Implementação de `IEventBus`/eventos (fase 03).
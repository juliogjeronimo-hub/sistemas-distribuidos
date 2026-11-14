# feat(auth): implementar OAuth 2.0 com JWT em OrderHub

## Descrição
Implementa autenticação OAuth 2.0 com Keycloak e validação JWT nos microsserviços (Gateway e Catalog).

## Alterações
- Unifica implementação de JWT em BuildingBlocks (remove `MetadataAddress` duplicado)
- Catalog passa a usar `AddOrderHubJwtAuth` (corrige bug em `AddCatalogAuth`)
- Configura Docker Compose com serviços .NET e variáveis de ambiente OAuth
- Reorganiza Postman collections para `docs/testing/postman/` por microsserviço
- Atualiza `.gitignore` para ignorar exportações Postman

## Testes
- Gateway + Catalog + Keycloak funcionando em Docker
- Autenticação OAuth 2.0 (client_credentials) testada com Postman
- Collections em `docs/testing/postman/` prontas para uso

## Conceitos exercitados
- Invocação remota (OAuth 2.0 / OIDC)
- Segurança (autenticação + validação de token)
- Middleware e pipeline HTTP

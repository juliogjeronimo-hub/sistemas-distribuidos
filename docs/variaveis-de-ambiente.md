# Variáveis de ambiente para hospedagem

Lista das variáveis necessárias para rodar o OrderHub fora do ambiente de desenvolvimento local. Levantada a partir de `deploy/docker-compose*.yml`, dos `appsettings.json` e do código que lê configuração (`AuthenticationExtensions`, `ServiceCollectionExtensions` do Catalog, `Program.cs` do Gateway).

Segredos (senhas, connection strings) devem vir de secrets do Swarm ou do provedor de hospedagem, nunca do repositório nem da imagem.

## Convenção de nomes (.NET)

O ASP.NET Core lê seções aninhadas com `__` (dois sublinhados): `Auth__ValidIssuer` equivale a `Auth:ValidIssuer`. Os compose atuais usam `:`, que funciona no Docker em Linux, mas `__` funciona em qualquer host (Swarm, Kubernetes, Azure, Linux e Windows). Prefira `__` na hospedagem.

## Gateway (`OrderHub.Gateway`)

| Variável | Obrigatória | Exemplo | Descrição |
|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | Sim | `Production` | Ambiente da aplicação. Não usar `Development` em hospedagem. |
| `Auth__ValidIssuer` | Sim | `https://auth.exemplo.com/realms/orderhub` | URL do realm no Keycloak. É usada como `Authority`: o serviço baixa dela o `/.well-known/openid-configuration` e as chaves de assinatura. Precisa ser acessível a partir do container. |
| `Auth__Audience` | Sim | `orderhub-api` | Valor esperado no claim `aud` do token. |
| `Auth__RequireHttpsMetadata` | Sim | `true` | Exige HTTPS para buscar os metadados. `false` só é aceitável no compose de dev. |
| `ReverseProxy__Clusters__catalog__Destinations__d1__Address` | Não | `http://catalog:8080/` | Endereço interno do Catalog. O padrão do `appsettings.json` é `http://catalog:8080/`; só mude se o nome ou a porta do serviço forem outros. |
| `ASPNETCORE_HTTP_PORTS` | Não | `8080` | Porta HTTP dentro do container. A imagem `aspnet` já usa `8080` por padrão. |

## Catalog (`OrderHub.Catalog.Api`)

| Variável | Obrigatória | Exemplo | Descrição |
|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | Sim | `Production` | Ver observação sobre migrations abaixo. |
| `ConnectionStrings__Catalog` | Sim (secret) | `Host=postgres-catalog;Port=5432;Database=orderhub_catalog;Username=orderhub;Password=<secret>` | Connection string do PostgreSQL do Catalog. O valor do `appsettings.json` (`Password=CHANGE_ME`) é só um placeholder. |
| `Auth__ValidIssuer` | Sim | `https://auth.exemplo.com/realms/orderhub` | Igual ao do Gateway. Cada serviço valida o token de novo. |
| `Auth__Audience` | Sim | `orderhub-api` | Igual ao do Gateway. |
| `Auth__RequireHttpsMetadata` | Sim | `true` | Igual ao do Gateway. |
| `ASPNETCORE_HTTP_PORTS` | Não | `8080` | Porta HTTP dentro do container. |

## PostgreSQL do Catalog (`postgres-catalog`)

| Variável | Obrigatória | Exemplo | Descrição |
|---|---|---|---|
| `POSTGRES_USER` | Sim | `orderhub` | Usuário criado na primeira inicialização do volume. |
| `POSTGRES_PASSWORD` | Sim (secret) | `<secret>` | Senha desse usuário. Deve bater com a `ConnectionStrings__Catalog`. |
| `POSTGRES_DB` | Sim | `orderhub_catalog` | Banco criado na primeira inicialização. |

Os compose leem `POSTGRES_USER` e `POSTGRES_PASSWORD` do ambiente de quem executa o `docker compose` (ou de um arquivo `.env` em `deploy/`). Sem elas, o valor padrão é `orderhub`/`orderhub`, que não deve ir para hospedagem.

## Keycloak

| Variável | Obrigatória | Exemplo | Descrição |
|---|---|---|---|
| `KC_HOSTNAME` | Sim | `https://auth.exemplo.com` | URL pública do Keycloak. Define o `iss` dos tokens, que os serviços validam. |
| `KC_BOOTSTRAP_ADMIN_USERNAME` | Sim (secret) | `admin` | Admin inicial. Usado só na primeira inicialização. |
| `KC_BOOTSTRAP_ADMIN_PASSWORD` | Sim (secret) | `<secret>` | Senha do admin inicial. |
| `KC_HEALTH_ENABLED` | Sim | `true` | Habilita `/health/ready` na porta 9000, usado pelo healthcheck do compose. |
| `KC_HOSTNAME_BACKCHANNEL_DYNAMIC` | Sim | `true` | Permite que os containers acessem o Keycloak pelo endereço interno (`http://keycloak:8080`) mantendo o `iss` público. |
| `KC_HTTP_ENABLED` | Depende | `true` | Necessário quando o TLS termina em um proxy reverso na frente do Keycloak. |
| `KC_PROXY_HEADERS` | Depende | `xforwarded` | Necessário atrás de proxy reverso, para o Keycloak respeitar `X-Forwarded-*`. |
| `KC_DB` | Recomendada | `postgres` | Banco do Keycloak. Sem ela, o Keycloak usa um H2 interno. |
| `KC_DB_URL` | Recomendada | `jdbc:postgresql://postgres-keycloak:5432/keycloak` | URL JDBC do banco do Keycloak. |
| `KC_DB_USERNAME` | Recomendada | `keycloak` | Usuário do banco do Keycloak. |
| `KC_DB_PASSWORD` | Recomendada (secret) | `<secret>` | Senha do banco do Keycloak. |

Hoje o compose sobe o Keycloak com `start-dev` e H2 interno. Para hospedagem, use `start` (modo produção) com as variáveis `KC_DB*` e um PostgreSQL próprio, senão usuários e sessões se perdem quando o container é recriado.

## RabbitMQ

| Variável | Obrigatória | Exemplo | Descrição |
|---|---|---|---|
| `RABBITMQ_DEFAULT_USER` | Sim (secret) | `orderhub` | Usuário criado na primeira inicialização. |
| `RABBITMQ_DEFAULT_PASS` | Sim (secret) | `<secret>` | Senha desse usuário. |

Nenhum serviço .NET usa o RabbitMQ ainda. Quando o `IEventBus` for implementado, os serviços vão precisar das credenciais de conexão (host, usuário, senha).

## Grafana

| Variável | Obrigatória | Exemplo | Descrição |
|---|---|---|---|
| `GF_SECURITY_ADMIN_USER` | Sim (secret) | `admin` | Admin do Grafana. |
| `GF_SECURITY_ADMIN_PASSWORD` | Sim (secret) | `<secret>` | Senha do admin. |

Redis e Prometheus não precisam de variáveis de ambiente na configuração atual. O Prometheus lê `deploy/prometheus/prometheus.yml`.

## Observações

- **Migrations em produção:** `MigrationExtensions.ApplyMigrations` só roda `Database.Migrate()` quando `ASPNETCORE_ENVIRONMENT=Development`. Com `Production`, o schema do Catalog não é criado sozinho. Aplique as migrations antes do deploy (por exemplo, com `dotnet ef database update` ou um bundle gerado por `dotnet ef migrations bundle`).
- **Chave `Auth:Authority` sem efeito:** o `appsettings.json` do Catalog define `Auth:Authority`, mas o `AuthOptions` usado pelo `AddOrderHubJwtAuth` lê `Auth:ValidIssuer`. Configure `Auth__ValidIssuer`; a chave `Authority` é ignorada.
- **Seção `Telemetry`:** o `appsettings.json` do Catalog tem `Telemetry:ServiceName` e `Telemetry:ServiceVersion`, mas nenhum código lê essa seção ainda.

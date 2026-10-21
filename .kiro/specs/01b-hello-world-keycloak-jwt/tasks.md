# Tarefas 01b: Hello World Keycloak + JWT

**Objetivo:** proteger o OrderHub com OAuth 2.0 via Keycloak. Gateway e Catalog validam JWT. Resultado esperado: `GET /catalog/v1/products` sem token = **401**; com token (client_credentials) = **200**; `/health/*` continua anônimo.

**Pré-requisito:** spec `01a-hello-gateway-catalog` concluída (catalog e gateway sobem e respondem).

**Ambiente:** Windows + PowerShell, .NET 10, Docker Compose. Usar `curl.exe`. Código, comentários e logs em inglês.

---

## Protocolo de execução (ler antes de começar)

1. **Retomada:** leia **apenas este arquivo**. Vá à primeira tarefa com `[ ]`. Antes de executá-la, rode a *Checagem* dela: se já estiver feita, marque `[x]` e passe para a próxima.

2. **Uma tarefa por vez.** Ao concluir e verificar, marque `[x]` **imediatamente** neste arquivo e faça o commit indicado.

3. **Economia de créditos:** sem spec nova, sem documentação, sem testes, sem refatoração, sem pacotes extras, sem ler arquivos fora dos citados. Escreva cada arquivo uma única vez, completo.

4. **Falhas:** no máximo **2 tentativas** de correção por tarefa. Se persistir, escreva o erro (só o trecho relevante) em *Registro de execução*, marque `[!]` e **pare**.

5. **Falha de conexão/interrupção:** ao retomar, comece pela *Checagem* da primeira tarefa pendente (ou `[!]`). Não refaça tarefas `[x]`.

6. **Não altere** Dockerfiles, configuração do YARP nem a estrutura de projetos.

7. **Resposta final:** no máximo 6 linhas.

Legenda: `[ ]` pendente · `[x]` concluída · `[!]` bloqueada (ver registro)

---

## Caminhos reais (preencher na Tarefa 0)

- Program.cs do Api do Catalog: `src/Catalog/OrderHub.Catalog.Api/Program.cs`

- csproj do Api do Catalog: `src/Catalog/OrderHub.Catalog.Api/OrderHub.Catalog.Api.csproj`

- Program.cs do Gateway: `src/Gateway/OrderHub.Gateway/Program.cs`

- csproj do Gateway: `src/Gateway/OrderHub.Gateway/OrderHub.Gateway.csproj`

- Compose(s) em `deploy/`: `deploy/docker-compose.yml`, `deploy/docker-compose.services.yml`, `deploy/docker-compose.override.yml`

---

## Tarefas

- [x] **0. Pré-requisito e caminhos**

  - `Get-ChildItem -Recurse -Filter *.csproj | Select-Object -ExpandProperty FullName` e preencha *Caminhos reais*.

  - Confirme a etapa 01a: `curl.exe -s -o NUL -w "%{http_code}`n" http://localhost:5000/catalog/v1/product `deve retornar` 20 `(suba com` docker compose up -d` se necessário). Se não funcionar, **pare** e registre.

  - *Checagem:* a seção *Caminhos reais* não contém `...`.

- [x] **1. Realm do Keycloak**

  - Crie `deploy/keycloak/orderhub-realm.json` (valores de DEV; o secret é só para desenvolvimento local):

    ```json

    {

      "realm": "orderhub",

      "enabled": true,

      "sslRequired": "none",

      "clientScopes": [

        {

          "name": "catalog.read",

          "protocol": "openid-connect",

          "attributes": {

            "include.in.token.scope": "true",

            "display.on.consent.screen": "false"

          },

          "protocolMappers": [

            {

              "name": "orderhub-api-audience",

              "protocol": "openid-connect",

              "protocolMapper": "oidc-audience-mapper",

              "config": {

                "included.custom.audience": "orderhub-api",

                "access.token.claim": "true",

                "id.token.claim": "false"

              }

            }

          ]

        }

      ],

      "clients": [

        {

          "clientId": "orderhub-cli",

          "enabled": true,

          "protocol": "openid-connect",

          "publicClient": false,

          "secret": "orderhub-cli-dev-secret",

          "serviceAccountsEnabled": true,

          "standardFlowEnabled": false,

          "directAccessGrantsEnabled": false,

          "defaultClientScopes": ["catalog.read"],

          "optionalClientScopes": []

        }

      ]

    }

    ```

  - *Checagem:* `Test-Path deploy/keycloak/orderhub-realm.json`

  - Commit: `feat(auth): adiciona realm orderhub do Keycloak`

- [x] **2. Compose: serviço Keycloak**

  - Adicione o serviço (mantenha `catalog` e `gateway` ativos; demais continuam comentados):

    ```yaml

      keycloak:

        image: quay.io/keycloak/keycloak:26.0

        container_name: orderhub-keycloak

        command: ["start-dev", "--import-realm"]

        environment:

          KC_BOOTSTRAP_ADMIN_USERNAME: admin      # dev only

          KC_BOOTSTRAP_ADMIN_PASSWORD: admin      # dev only

          KC_HOSTNAME: http://localhost:8180      # stable issuer for tokens issued via the host

          KC_HOSTNAME_BACKCHANNEL_DYNAMIC: "true" # containers reach Keycloak as keycloak:8080

          KC_HTTP_ENABLED: "true"

        volumes:

          - ./keycloak/orderhub-realm.json:/opt/keycloak/data/import/orderhub-realm.json:ro

        ports:

          - "8180:8080"

    ```

  - Ajuste o caminho do volume conforme a localização real do compose. **Sem** healthcheck no keycloak (a imagem não tem curl) e **sem** `depends_on` nele.

  - *Checagem:* `docker compose -f <compose(s)> config --services` inclui `keycloak`.

  - Commit: `feat(infra): adiciona keycloak ao compose`

- [x] **3. Compose: variáveis de autenticação em `catalog` e `gateway`**

  - Em `environment` de **ambos**:

    ```yaml

          Auth__MetadataAddress: http://keycloak:8080/realms/orderhub/.well-known/openid-configuration

          Auth__ValidIssuer: http://localhost:8180/realms/orderhub

          Auth__Audience: orderhub-api

          Auth__RequireHttpsMetadata: "false"

    ```

  - *Checagem:* `Select-String -Path <compose(s)> -Pattern "Auth__Audience"` retorna 2 ocorrências.

  - Commit: `feat(auth): configura variáveis de autenticação nos serviços`

- [x] **4. Catalog: pacote JwtBearer**

  - `dotnet add <csproj do Api do Catalog> package Microsoft.AspNetCore.Authentication.JwtBearer`

  - *Checagem:* `Select-String -Path <csproj do Catalog Api> -Pattern "JwtBearer"` retorna resultado.

- [x] **5. Catalog: JWT e policy no `Program.cs`**

  - Adicione `using Microsoft.AspNetCore.Authentication.JwtBearer;` e `using Microsoft.IdentityModel.Tokens;`.

  - Antes de `builder.Build()`:

    ```csharp

    var auth = builder.Configuration.GetSection("Auth");

    builder.Services

        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)

        .AddJwtBearer(o =>

        {

            o.MetadataAddress = auth["MetadataAddress"]!;

            o.RequireHttpsMetadata = bool.Parse(auth["RequireHttpsMetadata"] ?? "true");

            o.MapInboundClaims = false;

            o.TokenValidationParameters = new TokenValidationParameters

            {

                ValidIssuer = auth["ValidIssuer"],

                ValidAudience = auth["Audience"],

                ValidateIssuer = true,

                ValidateAudience = true,

                ValidateLifetime = true,

                ValidateIssuerSigningKey = true

            };

        });

    builder.Services.AddAuthorizationBuilder()

        .AddPolicy("catalog.read", p => p.RequireAssertion(ctx =>

            // Keycloak sends "scope" as a single space-separated string.

            ctx.User.FindFirst("scope")?.Value.Split(' ').Contains("catalog.read") == true));

    ```

  - Depois de `Build()`: `app.UseAuthentication(); app.UseAuthorization();` **antes** dos mapeamentos.

  - `/v1/products` recebe `.RequireAuthorization("catalog.read")`. `/health/*` permanecem **sem** `RequireAuthorization`.

  - *Checagem:* `Select-String -Path <Program.cs do Catalog> -Pattern "RequireAuthorization"` retorna resultado.

  - Commit: `feat(catalog): valida JWT e protege endpoint de produtos`

- [x] **6. Gateway: pacote JwtBearer**

  - `dotnet add <csproj do Gateway> package Microsoft.AspNetCore.Authentication.JwtBearer`

  - *Checagem:* `Select-String -Path <csproj do Gateway> -Pattern "JwtBearer"` retorna resultado.

- [x] **7. Gateway: JWT no `Program.cs`**

  - Mesma configuração de `AddAuthentication/AddJwtBearer` da Tarefa 5 (sem a policy `catalog.read`), mais `builder.Services.AddAuthorization();`, e os mesmos `usings`.

  - `app.UseAuthentication(); app.UseAuthorization();` antes dos mapeamentos.

  - `app.MapReverseProxy().RequireAuthorization();` e mantenha `app.MapHealthChecks("/health/live");` **sem** autorização. O YARP repassa o header `Authorization` ao Catalog por padrão.

  - *Checagem:* `Select-String -Path <Program.cs do Gateway> -Pattern "MapReverseProxy\(\).RequireAuthorization"` retorna resultado.

  - Commit: `feat(gateway): valida JWT e exige autorização no proxy`

- [~] **8. Subir e aguardar o Keycloak**

  - `docker compose -f <compose(s)> up --build -d`

  - Repita a cada 5 s, até 12 vezes, até retornar `200` (o Keycloak leva 30–60 s):

    `curl.exe -s -o NUL -w "%{http_code}`n" http://localhost:8180/realms/orderhub/.well-known/openid-configuration`

  - Se não chegar a `200`: `docker compose -f <compose(s)> logs keycloak --tail 40` e registre.

- [~] **9. Validar token e chamadas**

  - ```powershell

    $token = (Invoke-RestMethod -Method Post `

      -Uri "http://localhost:8180/realms/orderhub/protocol/openid-connect/token" `

      -Body @{ grant_type="client_credentials"; client_id="orderhub-cli"; client_secret="orderhub-cli-dev-secret" }).access_token

    curl.exe -s -o NUL -w "%{http_code}`n" http://localhost:5000/catalog/v1/products

    curl.exe -s -w `n%{http_code}`n" -H "Authorization: Bearer $token" http://localhost:5000/catalog/v1/products

    curl.exe -s -o NUL -w "%{http_code}`n" http://localhost:5000/health/live

    ```

  - *Critério de sucesso:* `401`; JSON + `200`; `200`.

- [~] **10. Diagnóstico (executar SOMENTE se a Tarefa 9 falhar)**

  - Nesta ordem, sem outras investigações:

    1. Decodifique o payload do token e confira `iss` (= `http://localhost:8180/realms/orderhub`), `aud` (contém `orderhub-api`) e `scope` (contém `catalog.read`):

       ```powershell

       $p=$token.Split('.')[1]; $p=$p.PadRight($p.Length+(4-$p.Length%4)%4,'='); [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($p.Replace('-','+').Replace('_','/')))

       ```

    2. `docker compose -f <compose(s)> logs gateway --tail 30` e `... logs catalog --tail 30`.

  - Interpretação:

    - *issuer inválido* → revisar `KC_HOSTNAME` e `Auth__ValidIssuer`.

    - *audience inválida* → realm importado sem o mapper: recrie o container do keycloak `docker compose -f <compose(s)> up -d --force-recreate keycloak`).

    - *401 com token válido* → serviço sem as variáveis `Auth__*` ou sem `UseAuthentication()` antes dos mapeamentos.

  - Depois do ajuste, repita a Tarefa 9.

- [~] **11. Fechamento**

  - Resumo de até 6 linhas: arquivos alterados + resultado das 3 chamadas.

  - Commit final, se restar algo: `chore: conclui hello world de autenticação com keycloak`

---

## Registro de execução

<!-- Anote: última tarefa concluída, erros (trecho relevante), decisões. Mantenha curto. -->

| Data/hora | Tarefa | Situação | Observação |

|---|---|---|---|

| 2025 | 0 | concluída | Caminhos reais preenchidos; `GET /catalog/v1/products` retornou 200 (01a OK). |

| 2025 | 2 | concluída | Keycloak adicionado em `docker-compose.yml`; `config --services` lista `keycloak`. |
| 2025 | 3 | concluída | Variáveis `Auth__*` adicionadas em `catalog` e `gateway`; `Auth__Audience` com 2 ocorrências. |
| 2025 | 4 | concluída | Pacote `Microsoft.AspNetCore.Authentication.JwtBearer` (10.0.0) já presente no csproj; checagem `JwtBearer` retornou resultado. Sem commit (vai com a Tarefa 5). |
| 2025 | 5 | concluída | `Program.cs` com `AddJwtBearer`, policy `catalog.read` e `UseAuthentication/UseAuthorization` antes dos mapeamentos; `/v1/products` com `RequireAuthorization`, `/health/*` anônimos. Checagem `RequireAuthorization` OK. Build: terminal interrompia o `dotnet build` (exit -1, logs truncados), mas a compilação progrediu sem erros e o `JwtBearer.dll` foi resolvido no `bin`; marcador `.Up2Date` presente. |
| 2025 | 6 | concluída | Pacote `Microsoft.AspNetCore.Authentication.JwtBearer` (10.0.0) já presente no csproj do Gateway; checagem `JwtBearer` retornou resultado. Sem commit (vai com a Tarefa 7). |
| 2025 | 7 | concluída | `Program.cs` do Gateway com `AddAuthentication/AddJwtBearer` (sem policy), `AddAuthorization()`, `UseAuthentication/UseAuthorization` antes dos mapeamentos; `MapReverseProxy().RequireAuthorization()`, `/health/*` anônimos. Checagem `MapReverseProxy().RequireAuthorization` OK (linha 43). Build: "Compilação com êxito, 0 Erro(s)"; `OrderHub.Gateway.dll` e dlls `JwtBearer`/`IdentityModel` presentes no `bin`. |

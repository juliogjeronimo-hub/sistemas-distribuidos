# Tarefas 01a: Hello World Gateway → Catalog (sem autenticação)

**Objetivo:** rodar `docker compose up --build` e chamar `GET /catalog/v1/products` pelo Gateway (YARP), que encaminha ao Catalog. Sem autenticação nesta etapa. Não implemente nada além do descrito.

**Ambiente:** Windows + PowerShell, .NET 10, Docker Compose. Usar `curl.exe` (não `curl`). Código, comentários e logs em inglês.

---

## Protocolo de execução (ler antes de começar)

1. **Retomada:** leia **apenas este arquivo**. Vá à primeira tarefa com `[ ]`. Antes de executá-la, rode o comando de *Checagem* dela: se já estiver feita, marque `[x]` e passe para a próxima.
2. **Uma tarefa por vez.** Ao concluir e verificar, marque `[x]` **imediatamente** neste arquivo e faça o commit indicado.
3. **Economia de créditos:** sem spec nova, sem documentação, sem testes, sem refatoração, sem pacotes extras, sem ler arquivos fora dos citados. Escreva cada arquivo uma única vez, completo.
4. **Falhas:** no máximo **2 tentativas** de correção por tarefa. Se persistir, escreva o erro (só o trecho relevante) em *Registro de execução*, marque a tarefa como `[!]` e **pare**.
5. **Falha de conexão/interrupção:** ao retomar, comece pela *Checagem* da primeira tarefa pendente (ou `[!]`). Não refaça tarefas marcadas `[x]`.
6. **Resposta final:** no máximo 6 linhas.

Legenda: `[ ]` pendente · `[x]` concluída · `[!]` bloqueada (ver registro)

---

## Caminhos reais (preencher na Tarefa 0)

- Api do Catalog (csproj): `src/Catalog/OrderHub.Catalog.Api/OrderHub.Catalog.Api.csproj`
- Application do Catalog (arquivo do `AddApplication`): `src/Catalog/OrderHub.Catalog.Application/DependencyInjection/ApplicationServiceCollectionExtensions.cs` (método `AddApplicationServices`)
- Api do Gateway (csproj): `src/Gateway/OrderHub.Gateway/OrderHub.Gateway.csproj`
- Dockerfile do Catalog: `src/Catalog/OrderHub.Catalog.Api/Dockerfile`
- Dockerfile do Gateway: `src/Gateway/OrderHub.Gateway/Dockerfile`
- Compose(s) em `deploy/`: `deploy/docker-compose.yml`, `deploy/docker-compose.services.yml`, `deploy/docker-compose.override.yml`
- Nome da dll do Catalog Api: `OrderHub.Catalog.Api.dll`
- Nome da dll do Gateway: `OrderHub.Gateway.dll`

---

## Tarefas

- [x] **0. Mapear caminhos reais**
  - Execute: `Get-ChildItem -Recurse -Filter *.csproj | Select-Object -ExpandProperty FullName`
  - Localize os Dockerfiles do catalog e do gateway e o(s) compose em `deploy/`.
  - Preencha a seção *Caminhos reais* acima. Os caminhos podem ser `src/Catalog/...` ou `src/services/catalog/...`.
  - *Checagem:* a seção *Caminhos reais* não contém `...`.
  - Commit: não necessário.

- [x] **1. DI da camada Application do Catalog**
  - No arquivo que contém `AddApplication`, garanta esta lógica (se já estiver assim, não altere):
    - Primeiro `services.Scan(...)` registrando `ICommandHandler<>` e `IQueryHandler<,>` com `publicOnly: false`, `.AsImplementedInterfaces()`, `.WithScopedLifetime()`.
    - Depois, decorators **somente** com `services.TryDecorate(...)` (nunca `Decorate`: lança `DecorationException` quando não há handlers).
    - Se não houver tipo marcador, crie `ApplicationAssemblyMarker` no projeto Application.
  - *Checagem:* `Select-String -Path <arquivo do AddApplication> -Pattern "TryDecorate"` retorna resultado e não há `.Decorate(`.
  - Commit: `fix(catalog): usa TryDecorate no registro da camada Application`

- [x] **2. Catalog: `Program.cs` do projeto Api**
  - Substitua o conteúdo por:
    ```csharp
    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddApplication();
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    app.MapHealthChecks("/health/live");
    app.MapHealthChecks("/health/ready");

    app.MapGet("/v1/products", () => new[]
    {
        new { Id = Guid.NewGuid(), Name = "Keyboard", Price = 199.90m },
        new { Id = Guid.NewGuid(), Name = "Mouse", Price = 89.90m }
    });

    app.Run();
    ```
  - Sem `UseHttpsRedirection`, sem autenticação, sem banco. **Não** chame `AddInfrastructure` nesta etapa. Ajuste `usings` apenas se necessário para compilar.
  - *Checagem:* `Select-String -Path <Program.cs do Catalog> -Pattern "/v1/products"` retorna resultado.
  - Commit: `feat(catalog): adiciona endpoint de produtos e health checks`

- [~] **3. Catalog: Dockerfile (estágio final)**
  - Mantenha os estágios `build` e `publish`. O estágio final fica:
    ```dockerfile
    FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

    # curl is only needed for the container healthcheck (aspnet image ships without it).
    USER root
    RUN apt-get update \
        && apt-get install -y --no-install-recommends curl \
        && rm -rf /var/lib/apt/lists/*

    WORKDIR /app
    COPY --from=publish /app/publish .

    # Built-in non-root user of official .NET images. Do NOT use adduser (not available).
    USER $APP_UID
    EXPOSE 8080
    ENTRYPOINT ["dotnet", "<DLL do Catalog Api>.dll"]
    ```
  - Remova qualquer `RUN adduser` e `--chown=appuser`. Não use `ASPNETCORE_URLS` na porta 80.
  - *Checagem:* `Select-String -Path <Dockerfile do Catalog> -Pattern "adduser"` não retorna nada e há `USER $APP_UID`.
  - Commit: `fix(catalog): usa usuário não-root embutido e instala curl no Dockerfile`

- [~] **4. Gateway: pacote, `Program.cs` e `appsettings.json`**
  - Pacote: `dotnet add <csproj do Gateway> package Yarp.ReverseProxy`
  - `Program.cs`:
    ```csharp
    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddReverseProxy()
        .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    app.MapHealthChecks("/health/live");
    app.MapReverseProxy();

    app.Run();
    ```
  - `appsettings.json` (preserve a seção `Logging` existente, se houver):
    ```json
    {
      "ReverseProxy": {
        "Routes": {
          "catalog": {
            "ClusterId": "catalog",
            "Match": { "Path": "/catalog/{**rest}" },
            "Transforms": [ { "PathRemovePrefix": "/catalog" } ]
          }
        },
        "Clusters": {
          "catalog": {
            "Destinations": { "d1": { "Address": "http://catalog:8080/" } }
          }
        }
      }
    }
    ```
  - *Checagem:* `Select-String -Path <csproj do Gateway> -Pattern "Yarp.ReverseProxy"` retorna resultado.
  - Commit: `feat(gateway): configura YARP com rota para o catalog`

- [~] **5. Gateway: Dockerfile**
  - Mesmo padrão do Catalog: multi-stage, `restore` em camada separada copiando só os `.csproj` necessários, estágio final com `curl` + `USER $APP_UID` + `EXPOSE 8080`, apontando para o csproj e a dll do Gateway. Contexto de build = raiz do repositório.
  - *Checagem:* `Select-String -Path <Dockerfile do Gateway> -Pattern "APP_UID"` retorna resultado.
  - Commit: `feat(gateway): adiciona Dockerfile multi-stage`

- [~] **6. `.dockerignore` na raiz do repositório**
  - Se não existir, crie:
    ```
    **/bin/
    **/obj/
    **/.vs/
    .git/
    .kiro/
    docs/
    labs/
    *.md
    ```
  - *Checagem:* `Test-Path .dockerignore`
  - Commit: `chore(infra): adiciona dockerignore`

- [~] **7. Compose (em `deploy/`)**
  - Deixe **apenas** `catalog` e `gateway` ativos. Comente (não apague) os demais serviços (keycloak, rabbitmq, prometheus, grafana, postgres...).
  - `catalog`: `build.context: ..`, `dockerfile:` com o caminho real, `container_name: orderhub-catalog`, sem `ASPNETCORE_URLS`, `ports: ["5001:8080"]`, healthcheck:
    `test: ["CMD", "curl", "-fsS", "http://localhost:8080/health/live"]`, `interval: 10s`, `timeout: 3s`, `retries: 5`, `start_period: 20s`.
  - `gateway`: build análogo, `container_name: orderhub-gateway`, `ports: ["5000:8080"]`, `depends_on: catalog: condition: service_healthy`, mesmo healthcheck.
  - Ambos na rede padrão do compose.
  - *Checagem:* `docker compose -f <compose(s)> config --services` lista apenas `catalog` e `gateway`.
  - Commit: `feat(infra): compose com catalog e gateway`

- [~] **8. Subir o ambiente**
  - `docker compose -f <compose(s)> up --build -d`
  - `docker compose -f <compose(s)> ps`
  - *Critério:* ambos os containers `healthy`.
  - Se falhar: `docker compose -f <compose(s)> logs <servico> --tail 40` (somente do serviço com problema). Nada além disso.

- [~] **9. Validar as chamadas**
  - ```powershell
    curl.exe -s -o NUL -w "%{http_code}`n" http://localhost:5000/health/live
    curl.exe -s http://localhost:5000/catalog/v1/products
    curl.exe -s -o NUL -w "%{http_code}`n" http://localhost:5000/catalog/health/live
    ```
  - *Critério de sucesso:* `200`; JSON com 2 produtos; `200`.

- [~] **10. Fechamento**
  - Resumo de até 6 linhas: arquivos alterados + resultado das 3 chamadas.
  - Commit final, se restar algo: `chore: conclui hello world gateway e catalog`

---

## Registro de execução

<!-- Use este espaço para anotar: última tarefa concluída, erros (trecho relevante), decisões. Mantenha curto. -->

| Data/hora | Tarefa | Situação | Observação |
|---|---|---|---|
| 2025 | 1 | concluída | Já estava correto: `Scan` com `publicOnly: false` + `TryDecorate` (4x), sem `.Decorate(`. `ApplicationAssemblyMarker` já existe. Sem alterações, sem commit. |
| 2025 | 2 | concluída | `Program.cs` já estava conforme a spec, usando `AddApplicationServices()`, health checks e `/v1/products`. Checagem OK e `dotnet build` com êxito (0 avisos, 0 erros). Sem alteração no arquivo. |

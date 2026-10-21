# ─── Stage 1: build ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./

COPY src/Catalog/OrderHub.Catalog.Domain/OrderHub.Catalog.Domain.csproj \
     src/Catalog/OrderHub.Catalog.Domain/
COPY src/Catalog/OrderHub.Catalog.Application/OrderHub.Catalog.Application.csproj \
     src/Catalog/OrderHub.Catalog.Application/
COPY src/Catalog/OrderHub.Catalog.Infrastructure/OrderHub.Catalog.Infrastructure.csproj \
     src/Catalog/OrderHub.Catalog.Infrastructure/
COPY src/Catalog/OrderHub.Catalog.Api/OrderHub.Catalog.Api.csproj \
     src/Catalog/OrderHub.Catalog.Api/

RUN dotnet restore src/Catalog/OrderHub.Catalog.Api/OrderHub.Catalog.Api.csproj

COPY src/ src/

RUN dotnet build src/Catalog/OrderHub.Catalog.Api/OrderHub.Catalog.Api.csproj \
    --no-restore \
    --configuration Release

# ─── Stage 2: publish ─────────────────────────────────────────────────────────
FROM build AS publish

RUN dotnet publish src/Catalog/OrderHub.Catalog.Api/OrderHub.Catalog.Api.csproj \
    --no-build \
    --configuration Release \
    --self-contained false \
    --output /app/publish

# ─── Stage 3: final ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY --from=publish /app/publish .

USER $APP_UID

EXPOSE 8080

ENTRYPOINT ["dotnet", "OrderHub.Catalog.Api.dll"]

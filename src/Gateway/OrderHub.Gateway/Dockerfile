# ─── Stage 1: build ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./

COPY src/Gateway/OrderHub.Gateway/OrderHub.Gateway.csproj \
     src/Gateway/OrderHub.Gateway/

RUN dotnet restore src/Gateway/OrderHub.Gateway/OrderHub.Gateway.csproj

COPY src/ src/

RUN dotnet build src/Gateway/OrderHub.Gateway/OrderHub.Gateway.csproj \
    --no-restore \
    --configuration Release

# ─── Stage 2: publish ─────────────────────────────────────────────────────────
FROM build AS publish

RUN dotnet publish src/Gateway/OrderHub.Gateway/OrderHub.Gateway.csproj \
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

ENTRYPOINT ["dotnet", "OrderHub.Gateway.dll"]

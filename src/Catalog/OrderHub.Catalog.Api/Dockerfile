# ─── Stage 1: build ───────────────────────────────────────────────────────────
# Build context is the repository root so we can reach Directory.Build.props
# and every project that Catalog depends on.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the global props first — it carries Nullable, TreatWarningsAsErrors, etc.
# Copying it before the .csproj files keeps this layer cached as long as the
# file hasn't changed.
COPY Directory.Build.props ./

# Copy only the project files to take advantage of Docker layer caching.
# dotnet restore is slow; isolating it in its own layer means a source-only
# change won't invalidate the NuGet cache layer.
COPY src/Catalog/OrderHub.Catalog.Domain/OrderHub.Catalog.Domain.csproj \
     src/Catalog/OrderHub.Catalog.Domain/
COPY src/Catalog/OrderHub.Catalog.Application/OrderHub.Catalog.Application.csproj \
     src/Catalog/OrderHub.Catalog.Application/
COPY src/Catalog/OrderHub.Catalog.Infrastructure/OrderHub.Catalog.Infrastructure.csproj \
     src/Catalog/OrderHub.Catalog.Infrastructure/
COPY src/Catalog/OrderHub.Catalog.Api/OrderHub.Catalog.Api.csproj \
     src/Catalog/OrderHub.Catalog.Api/

# When building-blocks projects are created, add their .csproj lines here:
# COPY src/building-blocks/OrderHub.BuildingBlocks.Contracts/OrderHub.BuildingBlocks.Contracts.csproj \
#      src/building-blocks/OrderHub.BuildingBlocks.Contracts/
# COPY src/building-blocks/OrderHub.BuildingBlocks.Messaging/OrderHub.BuildingBlocks.Messaging.csproj \
#      src/building-blocks/OrderHub.BuildingBlocks.Messaging/

# Restore using the Api project as entry point; NuGet resolves the full
# dependency graph automatically.
RUN dotnet restore src/Catalog/OrderHub.Catalog.Api/OrderHub.Catalog.Api.csproj

# Copy all source after the restore layer so that code changes don't bust the
# NuGet cache.
COPY src/ src/

# Build in Release without re-running restore (already done above).
RUN dotnet build src/Catalog/OrderHub.Catalog.Api/OrderHub.Catalog.Api.csproj \
    --no-restore \
    --configuration Release

# ─── Stage 2: publish ─────────────────────────────────────────────────────────
FROM build AS publish

# --no-build: artifacts are already in the build stage; skip recompilation.
# --self-contained false: rely on the aspnet runtime image (smaller output).
RUN dotnet publish src/Catalog/OrderHub.Catalog.Api/OrderHub.Catalog.Api.csproj \
    --no-build \
    --configuration Release \
    --self-contained false \
    --output /app/publish

# ─── Stage 3: final ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# Create a non-root user so the process never runs as root inside the container.
# --disabled-password and --gecos "" suppress interactive prompts.
RUN adduser --disabled-password --gecos "" appuser

WORKDIR /app

# Copy published output and hand ownership to appuser so the process can
# read the files without needing escalated privileges.
COPY --from=publish --chown=appuser:appuser /app/publish .

# Switch to the non-root user before the process starts.
USER appuser

# ASP.NET Core listens on 8080 by default in .NET 8+; align EXPOSE accordingly.
EXPOSE 8080

ENTRYPOINT ["dotnet", "OrderHub.Catalog.Api.dll"]

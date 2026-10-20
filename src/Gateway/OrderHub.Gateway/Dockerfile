# ─── Stage 1: build ───────────────────────────────────────────────────────────
# Build context is the repository root so we can reach Directory.Build.props.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the global props first — it carries Nullable, TreatWarningsAsErrors, etc.
# Copying it before the .csproj keeps this layer cached as long as the file
# hasn't changed.
COPY Directory.Build.props ./

# Copy only the project file to take advantage of Docker layer caching.
# dotnet restore is slow; isolating it in its own layer means a source-only
# change won't invalidate the NuGet cache layer.
COPY src/Gateway/OrderHub.Gateway/OrderHub.Gateway.csproj \
     src/Gateway/OrderHub.Gateway/

# Restore using the Gateway project; NuGet resolves the dependency graph.
RUN dotnet restore src/Gateway/OrderHub.Gateway/OrderHub.Gateway.csproj

# Copy all source after the restore layer so that code changes don't bust the
# NuGet cache.
COPY src/ src/

# Build in Release without re-running restore (already done above).
RUN dotnet build src/Gateway/OrderHub.Gateway/OrderHub.Gateway.csproj \
    --no-restore \
    --configuration Release

# ─── Stage 2: publish ─────────────────────────────────────────────────────────
FROM build AS publish

# --no-build: artifacts are already in the build stage; skip recompilation.
# --self-contained false: rely on the aspnet runtime image (smaller output).
RUN dotnet publish src/Gateway/OrderHub.Gateway/OrderHub.Gateway.csproj \
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

ENTRYPOINT ["dotnet", "OrderHub.Gateway.dll"]

# syntax=docker/dockerfile:1

# =======================================================
# Stage 1: Build Angular frontend
# =======================================================
FROM node:20-alpine AS client-build
WORKDIR /app

# Copy dependency specifications first for Docker layer caching
COPY WEB/ClientApp/package.json WEB/ClientApp/package-lock.json* ./
COPY WEB/ClientApp/patches/ ./patches/

# Install dependencies (respecting lockfile and applying patches)
RUN npm ci --prefer-offline --no-audit || npm install --force

# Copy Angular source code
COPY WEB/ClientApp/ ./

# Build Angular production bundle (outputs to /app/dist)
RUN npm run build -- --configuration production

# =======================================================
# Stage 2: Build ASP.NET Core backend
# =======================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore NuGet packages (layer cached)
COPY WEB/WEB.csproj ./WEB/
RUN dotnet restore WEB/WEB.csproj

# Copy backend source code
COPY WEB/ ./WEB/

# Copy pre-built Angular dist directly into wwwroot
COPY --from=client-build /app/dist/ ./WEB/wwwroot/

WORKDIR /src/WEB

# Publish self-contained Linux x64 application
RUN dotnet publish WEB.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -o /app/publish \
    /p:PublishRunWebpack=false

# =======================================================
# Stage 3: All-in-One Runtime (SQL Server 2022 + ASP.NET Core 10 + Angular)
# =======================================================
FROM mcr.microsoft.com/mssql/server:2022-latest AS runtime
USER root

# Install dependencies for .NET Linux binaries, curl for health checks, and procps
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        curl \
        ca-certificates \
        libicu70 \
        libssl3 \
        procps \
        libcap2-bin && \
    rm -rf /var/lib/apt/lists/*

# Copy published application and Angular static assets
WORKDIR /app
COPY --from=build /app/publish /app/
COPY --from=client-build /app/dist/ /app/wwwroot/

# Copy and prepare entrypoint startup script
COPY entrypoint.sh /entrypoint.sh
RUN sed -i 's/\r$//' /entrypoint.sh && chmod +x /entrypoint.sh

# Ensure persistent directories exist and grant ownership to mssql user
RUN mkdir -p /app/data /app/logs /var/opt/mssql && \
    chown -R mssql:0 /app /var/opt/mssql && \
    chmod -R 775 /app /var/opt/mssql

# Allow Monic.Web to bind to privileged ports (e.g. 80) if run as non-root
RUN setcap 'cap_net_bind_service=+ep' /app/Monic.Web || true

# Switch to mssql user (UID 10001) for native SQL Server execution
USER mssql

# Expose HTTP ports (3000, 80) and SQL Server port (1433)
EXPOSE 3000 80 1433

# Define persistent volumes for SQL Server data and DataProtection keys
VOLUME ["/var/opt/mssql", "/app/data"]

# Health check verifies the web app is responding on 3000 or 80
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:3000/health || curl -f http://localhost:80/health || exit 1

ENTRYPOINT ["/entrypoint.sh"]

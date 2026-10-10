# syntax=docker/dockerfile:1

# =======================================================
# Stage 1: Build & Publish ASP.NET Core Backend
# (Serves pre-compiled Angular 19 SPA directly from wwwroot)
# =======================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore NuGet packages
COPY WEB/WEB.csproj ./WEB/
RUN dotnet restore WEB/WEB.csproj

# Copy backend source code including pre-built Angular SPA in WEB/wwwroot
COPY WEB/ ./WEB/

WORKDIR /src/WEB

# Publish self-contained Linux x64 application
RUN dotnet publish WEB.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -o /app/publish \
    /p:PublishRunWebpack=false

# =======================================================
# Stage 2: All-in-One Runtime (SQL Server 2022 + ASP.NET Core 10 + Angular)
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
        procps && \
    rm -rf /var/lib/apt/lists/*

# Copy published application (includes backend binaries and wwwroot SPA)
WORKDIR /app
COPY --from=build /app/publish /app/

# Copy and prepare entrypoint startup script
COPY entrypoint.sh /entrypoint.sh
RUN sed -i 's/\r$//' /entrypoint.sh && chmod +x /entrypoint.sh

# Ensure persistent directories exist and have proper permissions
RUN mkdir -p /app/data /app/logs /var/opt/mssql && \
    chown -R mssql:root /var/opt/mssql && \
    chmod -R 775 /app /var/opt/mssql

# Expose HTTP ports (3000, 80) and SQL Server port (1433)
EXPOSE 3000 80 1433

# Define persistent volumes for SQL Server data and DataProtection keys
VOLUME ["/var/opt/mssql", "/app/data"]

# Health check verifies the web app is responding on 3000 or 80
# 300s start-period to allow initial SQL Server boot & all 64 EF Core migrations
HEALTHCHECK --interval=10s --timeout=5s --start-period=300s --retries=10 \
    CMD curl -f http://localhost:3000/health || curl -f http://localhost:3000/ || curl -f http://localhost:80/health || curl -f http://localhost:80/ || exit 1

ENTRYPOINT ["/entrypoint.sh"]

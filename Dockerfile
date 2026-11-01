# syntax=docker/dockerfile:1

# =======================================================
# Stage 1: Build the Angular 19 SPA
# =======================================================
FROM node:20-alpine AS spa
WORKDIR /spa

# Copy dependency manifests first for better layer caching
COPY WEB/ClientApp/package.json WEB/ClientApp/package-lock.json ./

# patch-package patches must exist before npm ci (postinstall applies them)
COPY WEB/ClientApp/patches ./patches

# npm ci installs exactly what the lockfile pins (reproducible, no --force)
RUN npm ci --no-audit --no-fund

COPY WEB/ClientApp/ ./
RUN npm run build

# =======================================================
# Stage 2: Build & publish the ASP.NET Core 10 backend
# =======================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the project file and restore NuGet packages first for better caching
COPY WEB/WEB.csproj ./WEB/
RUN dotnet restore WEB/WEB.csproj

COPY WEB/ ./WEB/
WORKDIR /src/WEB

RUN dotnet publish WEB.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:PublishRunWebpack=false

# =======================================================
# Stage 3: Runtime (ASP.NET Core 10)
# =======================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# libgdiplus needed for System.Drawing (PDF/image generation) + curl for healthcheck
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgdiplus \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Copy the published app and replace wwwroot with freshly built SPA
COPY --from=build /app/publish .
RUN rm -rf ./wwwroot
COPY --from=spa /spa/dist ./wwwroot

COPY entrypoint.sh /entrypoint.sh
RUN sed -i 's/\r$//' /entrypoint.sh && chmod +x /entrypoint.sh

# Persistent state: OpenIddict signing certificate + DataProtection keys
RUN mkdir -p /app/data /app/logs && \
    chmod -R 775 /app/data /app/logs

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=300s --retries=5 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["/entrypoint.sh"]

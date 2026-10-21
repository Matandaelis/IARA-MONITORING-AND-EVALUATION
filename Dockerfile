# syntax=docker/dockerfile:1

# =======================================================
# Stage 1: Build the Angular 19 SPA
# (replaces the stale committed bundles in WEB/wwwroot)
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
RUN npm run build -- --output-path /spa/dist

# =======================================================
# Stage 2: Build & publish the ASP.NET Core 10 backend
# =======================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy the project file and restore NuGet packages first for better caching
COPY WEB/WEB.csproj ./WEB/
RUN dotnet restore WEB/WEB.csproj

COPY WEB/ ./WEB/
WORKDIR /src/WEB

# Framework-dependent publish (no RID): portable across glibc/musl runtime
# images and smaller than self-contained. The SPA is built in the 'spa'
# stage (PublishRunWebpack=false skips the npm publish step from the csproj).
RUN dotnet publish WEB.csproj \
    -c Release \
    -o /app/publish \
    /p:PublishRunWebpack=false

# =======================================================
# Stage 3: Runtime (ASP.NET Core 10 on Alpine, non-root)
# SQL Server runs as a separate service (docker-compose 'monic-db',
# a managed cloud database, etc.) - NOT inside this container.
# =======================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final

WORKDIR /app

# Copy the published app, then replace the committed (stale) SPA in
# wwwroot with the freshly built one from stage 1.
COPY --from=build /app/publish .
RUN rm -rf ./wwwroot
COPY --from=spa /spa/dist ./wwwroot

COPY entrypoint.sh /entrypoint.sh
RUN sed -i 's/\r$//' /entrypoint.sh && chmod +x /entrypoint.sh

# Persistent state: OpenIddict signing certificate + DataProtection keys
# (survive container restarts via the monic-app-data volume)
RUN mkdir -p /app/data /app/logs && \
    chown -R $APP_UID:$APP_UID /app/data /app/logs

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

# wget ships with busybox on Alpine (no curl needed).
# 300s start period covers the first-boot EF Core migrations against a
# freshly created database (64-table model).
HEALTHCHECK --interval=30s --timeout=5s --start-period=300s --retries=5 \
    CMD wget -q -O /dev/null http://localhost:8080/health || exit 1

# Non-root: the app user only needs to READ /app and WRITE /app/data, /app/logs
USER $APP_UID

ENTRYPOINT ["/entrypoint.sh"]

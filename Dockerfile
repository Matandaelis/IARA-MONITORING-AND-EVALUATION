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

# Publish application in Release mode (skipping redundant npm rebuild)
RUN dotnet publish WEB.csproj \
    -c Release \
    -o /app/publish \
    /p:PublishRunWebpack=false \
    /p:UseAppHost=false

# =======================================================
# Stage 3: Runtime image
# =======================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Install curl for container health checks
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=build /app/publish .

# Create persistent storage directories
RUN mkdir -p /app/data /app/logs

# Environment defaults
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:80 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DOTNET_RUNNING_IN_CONTAINER=true

# Health check compatible with Coolify, Docker, AWS, Render
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:80/health || exit 1

EXPOSE 80

ENTRYPOINT ["dotnet", "Monic.Web.dll"]

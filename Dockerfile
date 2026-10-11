# ── Stage 1: Build Angular frontend ──────────────────
FROM node:22-alpine AS node-build
WORKDIR /app/WEB/ClientApp

# Install dependencies first (better layer caching)
COPY WEB/ClientApp/package*.json ./
RUN npm install --force

# Copy source and build for production
COPY WEB/ClientApp/ ./
RUN npm run build -- --configuration production

# ── Stage 2: Build .NET 10 backend ───────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dotnet-build
WORKDIR /src

# Restore NuGet packages
COPY WEB/WEB.csproj WEB/
RUN dotnet restore WEB/WEB.csproj

# Copy full source (Angular dist already built in prior stage)
COPY WEB/ WEB/

# Copy the built Angular app into ClientApp/dist
COPY --from=node-build /app/WEB/ClientApp/dist WEB/ClientApp/dist

# Publish in Release mode (Angular step skipped — dist already present)
RUN dotnet publish WEB/WEB.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:RunWebpack=false

# ── Stage 3: Runtime image ───────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# libgdiplus needed for System.Drawing (PDF/image generation)
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgdiplus \
    && rm -rf /var/lib/apt/lists/*

COPY --from=dotnet-build /app/publish .

# ASP.NET Core listens on 8080 by default in containers
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "WEB.dll"]
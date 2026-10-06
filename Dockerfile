# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Install Node.js because WEB/WEB.csproj runs npm during publish
RUN apt-get update && \
    apt-get install -y curl ca-certificates gnupg && \
    curl -fsSL https://deb.nodesource.com/setup_20.x | bash - && \
    apt-get install -y nodejs && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /src

# Copy entire repository
COPY . .

WORKDIR /src/WEB

# Restore NuGet packages
RUN dotnet restore WEB.csproj

# Build and publish the application
# This will:
# 1. Run npm install in ClientApp (via PublishRunWebpack target)
# 2. Build Angular frontend with npm run build
# 3. Compile ASP.NET Core backend
# 4. Publish to /app/publish
RUN dotnet publish WEB.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published application from build stage
COPY --from=build /app/publish .

# Environment configuration
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

# Health check for Docker/Coolify
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost/health || exit 1

EXPOSE 80

ENTRYPOINT ["dotnet", "Monic.Web.dll"]

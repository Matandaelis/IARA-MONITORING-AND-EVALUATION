FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Install Node.js 20 for the Angular frontend build
RUN apt-get update \
    && apt-get install -y curl ca-certificates gnupg \
    && curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src
COPY . .

WORKDIR /src/WEB

# Restore .NET dependencies
RUN dotnet restore WEB.csproj

# Create the configuration file expected by the custom publish target
RUN cp appsettings.json appsettings.coolify.json

# Build Angular and publish the ASP.NET Core application
RUN dotnet publish WEB.csproj \
    --configuration Release \
    --output /app/publish \
    -p:ClientName=coolify \
    -p:UseAppHost=false

# Verify that the application entry point exists
RUN test -f /app/publish/Monic.Web.dll

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

# Install curl for the container health check
RUN apt-get update \
    && apt-get install -y curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=5 \
    CMD curl --fail http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "Monic.Web.dll"]
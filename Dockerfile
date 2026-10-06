# Multi-stage build for MONIC
# Stage 1: Build Node dependencies and Angular frontend
FROM node:20-alpine AS node-builder
WORKDIR /app/WEB/ClientApp
COPY WEB/ClientApp/package*.json ./
RUN npm install --force

# Stage 2: Build .NET backend
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS dotnet-builder
WORKDIR /app
COPY . .
COPY --from=node-builder /app/WEB/ClientApp/node_modules ./WEB/ClientApp/node_modules

# Restore and build the backend
RUN dotnet restore WEB/WEB.csproj
RUN dotnet publish -c Release -o /app/publish WEB/WEB.csproj

# Stage 3: Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine

# Install required dependencies
RUN apk add --no-cache \
    curl \
    ca-certificates

WORKDIR /app

# Copy published app from build stage
COPY --from=dotnet-builder /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:80/health || exit 1

# Expose ports
EXPOSE 80 443

# Set environment variables
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "Monic.Web.dll"]

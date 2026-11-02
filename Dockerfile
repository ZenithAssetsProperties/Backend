# ==============================================================================
# Multi-Stage Production Dockerfile for Zenith Assets Properties API (.NET 10)
# ==============================================================================

# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy solution and project files first to leverage Docker layer caching
COPY ZenithAssets.sln ./
COPY src/ZenithAssets.Domain/ZenithAssets.Domain.csproj src/ZenithAssets.Domain/
COPY src/ZenithAssets.Application/ZenithAssets.Application.csproj src/ZenithAssets.Application/
COPY src/ZenithAssets.Infrastructure/ZenithAssets.Infrastructure.csproj src/ZenithAssets.Infrastructure/
COPY src/ZenithAssets.WebApi/ZenithAssets.WebApi.csproj src/ZenithAssets.WebApi/

# Restore dependencies
RUN dotnet restore ZenithAssets.sln

# Copy the remaining source code
COPY src/ src/

# Build and publish release binaries
WORKDIR /src/src/ZenithAssets.WebApi
RUN dotnet publish ZenithAssets.WebApi.csproj -c Release -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Stage 2: Runtime Image (Hardened, Minimal Alpine Linux)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# Configure ASP.NET Core environment for production
ENV ASPNETCORE_URLS=http://+:5000 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Install ICU libraries for proper globalization (currencies, dates)
RUN apk add --no-cache icu-libs tzdata

# Create non-root system user for security compliance
USER $APP_UID

# Copy compiled artifacts from build stage
COPY --from=build /app/publish .

EXPOSE 5000

# Health check
HEALTHCHECK --interval=15s --timeout=5s --start-period=10s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:5000/health/live || exit 1

ENTRYPOINT ["dotnet", "ZenithAssets.WebApi.dll"]

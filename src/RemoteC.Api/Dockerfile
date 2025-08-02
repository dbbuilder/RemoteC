# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files for dependency projects
COPY src/RemoteC.Api/RemoteC.Api.csproj src/RemoteC.Api/
COPY src/RemoteC.Data/RemoteC.Data.csproj src/RemoteC.Data/
COPY src/RemoteC.Shared/RemoteC.Shared.csproj src/RemoteC.Shared/
COPY src/RemoteC.Core.Interop/RemoteC.Core.Interop.csproj src/RemoteC.Core.Interop/

# Restore dependencies for the API project only
WORKDIR /src/src/RemoteC.Api
RUN dotnet restore

# Copy source code
WORKDIR /src
COPY src/ src/

# Build application
WORKDIR /src/src/RemoteC.Api
RUN dotnet build -c Release -o /app/build /p:NoWarn=CS1591

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false /p:NoWarn=CS1591

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN groupadd -g 1000 remotec && \
    useradd -u 1000 -g remotec -m -s /bin/bash remotec

# Copy published files
COPY --from=publish /app/publish .

# Create directories for logs and certificates
RUN mkdir -p /app/logs /app/certs && \
    chown -R remotec:remotec /app

# Switch to non-root user
USER remotec

# Expose ports
EXPOSE 80
EXPOSE 443

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:80/health || exit 1

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_USE_POLLING_FILE_WATCHER=true

# Entry point
ENTRYPOINT ["dotnet", "RemoteC.Api.dll"]
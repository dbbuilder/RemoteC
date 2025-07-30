# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY RemoteC.sln .
COPY src/RemoteC.Host/RemoteC.Host.csproj src/RemoteC.Host/
COPY src/RemoteC.Shared/RemoteC.Shared.csproj src/RemoteC.Shared/
COPY src/RemoteC.Core.Interop/RemoteC.Core.Interop.csproj src/RemoteC.Core.Interop/

# Restore dependencies
RUN dotnet restore "src/RemoteC.Host/RemoteC.Host.csproj"

# Copy source code
COPY src/ src/

# Build application
WORKDIR /src/src/RemoteC.Host
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=true /p:PublishSingleFile=false

# Windows runtime stage for Windows containers
FROM mcr.microsoft.com/dotnet/runtime:8.0-windowsservercore-ltsc2022 AS runtime-windows
WORKDIR /app

# Copy published files
COPY --from=publish /app/publish .

# Create directories for logs
RUN mkdir logs

# Health check using PowerShell
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD powershell -Command "if ((Get-Process -Name RemoteC.Host -ErrorAction SilentlyContinue) -eq $null) { exit 1 } else { exit 0 }"

# Set environment variables
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV REMOTEC_HOST_MODE=service

# Entry point
ENTRYPOINT ["RemoteC.Host.exe"]

# Linux runtime stage (for testing/development only - full functionality requires Windows)
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime-linux
WORKDIR /app

# Install required dependencies for limited Linux support
RUN apt-get update && \
    apt-get install -y \
        libx11-6 \
        libxext6 \
        libxrender1 \
        libxtst6 \
        libxi6 \
        libfreetype6 \
        libfontconfig1 && \
    rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN groupadd -g 1000 remotec && \
    useradd -u 1000 -g remotec -m -s /bin/bash remotec

# Copy published files
COPY --from=publish /app/publish .

# Create directories and set permissions
RUN mkdir -p /app/logs && \
    chown -R remotec:remotec /app

# Switch to non-root user
USER remotec

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD test -f /app/logs/heartbeat || exit 1

# Set environment variables
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV REMOTEC_HOST_MODE=limited

# Note: Full remote control functionality requires Windows
# This Linux container provides limited functionality for testing
ENTRYPOINT ["dotnet", "RemoteC.Host.dll"]
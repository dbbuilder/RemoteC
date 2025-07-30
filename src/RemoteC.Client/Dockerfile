# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY RemoteC.sln .
COPY src/RemoteC.Client/RemoteC.Client.csproj src/RemoteC.Client/
COPY src/RemoteC.Shared/RemoteC.Shared.csproj src/RemoteC.Shared/

# Restore dependencies
RUN dotnet restore "src/RemoteC.Client/RemoteC.Client.csproj"

# Copy source code
COPY src/ src/

# Build application
WORKDIR /src/src/RemoteC.Client
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage - using SDK image for Avalonia UI support
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS runtime
WORKDIR /app

# Install dependencies for Avalonia UI
RUN apt-get update && \
    apt-get install -y \
        libx11-6 \
        libx11-xcb1 \
        libxcb1 \
        libxcb-icccm4 \
        libxcb-image0 \
        libxcb-keysyms1 \
        libxcb-randr0 \
        libxcb-render0 \
        libxcb-render-util0 \
        libxcb-shape0 \
        libxcb-shm0 \
        libxcb-sync1 \
        libxcb-xfixes0 \
        libxcb-xinerama0 \
        libxcb-xkb1 \
        libxkbcommon0 \
        libxkbcommon-x11-0 \
        libfontconfig1 \
        libfreetype6 \
        libssl3 \
        libgdiplus \
        ca-certificates \
        fonts-liberation \
        xvfb && \
    rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN groupadd -g 1000 remotec && \
    useradd -u 1000 -g remotec -m -s /bin/bash remotec

# Copy published files
COPY --from=publish /app/publish .

# Create directories and set permissions
RUN mkdir -p /app/logs /app/config && \
    chown -R remotec:remotec /app

# Switch to non-root user
USER remotec

# Set display for X11
ENV DISPLAY=:99

# Set environment variables
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV AVALONIA_SCREEN_SCALE_FACTORS="1"

# Create startup script for Xvfb and application
RUN echo '#!/bin/bash\n\
Xvfb :99 -screen 0 1024x768x24 -ac +extension GLX +render -noreset &\n\
sleep 2\n\
dotnet RemoteC.Client.dll "$@"' > /app/start.sh && \
    chmod +x /app/start.sh

# Note: This container is primarily for CI/CD testing
# For production use, the client should be installed natively
ENTRYPOINT ["/app/start.sh"]

# Alternative: Direct native execution without virtual display
# Uncomment for environments with X11 forwarding
# ENTRYPOINT ["dotnet", "RemoteC.Client.dll"]
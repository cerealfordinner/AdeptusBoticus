# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy everything first (trick Docker cache for restore)
COPY . .

# Restore dependencies with clean cache to avoid stale package issues
RUN dotnet nuget locals all --clear && \
    dotnet restore "src/AdeptusBoticus/AdeptusBoticus.csproj" --force

# Build and publish
WORKDIR "/src/src/AdeptusBoticus"
RUN dotnet publish "AdeptusBoticus.csproj" -c Release \
    --runtime linux-x64 \
    --self-contained true \
    -o /app/publish \
    /p:PublishSingleFile=true

# Runtime stage - since it's self-contained, we only need a minimal Linux base
FROM debian:bookworm-slim AS runtime
WORKDIR /app

# Install basic dependencies
RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
    libicu72 \
    && rm -rf /var/lib/apt/lists/* \
    && ldconfig

# Copy published output from build stage
COPY --from=build /app/publish/ ./

# Ensure binary is executable
RUN chmod +x AdeptusBoticus

# Create data directory for persistent volume
RUN mkdir -p /data && chmod 755 /data

# Create logs directory and set permissions for non-root user
RUN mkdir -p /app/logs && chown 1000:1000 /app/logs

# Run as non-root for security
USER 1000

ENTRYPOINT ["./AdeptusBoticus"]

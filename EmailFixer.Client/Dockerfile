# Build stage
# Force rebuild with clean artifacts to fix SRI integrity mismatches
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy solution and project files for better caching
COPY ["EmailFixer.sln", "./"]
COPY ["EmailFixer.Client/EmailFixer.Client.csproj", "EmailFixer.Client/"]
COPY ["EmailFixer.Shared/EmailFixer.Shared.csproj", "EmailFixer.Shared/"]

# Restore dependencies
RUN dotnet restore "EmailFixer.Client/EmailFixer.Client.csproj"

# Copy source code
COPY . .

# Build and publish Blazor WebAssembly application
WORKDIR "/src/EmailFixer.Client"

# AGGRESSIVE CLEAN: Remove ALL build artifacts and NuGet packages
# This forces completely fresh build of all framework files
RUN rm -rf bin obj .vs .vscode /app/publish
RUN find /src \( -type d -name "bin" -o -type d -name "obj" \) -exec rm -rf {} + 2>/dev/null || true

RUN dotnet publish "EmailFixer.Client.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:PublishTrimmed=false \
    /p:PublishReadyToRun=false

# Verify wwwroot exists and has files
RUN echo "=== Checking publish output ===" && \
    ls -la /app/publish/ && \
    echo "=== Checking wwwroot ===" && \
    ls -la /app/publish/wwwroot/ && \
    echo "=== Checking _framework ===" && \
    ls -la /app/publish/wwwroot/_framework/ | head -30 && \
    echo "=== Framework files count ===" && \
    find /app/publish/wwwroot/_framework/ -type f | wc -l

# Runtime stage - nginx to serve static files
FROM nginx:alpine AS runtime

# Install curl for health checks
RUN apk add --no-cache curl

# Copy published Blazor WASM app to nginx html root
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

# Ensure nginx can read all files
RUN chmod -R 755 /usr/share/nginx/html && \
    echo "=== Final files in container ===" && \
    ls -la /usr/share/nginx/html/_framework/ | head -20

# Copy custom nginx configuration
COPY nginx.conf /etc/nginx/nginx.conf

# Cloud Run requires listening on PORT environment variable (default 8080)
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/ || exit 1

# Start nginx
CMD ["nginx", "-g", "daemon off;"]

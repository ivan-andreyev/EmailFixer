# Build stage
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
RUN dotnet publish "EmailFixer.Client.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false && \
    # Verify wwwroot exists and has files
    ls -la /app/publish/ && \
    ls -la /app/publish/wwwroot/ || true

# Runtime stage - nginx to serve static files
FROM nginx:alpine AS runtime

# Install curl for health checks
RUN apk add --no-cache curl

# Copy published Blazor WASM app to nginx html root
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

# Copy custom nginx configuration
COPY nginx.conf /etc/nginx/nginx.conf

# Expose port 80
EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD curl -f http://localhost/ || exit 1

# Start nginx
CMD ["nginx", "-g", "daemon off;"]

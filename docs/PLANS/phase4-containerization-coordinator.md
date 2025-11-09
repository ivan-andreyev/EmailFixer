# 🐳 Phase 4: Containerization Coordinator

**Phase ID:** phase4-containerization
**Parent Plan:** [emailfixer-completion-plan.md](emailfixer-completion-plan.md)
**Duration:** 2 hours
**Dependencies:** Phase 2 API (structure complete)
**Priority:** P1 - Deployment Prerequisite
**Parallel Execution:** Can run alongside Phase 3 Client

## 📋 Phase Overview

Create Docker containers for all components with optimized multi-stage builds, compose orchestration, and production-ready configurations. Focus on small image sizes and security best practices.

## 🎯 Containerization Strategy

### Architecture Decisions:
- **Multi-stage builds:** Minimize final image size
- **Alpine base images:** Security and size optimization
- **Non-root users:** Security hardening
- **Layer caching:** Fast rebuilds
- **Health checks:** Container orchestration support

### Target Metrics:
- API Image: < 200MB
- Client Image: < 50MB
- Build time: < 3 minutes
- Startup time: < 10 seconds

## 📝 Task Breakdown

### Task 4.1: API Dockerfile
**Duration:** 30 minutes
**LLM Readiness:** 100%

**File:** `EmailFixer.Api/Dockerfile`

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy solution and project files for better caching
COPY ["EmailFixer.sln", "./"]
COPY ["EmailFixer.Api/EmailFixer.Api.csproj", "EmailFixer.Api/"]
COPY ["EmailFixer.Core/EmailFixer.Core.csproj", "EmailFixer.Core/"]
COPY ["EmailFixer.Infrastructure/EmailFixer.Infrastructure.csproj", "EmailFixer.Infrastructure/"]
COPY ["EmailFixer.Shared/EmailFixer.Shared.csproj", "EmailFixer.Shared/"]

# Restore dependencies
RUN dotnet restore "EmailFixer.Api/EmailFixer.Api.csproj"

# Copy source code
COPY . .

# Build application
WORKDIR "/src/EmailFixer.Api"
RUN dotnet build "EmailFixer.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "EmailFixer.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:PublishSingleFile=false \
    /p:PublishTrimmed=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Install culture data for globalization
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Create non-root user
RUN addgroup -g 1000 appuser && \
    adduser -u 1000 -G appuser -D appuser

# Copy published app
COPY --from=publish /app/publish .

# Set ownership
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Configure ports and health check
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "EmailFixer.Api.dll"]
```

**File:** `EmailFixer.Api/.dockerignore`

```
# Build artifacts
**/bin/
**/obj/
**/out/

# Visual Studio
.vs/
*.user
*.suo

# User files
*.DS_Store
Thumbs.db

# Git
.git/
.gitignore

# Documentation
*.md
docs/

# Tests
**/*Tests/
**/*Tests.csproj

# Local settings
**/appsettings.Local.json
**/appsettings.*.Local.json
.env
```

**Validation:**
```powershell
# Build API image
docker build -t emailfixer-api:latest -f EmailFixer.Api/Dockerfile .

# Test run
docker run -d -p 8080:8080 --name test-api emailfixer-api:latest

# Check health
docker exec test-api wget -O- http://localhost:8080/health

# Check size
docker images emailfixer-api:latest
```

### Task 4.2: Blazor Client Dockerfile
**Duration:** 35 minutes
**LLM Readiness:** 100%

**File:** `EmailFixer.Client/Dockerfile`

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy project files
COPY ["EmailFixer.Client/EmailFixer.Client.csproj", "EmailFixer.Client/"]
COPY ["EmailFixer.Shared/EmailFixer.Shared.csproj", "EmailFixer.Shared/"]

# Restore dependencies
RUN dotnet restore "EmailFixer.Client/EmailFixer.Client.csproj"

# Copy source code
COPY EmailFixer.Client/ EmailFixer.Client/
COPY EmailFixer.Shared/ EmailFixer.Shared/

# Build and publish Blazor WASM
WORKDIR "/src/EmailFixer.Client"
RUN dotnet publish "EmailFixer.Client.csproj" \
    -c Release \
    -o /app/publish \
    /p:BlazorEnableCompression=true \
    /p:BlazorWebAssemblyEnableLinking=true

# Runtime stage - nginx
FROM nginx:alpine AS runtime

# Remove default nginx files
RUN rm -rf /usr/share/nginx/html/*
RUN rm /etc/nginx/conf.d/default.conf

# Copy nginx configuration
COPY EmailFixer.Client/nginx.conf /etc/nginx/conf.d/

# Copy Blazor app
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

# Create non-root user
RUN addgroup -g 1000 appuser && \
    adduser -u 1000 -G appuser -D appuser && \
    chown -R appuser:appuser /usr/share/nginx/html && \
    chown -R appuser:appuser /var/cache/nginx && \
    chown -R appuser:appuser /var/log/nginx && \
    touch /var/run/nginx.pid && \
    chown appuser:appuser /var/run/nginx.pid

# Security headers and optimizations
RUN echo "server_tokens off;" >> /etc/nginx/nginx.conf

USER appuser

EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/ || exit 1

CMD ["nginx", "-g", "daemon off;"]
```

**File:** `EmailFixer.Client/nginx.conf`

```nginx
server {
    listen 8080;
    server_name _;

    root /usr/share/nginx/html;
    index index.html;

    # Compression
    gzip on;
    gzip_vary on;
    gzip_min_length 1024;
    gzip_types text/css application/javascript application/json application/wasm;

    # Security headers
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;

    # SPA routing - serve index.html for all routes
    location / {
        try_files $uri $uri/ /index.html;

        # Cache static assets
        location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot)$ {
            expires 30d;
            add_header Cache-Control "public, immutable";
        }

        # Don't cache index.html
        location = /index.html {
            expires -1;
            add_header Cache-Control "no-cache, no-store, must-revalidate";
        }

        # Blazor framework files
        location /_framework {
            expires 30d;
            add_header Cache-Control "public, immutable";

            # WASM mime type
            location ~ \.wasm$ {
                add_header Content-Type "application/wasm";
            }

            # .NET assemblies
            location ~ \.dll$ {
                add_header Content-Type "application/octet-stream";
            }
        }
    }

    # API proxy (optional, if not using CORS)
    location /api {
        proxy_pass http://emailfixer-api:8080;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### Task 4.3: Docker Compose Configuration
**Duration:** 25 minutes
**LLM Readiness:** 100%

**File:** `docker-compose.yml`

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:15-alpine
    container_name: emailfixer-postgres
    restart: unless-stopped
    environment:
      POSTGRES_USER: ${POSTGRES_USER:-postgres}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:-DevPassword123!}
      POSTGRES_DB: ${POSTGRES_DB:-emailfixer}
    ports:
      - "${POSTGRES_PORT:-5432}:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    networks:
      - emailfixer-network
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5

  api:
    build:
      context: .
      dockerfile: EmailFixer.Api/Dockerfile
    image: emailfixer-api:${VERSION:-latest}
    container_name: emailfixer-api
    restart: unless-stopped
    environment:
      ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENVIRONMENT:-Development}
      ConnectionStrings__DefaultConnection: Host=postgres;Port=5432;Database=emailfixer;Username=${POSTGRES_USER:-postgres};Password=${POSTGRES_PASSWORD:-DevPassword123!}
      Paddle__ApiKey: ${PADDLE_API_KEY}
      Paddle__ApiUrl: ${PADDLE_API_URL:-https://sandbox-api.paddle.com}
      Paddle__VendorId: ${PADDLE_VENDOR_ID}
      Paddle__WebhookSecret: ${PADDLE_WEBHOOK_SECRET}
    ports:
      - "${API_PORT:-8080}:8080"
    depends_on:
      postgres:
        condition: service_healthy
    networks:
      - emailfixer-network
    healthcheck:
      test: ["CMD", "wget", "--no-verbose", "--tries=1", "--spider", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

  client:
    build:
      context: .
      dockerfile: EmailFixer.Client/Dockerfile
    image: emailfixer-client:${VERSION:-latest}
    container_name: emailfixer-client
    restart: unless-stopped
    environment:
      API_BASE_URL: ${API_BASE_URL:-http://api:8080}
    ports:
      - "${CLIENT_PORT:-80}:8080"
    depends_on:
      api:
        condition: service_healthy
    networks:
      - emailfixer-network
    healthcheck:
      test: ["CMD", "wget", "--no-verbose", "--tries=1", "--spider", "http://localhost:8080/"]
      interval: 30s
      timeout: 10s
      retries: 3

volumes:
  postgres_data:
    driver: local

networks:
  emailfixer-network:
    driver: bridge
```

**File:** `docker-compose.override.yml` (for development)

```yaml
version: '3.8'

services:
  postgres:
    ports:
      - "5432:5432"

  api:
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      Logging__LogLevel__Default: Debug
      Logging__LogLevel__Microsoft: Information
    volumes:
      - ./EmailFixer.Api/appsettings.Development.json:/app/appsettings.Development.json:ro
    ports:
      - "5001:8080"

  client:
    environment:
      BLAZOR_ENVIRONMENT: Development
    ports:
      - "5000:8080"
```

**File:** `.env.example`

```env
# PostgreSQL
POSTGRES_USER=postgres
POSTGRES_PASSWORD=DevPassword123!
POSTGRES_DB=emailfixer
POSTGRES_PORT=5432

# API
API_PORT=8080
ASPNETCORE_ENVIRONMENT=Development

# Client
CLIENT_PORT=80
API_BASE_URL=http://localhost:8080

# Paddle
PADDLE_API_KEY=your_paddle_api_key
PADDLE_API_URL=https://sandbox-api.paddle.com
PADDLE_VENDOR_ID=your_vendor_id
PADDLE_WEBHOOK_SECRET=your_webhook_secret

# Version
VERSION=latest
```

### Task 4.4: Build Optimization Script
**Duration:** 15 minutes
**LLM Readiness:** 100%

**File:** `scripts/docker-build.ps1` (Windows PowerShell)

```powershell
# Docker build script for Windows
param(
    [string]$Version = "latest",
    [switch]$Push = $false,
    [string]$Registry = "gcr.io/emailfixer-prod"
)

Write-Host "Building EmailFixer Docker images..." -ForegroundColor Green

# Build API
Write-Host "Building API image..." -ForegroundColor Yellow
docker build -t emailfixer-api:$Version -f EmailFixer.Api/Dockerfile .

if ($LASTEXITCODE -ne 0) {
    Write-Error "API build failed"
    exit 1
}

# Build Client
Write-Host "Building Client image..." -ForegroundColor Yellow
docker build -t emailfixer-client:$Version -f EmailFixer.Client/Dockerfile .

if ($LASTEXITCODE -ne 0) {
    Write-Error "Client build failed"
    exit 1
}

# Display sizes
Write-Host "`nImage sizes:" -ForegroundColor Green
docker images | Select-String "emailfixer"

# Tag for registry if pushing
if ($Push) {
    Write-Host "`nTagging images for registry..." -ForegroundColor Yellow
    docker tag emailfixer-api:$Version "$Registry/emailfixer-api:$Version"
    docker tag emailfixer-client:$Version "$Registry/emailfixer-client:$Version"

    Write-Host "Pushing to registry..." -ForegroundColor Yellow
    docker push "$Registry/emailfixer-api:$Version"
    docker push "$Registry/emailfixer-client:$Version"
}

Write-Host "`nBuild complete!" -ForegroundColor Green
```

### Task 4.5: Database Migration Container
**Duration:** 20 minutes
**LLM Readiness:** 100%

**File:** `EmailFixer.Migrations/Dockerfile`

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Install EF Core tools
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

# Copy project files
COPY ["EmailFixer.Infrastructure/EmailFixer.Infrastructure.csproj", "EmailFixer.Infrastructure/"]
COPY ["EmailFixer.Api/EmailFixer.Api.csproj", "EmailFixer.Api/"]
COPY ["EmailFixer.Core/EmailFixer.Core.csproj", "EmailFixer.Core/"]
COPY ["EmailFixer.Shared/EmailFixer.Shared.csproj", "EmailFixer.Shared/"]

# Restore
RUN dotnet restore "EmailFixer.Api/EmailFixer.Api.csproj"

# Copy source
COPY . .

# Create migration script
WORKDIR "/src"
RUN dotnet ef migrations script \
    -p EmailFixer.Infrastructure \
    -s EmailFixer.Api \
    -o /migration.sql \
    --idempotent

# Runtime stage
FROM postgres:15-alpine AS runtime

COPY --from=build /migration.sql /docker-entrypoint-initdb.d/

ENV POSTGRES_DB=emailfixer
```

## 🧪 Testing & Validation

### Local Testing:
```powershell
# Build all images
docker-compose build

# Start services
docker-compose up -d

# Check health
docker-compose ps

# View logs
docker-compose logs -f api

# Test endpoints
Invoke-WebRequest http://localhost:8080/health
Invoke-WebRequest http://localhost/

# Clean up
docker-compose down -v
```

### Production Build Testing:
```powershell
# Build with production config
docker-compose -f docker-compose.yml build

# Run without override
docker-compose -f docker-compose.yml up -d

# Verify smaller images
docker images | Select-String emailfixer
```

## ✅ Phase Completion Checklist

- [ ] API Dockerfile created and builds successfully
- [ ] Client Dockerfile created with nginx
- [ ] docker-compose.yml orchestrates all services
- [ ] .env.example documents all variables
- [ ] Images optimized (API < 200MB, Client < 50MB)
- [ ] Health checks functioning
- [ ] Non-root users configured
- [ ] Build script automated
- [ ] Migration container ready
- [ ] All services communicate properly
- [ ] Volumes persist data correctly
- [ ] Security headers configured

## 🚨 Common Issues & Solutions

| Issue | Solution |
|-------|----------|
| "Connection refused to postgres" | Ensure postgres health check passes before API starts |
| "CORS errors in browser" | Check nginx proxy configuration or API CORS settings |
| "Large image sizes" | Use alpine base images, multi-stage builds |
| "Permission denied in container" | Ensure proper user/group ownership |
| "Build cache not working" | Order COPY commands from least to most frequently changed |

## 📊 Success Metrics

- ✅ API image: < 200MB
- ✅ Client image: < 50MB
- ✅ Total build time: < 3 minutes
- ✅ Container startup: < 10 seconds
- ✅ All health checks passing
- ✅ Zero security vulnerabilities (scan with trivy)

## 🔒 Security Scan

```powershell
# Install trivy
choco install trivy

# Scan images
trivy image emailfixer-api:latest
trivy image emailfixer-client:latest

# Fix any HIGH or CRITICAL vulnerabilities before deployment
```

## 🔗 Next Phase

After successful completion:
1. ✅ Mark Phase 4 complete in master plan
2. ➡️ Proceed to [Phase 5: Deployment](phase5-deployment-coordinator.md)
3. 📝 Document any configuration changes

---

**Estimated Time:** 2 hours
**Actual Time:** _[To be filled by executor]_
**Executor Notes:** _[To be filled by executor]_
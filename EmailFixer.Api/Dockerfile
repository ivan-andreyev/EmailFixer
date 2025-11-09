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

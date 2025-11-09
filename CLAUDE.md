# EmailFixer - AI Assistant Documentation

This file provides guidance to Claude Code when working in this repository.

## Project Overview

EmailFixer is a professional email validation service built with .NET 8, Blazor WebAssembly, and PostgreSQL. It provides real-time email validation with typo suggestions, credit-based billing via Paddle, and cloud deployment on Google Cloud Platform.

## Architecture Summary

### Technology Stack
- **Backend:** .NET 8 Web API with Clean Architecture
- **Frontend:** Blazor WebAssembly (WASM)
- **Database:** PostgreSQL 16 (SQLite for development)
- **Payments:** Paddle.com integration
- **Deployment:** Google Cloud (Cloud Run, Cloud SQL, Cloud Storage)
- **Containerization:** Docker with multi-stage builds
- **CI/CD:** GitHub Actions

### Project Structure
```
EmailFixer/
├── EmailFixer.Api/           # REST API with controllers
│   ├── Controllers/
│   │   ├── EmailValidationController.cs
│   │   ├── UserController.cs
│   │   └── PaymentController.cs
│   ├── Validators/          # FluentValidation validators
│   ├── Middleware/          # Global exception handling
│   ├── Dockerfile
│   └── Program.cs           # DI configuration
│
├── EmailFixer.Client/        # Blazor WASM frontend
│   ├── Pages/
│   ├── Components/
│   ├── Services/            # HTTP clients
│   ├── wwwroot/
│   └── Dockerfile
│
├── EmailFixer.Core/          # Business logic and domain
│   ├── Models/
│   │   └── EmailValidationResult.cs
│   └── Validators/
│       ├── IEmailValidator.cs
│       └── EmailValidator.cs
│
├── EmailFixer.Infrastructure/# Data access and external services
│   ├── Data/
│   │   ├── EmailFixerDbContext.cs
│   │   ├── Entities/        # DB entities
│   │   │   ├── User.cs
│   │   │   ├── EmailCheck.cs
│   │   │   └── CreditTransaction.cs
│   │   └── Repositories/    # Repository pattern
│   └── Services/
│       └── Payment/
│           └── PaddlePaymentService.cs
│
├── EmailFixer.Shared/        # Shared DTOs and contracts
│   └── Models/
│
├── docs/                     # Documentation
│   ├── PLANS/               # Execution plans
│   └── ANALYSIS/            # Research artifacts
│
├── .github/workflows/       # CI/CD pipelines
├── docker-compose.yml       # Container orchestration
└── EmailFixer.sln           # Solution file
```

## Quick Start Guide

### Prerequisites
- .NET 8 SDK
- Docker Desktop
- PostgreSQL 16 (or use Docker)
- Git

### Local Development Setup

#### Option 1: Docker Compose (Recommended)

```powershell
# Clone repository
git clone <repo>
cd EmailFixer

# Start all services (API, Client, PostgreSQL)
docker-compose up --build

# Services will be available at:
# - API: http://localhost:5165
# - Client: http://localhost:80
# - PostgreSQL: localhost:5432
# - Swagger: http://localhost:5165/swagger
```

#### Option 2: Manual Setup

**1. Database Setup**
```powershell
# Option A: Use Docker for PostgreSQL
docker run -d --name emailfixer-postgres `
  -e POSTGRES_PASSWORD=postgres `
  -e POSTGRES_DB=emailfixer `
  -p 5432:5432 `
  postgres:16-alpine

# Option B: Use SQLite (no setup needed, auto-created)
# Just don't set USE_POSTGRES environment variable
```

**2. Apply Migrations**
```powershell
dotnet ef database update -p EmailFixer.Infrastructure -s EmailFixer.Api
```

**3. Run API**
```powershell
cd EmailFixer.Api
dotnet run

# API available at http://localhost:5165
# Swagger UI at http://localhost:5165/swagger
```

**4. Run Client (separate terminal)**
```powershell
cd EmailFixer.Client
dotnet run

# Client available at http://localhost:5000
```

## Build Commands

### Development Build
```powershell
dotnet build
```

### Release Build
```powershell
dotnet build -c Release
```

### Watch Mode (for development)
```powershell
# API with hot reload
cd EmailFixer.Api
dotnet watch run

# Client with hot reload
cd EmailFixer.Client
dotnet watch run
```

### Clean Build
```powershell
dotnet clean
dotnet restore
dotnet build
```

## Run & Test

### Run Tests
```powershell
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Run specific test project
dotnet test EmailFixer.Tests
```

### Health Check
```powershell
# Check API health
curl http://localhost:5165/health

# Expected response:
# {"status":"healthy","timestamp":"2025-11-09T..."}
```

## Database

### Create Migration
```powershell
dotnet ef migrations add MigrationName `
  -p EmailFixer.Infrastructure `
  -s EmailFixer.Api
```

### Apply Migration
```powershell
# Update to latest
dotnet ef database update `
  -p EmailFixer.Infrastructure `
  -s EmailFixer.Api

# Update to specific migration
dotnet ef database update MigrationName `
  -p EmailFixer.Infrastructure `
  -s EmailFixer.Api
```

### Rollback Migration
```powershell
# Rollback to previous
dotnet ef database update PreviousMigrationName `
  -p EmailFixer.Infrastructure `
  -s EmailFixer.Api
```

### List Migrations
```powershell
dotnet ef migrations list `
  -p EmailFixer.Infrastructure `
  -s EmailFixer.Api
```

### Remove Last Migration
```powershell
dotnet ef migrations remove `
  -p EmailFixer.Infrastructure `
  -s EmailFixer.Api
```

### Generate SQL Script
```powershell
# For production deployment
dotnet ef migrations script `
  -p EmailFixer.Infrastructure `
  -s EmailFixer.Api `
  -o migration.sql
```

## Key Components

### Email Validator (Core)
**Location:** `EmailFixer.Core/Validators/EmailValidator.cs`

**Features:**
- Regex validation for RFC 5322 compliance
- MX record DNS lookup via DnsClient
- Common typo detection (gmial→gmail, yahooo→yahoo)
- Disposable email detection (temporary mail services)
- Business email verification

**Usage:**
```csharp
var validator = new EmailValidator();
var result = await validator.ValidateEmailAsync("test@gmail.com");

if (result.IsValid)
{
    Console.WriteLine("Email is valid");
}
else if (!string.IsNullOrEmpty(result.Suggestion))
{
    Console.WriteLine($"Did you mean: {result.Suggestion}?");
}
```

### API Endpoints

#### Email Validation
- `POST /api/email/validate` - Single email validation
  ```json
  {
    "userId": "guid",
    "email": "test@gmail.com"
  }
  ```

- `POST /api/email/validate-batch` - Batch validation (max 100 emails)
  ```json
  {
    "userId": "guid",
    "emails": ["email1@test.com", "email2@test.com"]
  }
  ```

#### User Management
- `GET /api/users/{id}` - Get user details
- `POST /api/users` - Create new user
- `GET /api/users/{id}/credits` - Check credit balance
- `GET /api/users/{id}/history` - Validation history

#### Payment Integration
- `POST /api/payment/checkout` - Create Paddle checkout session
- `POST /api/payment/webhook` - Paddle webhook handler (internal)
- `GET /api/payment/plans` - Available credit packages

#### Health
- `GET /health` - Health check endpoint

### Database Schema

#### Users Table
```sql
CREATE TABLE Users (
    Id UUID PRIMARY KEY,
    Email VARCHAR(255) UNIQUE NOT NULL,
    Credits INT NOT NULL DEFAULT 0,
    PaddleCustomerId VARCHAR(255),
    CreatedAt TIMESTAMP NOT NULL,
    UpdatedAt TIMESTAMP NOT NULL
);
```

#### EmailChecks Table
```sql
CREATE TABLE EmailChecks (
    Id UUID PRIMARY KEY,
    UserId UUID NOT NULL REFERENCES Users(Id),
    Email VARCHAR(255) NOT NULL,
    IsValid BOOLEAN NOT NULL,
    Suggestion VARCHAR(255),
    ValidationDetails TEXT,
    BatchId UUID,
    CheckedAt TIMESTAMP NOT NULL,
    CreditsUsed INT NOT NULL
);
```

#### CreditTransactions Table
```sql
CREATE TABLE CreditTransactions (
    Id UUID PRIMARY KEY,
    UserId UUID NOT NULL REFERENCES Users(Id),
    PaddleTransactionId VARCHAR(255) UNIQUE,
    Credits INT NOT NULL,
    Amount DECIMAL(10,2) NOT NULL,
    Currency VARCHAR(3) NOT NULL,
    TransactionType VARCHAR(50) NOT NULL,
    CreatedAt TIMESTAMP NOT NULL
);
```

## Configuration

### Environment Variables

**Development:**
```env
# Database (optional, defaults to SQLite)
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=emailfixer;Username=postgres;Password=postgres

# Paddle (test keys)
Paddle__ApiKey=test_your_paddle_api_key
Paddle__SellerId=test_your_seller_id
Paddle__WebhookSecret=test_your_webhook_secret
Paddle__ApiBaseUrl=https://sandbox-api.paddle.com

# JWT (optional)
Jwt__SecretKey=your-super-secret-key-change-in-production
Jwt__Issuer=EmailFixer.Api
Jwt__Audience=EmailFixer.Client
Jwt__ExpirationMinutes=60

# Environment
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:8080

# Database mode
USE_POSTGRES=true  # Set to use PostgreSQL instead of SQLite
```

**Production:**
```env
ConnectionStrings__DefaultConnection=Host=/cloudsql/project:region:instance;Database=emailfixer;Username=xxx;Password=xxx

Paddle__ApiKey=live_your_paddle_api_key
Paddle__SellerId=your_seller_id
Paddle__WebhookSecret=your_webhook_secret
Paddle__ApiBaseUrl=https://api.paddle.com

Jwt__SecretKey=<secure-random-key>

ASPNETCORE_ENVIRONMENT=Production
```

### appsettings.json Files

**appsettings.json** (base configuration)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

**appsettings.Development.json**
- SQLite database for local development
- Logging: Debug level
- Paddle: test/sandbox keys
- CORS: localhost origins

**appsettings.Production.json**
- PostgreSQL on Cloud SQL
- Logging: Warning level
- Secrets from Secret Manager
- CORS: production domains

## Deployment

### Local Docker Deployment
```powershell
# Build and run all services
docker-compose up -d

# View logs
docker-compose logs -f api
docker-compose logs -f client

# Stop all services
docker-compose down

# Rebuild specific service
docker-compose up --build api
```

### Google Cloud Production

**Prerequisites:**
```powershell
# Install gcloud CLI
# Authenticate
gcloud auth login
gcloud config set project your-project-id
```

**Deploy API to Cloud Run:**
```powershell
# Build and push image
gcloud builds submit --tag gcr.io/your-project/emailfixer-api

# Deploy
gcloud run deploy emailfixer-api `
  --image gcr.io/your-project/emailfixer-api `
  --region us-central1 `
  --platform managed `
  --allow-unauthenticated `
  --add-cloudsql-instances your-project:us-central1:emailfixer-db

# Set environment variables
gcloud run services update emailfixer-api `
  --update-env-vars "Paddle__ApiKey=xxx,Paddle__SellerId=xxx"
```

**Deploy Client to Cloud Storage:**
```powershell
# Publish client
cd EmailFixer.Client
dotnet publish -c Release -o ./publish

# Upload to Cloud Storage
gsutil -m rsync -r ./publish/wwwroot gs://emailfixer-client

# Set CORS
gsutil cors set cors.json gs://emailfixer-client
```

**Update Database in Cloud SQL:**
```powershell
# Generate migration script
dotnet ef migrations script -o migration.sql `
  -p EmailFixer.Infrastructure `
  -s EmailFixer.Api

# Apply via Cloud SQL Proxy or console
```

### CI/CD Pipeline

**GitHub Actions automatically:**
- Triggers on push to main branch
- Runs tests
- Builds Docker images
- Pushes to Google Container Registry
- Deploys to Cloud Run
- Invalidates CDN cache

**Manual trigger:**
```powershell
# Via GitHub CLI
gh workflow run deploy.yml
```

## Common Development Tasks

### Add New Email Validation Rule

1. Edit `EmailFixer.Core/Validators/EmailValidator.cs`
2. Add validation logic to `ValidateEmailAsync` method
3. Update `EmailValidationResult` if needed
4. Add tests in `EmailFixer.Tests/Core/EmailValidatorTests.cs`
5. Test locally
6. Deploy changes

### Add New API Endpoint

1. Create controller method in appropriate controller
2. Add request/response DTOs to `EmailFixer.Shared/DTOs/`
3. Add XML documentation for Swagger
4. Update FluentValidation validators if needed
5. Add integration tests
6. Update CORS policy if needed in `Program.cs`
7. Test with Swagger UI
8. Deploy

### Modify Database Schema

1. Edit entity in `EmailFixer.Infrastructure/Data/Entities/`
2. Update `EmailFixerDbContext.cs` if needed (e.g., indexes, relationships)
3. Create migration:
   ```powershell
   dotnet ef migrations add YourMigrationName -p EmailFixer.Infrastructure -s EmailFixer.Api
   ```
4. Review generated migration
5. Test locally:
   ```powershell
   dotnet ef database update -p EmailFixer.Infrastructure -s EmailFixer.Api
   ```
6. Generate SQL script for production
7. Apply to production database
8. Deploy application

### Add New Blazor Component

1. Create component in `EmailFixer.Client/Components/`
2. Add service interface/implementation if needed in `EmailFixer.Client/Services/`
3. Update dependency injection in `Program.cs` if needed
4. Test with `dotnet watch run`
5. Build for production
6. Deploy client

### Update Payment Logic

1. Edit `EmailFixer.Infrastructure/Services/Payment/PaddlePaymentService.cs`
2. Update webhook handling in `PaymentController.cs`
3. Test with Paddle sandbox
4. Verify webhook signature validation
5. Test idempotency
6. Deploy carefully (critical path)

## Troubleshooting

### Database Connection Issues

**Symptoms:** Cannot connect to database

**Solutions:**
```powershell
# Check PostgreSQL is running
docker ps | grep postgres

# Test connection directly
docker exec -it emailfixer-postgres psql -U postgres -d emailfixer

# Check connection string
# In appsettings.Development.json or environment variables

# For SQLite: check file exists
ls emailfixer.db

# Recreate database
dotnet ef database drop -p EmailFixer.Infrastructure -s EmailFixer.Api
dotnet ef database update -p EmailFixer.Infrastructure -s EmailFixer.Api
```

### Migration Errors

**Symptoms:** Migration fails to apply

**Solutions:**
```powershell
# Check migration status
dotnet ef migrations list -p EmailFixer.Infrastructure -s EmailFixer.Api

# View last migration
dotnet ef migrations script --from PreviousMigration --to CurrentMigration

# Rollback if needed
dotnet ef database update PreviousMigrationName

# Remove problematic migration
dotnet ef migrations remove

# Recreate migration
dotnet ef migrations add FixedMigrationName
```

### CORS Errors

**Symptoms:** "Access-Control-Allow-Origin" errors in browser

**Solutions:**
1. Verify CORS policy in `Program.cs` includes correct origins
2. Check allowed methods and headers
3. Ensure `AllowCredentials()` if needed
4. Restart API after changes
5. Clear browser cache

**Example fix:**
```csharp
options.AddPolicy("BlazorClient", policy =>
{
    policy.WithOrigins("http://localhost:80", "https://yourdomain.com")
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
});
```

### Paddle Webhook Failures

**Symptoms:** Webhooks not received or failing

**Solutions:**
1. Verify webhook URL is publicly accessible (use ngrok for local testing)
2. Check signature validation logic
3. Review webhook logs in Paddle dashboard
4. Ensure idempotency is implemented (duplicate webhooks)
5. Test with Paddle's webhook testing tool
6. Check webhook secret matches configuration

**Debug webhook:**
```csharp
// Add logging to PaymentController.cs webhook handler
_logger.LogInformation("Webhook received: {Signature}", Request.Headers["Paddle-Signature"]);
```

### Docker Build Issues

**Symptoms:** Docker build fails

**Solutions:**
```powershell
# Clean Docker cache
docker system prune -a

# Build with no cache
docker-compose build --no-cache

# Check logs
docker-compose logs api

# Build single service
docker-compose build api

# Check Dockerfile syntax
docker build -f EmailFixer.Api/Dockerfile .
```

### API Not Starting

**Symptoms:** API crashes on startup

**Solutions:**
```powershell
# Check logs
dotnet run --project EmailFixer.Api

# Common issues:
# 1. Port already in use
netstat -ano | findstr :5165

# 2. Missing appsettings
ls EmailFixer.Api/appsettings*.json

# 3. Invalid connection string
# Check appsettings.Development.json

# 4. Missing migrations
dotnet ef database update -p EmailFixer.Infrastructure -s EmailFixer.Api
```

### Blazor Client Issues

**Symptoms:** Client won't load or compile

**Solutions:**
```powershell
# Clear obj/bin folders
cd EmailFixer.Client
rmdir /s /q bin obj
dotnet restore
dotnet build

# Check API URL in appsettings
cat wwwroot/appsettings.json

# Test API connection
curl http://localhost:5165/health
```

## Testing

### Unit Tests
```powershell
# Run all tests
dotnet test

# Run with coverage (if configured)
dotnet test --collect:"XPlat Code Coverage"

# Run specific test
dotnet test --filter FullyQualifiedName~EmailValidator
```

### Integration Tests
```powershell
# Requires database
# Set connection string in test project
dotnet test --filter Category=Integration
```

### Manual API Testing
```powershell
# Using curl
curl -X POST http://localhost:5165/api/email/validate `
  -H "Content-Type: application/json" `
  -d '{"userId":"00000000-0000-0000-0000-000000000001","email":"test@gmail.com"}'

# Using Swagger UI
# Navigate to http://localhost:5165/swagger
```

### Load Testing
```powershell
# Using Apache Bench (if installed)
ab -n 1000 -c 10 -T application/json `
  -p test_payload.json `
  http://localhost:5165/api/email/validate

# Using k6 (if installed)
k6 run loadtest.js
```

## Security Considerations

### API Security
1. **Rate Limiting** - Consider implementing (not yet in place)
2. **Input Validation** - FluentValidation on all endpoints
3. **SQL Injection** - Protected via EF Core parameterized queries
4. **XSS Protection** - Built into Blazor

### Secrets Management
1. **Never commit secrets** - Use .gitignore for sensitive files
2. **Development** - Use User Secrets:
   ```powershell
   dotnet user-secrets set "Paddle:ApiKey" "your-key" --project EmailFixer.Api
   ```
3. **Production** - Use Google Secret Manager
4. **Rotate keys** - Regular rotation policy

### Data Protection
1. **HTTPS Only** - Enforced in production
2. **PII Data** - User emails stored, consider encryption at rest
3. **GDPR Compliance** - Implement data deletion on request
4. **Audit Logging** - Consider adding for sensitive operations

### Webhook Security
1. **Signature Validation** - Always validate Paddle webhook signatures
2. **HTTPS Only** - Webhooks must use HTTPS in production
3. **Idempotency** - Handle duplicate webhooks (implemented)

## Performance Optimization

### Database
1. **Indexes** - On UserId, Email, CreatedAt fields
2. **Connection Pooling** - Configured in EF Core
3. **Async Queries** - All database operations async
4. **Read Replicas** - Consider for high load (not yet implemented)

### API
1. **Response Caching** - For static data (e.g., payment plans)
2. **Compression** - Enabled for responses
3. **Minimal JSON** - Use DTOs to control serialization
4. **Async/Await** - Throughout API

### Client
1. **Lazy Loading** - For components (if needed)
2. **CDN** - For static assets (Cloud Storage)
3. **Service Worker** - PWA caching (consider implementing)
4. **Virtualization** - For long lists (if needed)

### Email Validation
1. **DNS Caching** - Consider caching MX lookups
2. **Batch Processing** - Already supports up to 100 emails
3. **Parallel Validation** - Use Task.WhenAll for batches

## Monitoring

### Health Checks
- `/health` endpoint for liveness probes
- Database connectivity check
- Paddle API connectivity (consider adding)

### Logging
- Structured logging via ILogger
- Log levels: Information, Warning, Error
- Consider: Application Insights, Sentry, or Google Cloud Logging

### Metrics (to implement)
- API response times
- Validation success/failure rates
- Credit usage trends
- Error rates

### Alerting (to implement)
- Database down
- API errors > threshold
- Paddle webhook failures
- Low credit warnings

## Support & Maintenance

### Regular Tasks
- **Weekly:** Check error logs, review failed validations
- **Monthly:** Review performance metrics, update dependencies
- **Quarterly:** Security audit, dependency updates
- **Yearly:** Rotate secrets, review architecture

### Backup Strategy
- **Database:** Daily automated backups (Cloud SQL)
- **Code:** Git repository (GitHub)
- **Configurations:** Secret Manager versioning

### Disaster Recovery
1. Database restore from backup
2. Redeploy from main branch
3. Restore secrets from Secret Manager
4. Verify health checks pass

## Additional Resources

### Official Documentation
- [.NET 8 Documentation](https://docs.microsoft.com/dotnet/core/whats-new/dotnet-8)
- [Blazor Documentation](https://docs.microsoft.com/aspnet/core/blazor)
- [Entity Framework Core 8](https://docs.microsoft.com/ef/core/)
- [Paddle API Reference](https://developer.paddle.com/api-reference)
- [Google Cloud Run](https://cloud.google.com/run/docs)

### Tutorials
- [Clean Architecture in .NET](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/)
- [Blazor WebAssembly Tutorial](https://dotnet.microsoft.com/learn/aspnet/blazor-tutorial)
- [Docker Multi-Stage Builds](https://docs.docker.com/build/building/multi-stage/)

### Tools
- [Postman](https://www.postman.com/) - API testing
- [Docker Desktop](https://www.docker.com/products/docker-desktop) - Containerization
- [pgAdmin](https://www.pgadmin.org/) - PostgreSQL management
- [DBeaver](https://dbeaver.io/) - Universal database tool

## Project Status

**Version:** 1.0.0
**Status:** Production Ready
**Last Updated:** 2025-11-09
**Maintained By:** EmailFixer Development Team

### Completed Features
- Email validation with MX lookup
- Typo detection and suggestions
- Batch validation (up to 100 emails)
- Credit-based billing system
- Paddle payment integration
- User management
- RESTful API with Swagger
- Blazor WebAssembly client
- Docker containerization
- PostgreSQL/SQLite dual database support
- Google Cloud deployment configuration
- CI/CD pipeline

### Future Enhancements (Backlog)
- Rate limiting
- Advanced analytics dashboard
- Email deliverability scoring
- API key authentication
- Webhook retry mechanism
- Internationalization (i18n)
- Mobile app
- Bulk CSV upload
- API usage statistics
- Custom validation rules per user

---

**Generated by Claude Code**
**For questions or issues, refer to project documentation in `docs/` folder**

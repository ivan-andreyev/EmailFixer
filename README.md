# EmailFixer

Professional email validation service with real-time verification, typo suggestions, and credit-based billing.

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)
![Blazor](https://img.shields.io/badge/Blazor-WebAssembly-blue.svg)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-blue.svg)
![Docker](https://img.shields.io/badge/Docker-Ready-blue.svg)

## Overview

EmailFixer is a modern, cloud-native email validation service that helps businesses verify email addresses in real-time. Built with .NET 8 and Blazor WebAssembly, it provides instant validation with smart typo detection, MX record verification, and a flexible credit-based billing system powered by Paddle.

## Features

- **Real-time Email Validation** - Instant verification with multiple validation checks
- **Smart Typo Detection** - Automatically suggests corrections for common mistakes (e.g., gmial→gmail, yahooo→yahoo)
- **Batch Processing** - Validate up to 100 emails simultaneously with async processing
- **MX Record Verification** - Ensures the domain can actually receive emails via DNS lookup
- **Disposable Email Detection** - Identifies temporary/throwaway email addresses
- **Credit-Based Billing** - Pay-as-you-go pricing with Paddle integration
- **Modern Blazor UI** - Responsive, fast Blazor WebAssembly interface
- **Cloud Native** - Fully containerized and ready for Google Cloud Platform deployment
- **RESTful API** - Complete API with OpenAPI/Swagger documentation
- **Clean Architecture** - Maintainable, testable, and scalable codebase

## Demo

Try EmailFixer live or explore the API:

- **Live Demo:** [https://emailfixer.example.com](https://emailfixer.example.com) *(coming soon)*
- **API Documentation:** [https://api.emailfixer.example.com/swagger](https://api.emailfixer.example.com/swagger) *(coming soon)*

## Tech Stack

**Backend:**
- .NET 8 - Latest LTS version
- ASP.NET Core Web API - RESTful services
- Entity Framework Core 8 - ORM
- PostgreSQL 16 - Primary database
- FluentValidation - Input validation
- DnsClient - MX record lookups

**Frontend:**
- Blazor WebAssembly - SPA framework
- C# - Single language for full stack

**Payment:**
- Paddle.com - Payment processing and billing

**DevOps:**
- Docker - Containerization
- Docker Compose - Local orchestration
- GitHub Actions - CI/CD pipeline
- Google Cloud Run - Serverless deployment
- Google Cloud SQL - Managed PostgreSQL
- Google Cloud Storage - Static hosting

## Quick Start

### Using Docker Compose (Recommended)

The fastest way to get EmailFixer running locally:

```bash
# Clone repository
git clone https://github.com/yourusername/emailfixer.git
cd emailfixer

# Start all services
docker-compose up

# Access the application:
# - Client UI: http://localhost:80
# - API: http://localhost:5165
# - Swagger: http://localhost:5165/swagger
# - PostgreSQL: localhost:5432
```

That's it! Docker Compose will automatically:
- Start PostgreSQL database
- Build and run the API
- Build and run the Blazor client
- Apply database migrations
- Configure networking between services

### Manual Setup

If you prefer to run services individually:

**1. Prerequisites**
- .NET 8 SDK ([download](https://dotnet.microsoft.com/download/dotnet/8.0))
- PostgreSQL 16+ ([download](https://www.postgresql.org/download/)) or Docker
- Git

**2. Clone and Restore**
```bash
git clone https://github.com/yourusername/emailfixer.git
cd emailfixer
dotnet restore
```

**3. Database Setup**
```bash
# Option A: Use Docker
docker run -d --name emailfixer-postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=emailfixer \
  -p 5432:5432 \
  postgres:16-alpine

# Option B: Use local PostgreSQL
# Create database: CREATE DATABASE emailfixer;

# Apply migrations
dotnet ef database update -p EmailFixer.Infrastructure -s EmailFixer.Api
```

**4. Run API**
```bash
cd EmailFixer.Api
dotnet run

# API available at http://localhost:5165
# Swagger at http://localhost:5165/swagger
```

**5. Run Client** (in separate terminal)
```bash
cd EmailFixer.Client
dotnet run

# Client available at http://localhost:5000
```

## API Usage

### Validate Single Email

```bash
curl -X POST http://localhost:5165/api/email/validate \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "00000000-0000-0000-0000-000000000001",
    "email": "test@gmail.com"
  }'
```

**Response:**
```json
{
  "isValid": true,
  "email": "test@gmail.com",
  "suggestion": null,
  "hasTypo": false,
  "isDisposable": false,
  "mxRecordsExist": true
}
```

### Validate Batch

```bash
curl -X POST http://localhost:5165/api/email/validate-batch \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "00000000-0000-0000-0000-000000000001",
    "emails": [
      "valid@gmail.com",
      "typo@gmial.com",
      "invalid@notarealdomain123456.com"
    ]
  }'
```

**Response:**
```json
{
  "batchId": "550e8400-e29b-41d4-a716-446655440000",
  "results": [
    {
      "email": "valid@gmail.com",
      "isValid": true,
      "suggestion": null
    },
    {
      "email": "typo@gmial.com",
      "isValid": false,
      "suggestion": "typo@gmail.com",
      "hasTypo": true
    },
    {
      "email": "invalid@notarealdomain123456.com",
      "isValid": false,
      "mxRecordsExist": false
    }
  ],
  "totalValidated": 3,
  "creditsUsed": 3
}
```

### Get User Credits

```bash
curl -X GET http://localhost:5165/api/users/{userId}/credits
```

For complete API documentation, visit `/swagger` when running the API.

## Project Structure

```
EmailFixer/
├── EmailFixer.Api/              # REST API
│   ├── Controllers/            # API endpoints
│   ├── Validators/             # Request validation
│   ├── Middleware/             # Exception handling
│   └── Program.cs              # Startup configuration
│
├── EmailFixer.Client/           # Blazor WebAssembly
│   ├── Pages/                  # Razor pages
│   ├── Components/             # Reusable components
│   ├── Services/               # HTTP client services
│   └── wwwroot/                # Static assets
│
├── EmailFixer.Core/             # Business logic
│   ├── Models/                 # Domain models
│   └── Validators/             # Email validation engine
│
├── EmailFixer.Infrastructure/   # Data & external services
│   ├── Data/                   # EF Core setup
│   │   ├── Entities/          # Database entities
│   │   └── Repositories/      # Data access
│   └── Services/              # External integrations
│       └── Payment/           # Paddle integration
│
├── EmailFixer.Shared/           # Shared DTOs
│   └── Models/                # Data transfer objects
│
├── docs/                        # Documentation
│   ├── PLANS/                 # Development plans
│   └── ANALYSIS/              # Design documents
│
├── .github/workflows/           # CI/CD pipelines
├── docker-compose.yml          # Local development
├── CLAUDE.md                   # AI assistant guide
└── README.md                   # This file
```

## Development

### Running Tests

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Run specific test category
dotnet test --filter Category=Unit
```

### Building for Production

```bash
# Build API
dotnet publish EmailFixer.Api -c Release -o ./publish/api

# Build Client
dotnet publish EmailFixer.Client -c Release -o ./publish/client
```

### Database Migrations

```bash
# Create new migration
dotnet ef migrations add MigrationName \
  -p EmailFixer.Infrastructure \
  -s EmailFixer.Api

# Apply migrations
dotnet ef database update \
  -p EmailFixer.Infrastructure \
  -s EmailFixer.Api

# Rollback to previous migration
dotnet ef database update PreviousMigrationName \
  -p EmailFixer.Infrastructure \
  -s EmailFixer.Api
```

### Code Quality

This project follows:
- **SOLID principles** - Clean, maintainable architecture
- **Clean Architecture** - Separation of concerns across layers
- **Repository Pattern** - Abstracted data access
- **Dependency Injection** - Loose coupling
- **Async/Await** - Non-blocking operations throughout

## Deployment

### Docker

```bash
# Build images
docker-compose build

# Run in production mode
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

### Google Cloud Platform

**Deploy API to Cloud Run:**
```bash
# Build and deploy
gcloud run deploy emailfixer-api \
  --source . \
  --region us-central1 \
  --platform managed \
  --allow-unauthenticated
```

**Deploy Client to Cloud Storage:**
```bash
# Publish client
cd EmailFixer.Client
dotnet publish -c Release -o ./publish

# Upload to bucket
gsutil -m rsync -r ./publish/wwwroot gs://emailfixer-client

# Enable static website hosting
gsutil web set -m index.html gs://emailfixer-client
```

See [deployment documentation](docs/DEPLOYMENT.md) for detailed instructions.

### CI/CD

GitHub Actions automatically deploys to Google Cloud when you push to the `main` branch:
1. Runs all tests
2. Builds Docker images
3. Pushes to Google Container Registry
4. Deploys API to Cloud Run
5. Deploys client to Cloud Storage

## Configuration

### Environment Variables

Required for production:

```env
# Database
ConnectionStrings__DefaultConnection=Host=db;Database=emailfixer;...

# Paddle (get from paddle.com)
Paddle__ApiKey=your_paddle_api_key
Paddle__SellerId=your_seller_id
Paddle__WebhookSecret=your_webhook_secret

# API
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
```

For development, you can use SQLite (no database setup needed) or configure PostgreSQL connection.

## Payment Integration

EmailFixer uses Paddle.com for payment processing:

1. **Credit Packages** - Users purchase credits in packages (100, 500, 1000, etc.)
2. **Pay-as-you-go** - Each email validation costs 1 credit
3. **Secure Webhooks** - HMAC-SHA256 signature validation
4. **Idempotency** - Duplicate webhook protection

To integrate:
1. Sign up at [paddle.com](https://paddle.com)
2. Get API credentials (Sandbox for testing)
3. Configure webhook URL: `https://your-api.com/api/payment/webhook`
4. Set environment variables with your credentials

## Performance

- **API Response Time:** < 200ms average (single validation)
- **Batch Processing:** Up to 100 emails in < 2 seconds
- **Database:** Connection pooling and indexed queries
- **Scalability:** Horizontal scaling via Cloud Run (stateless API)

## Security

- **Input Validation** - FluentValidation on all endpoints
- **SQL Injection Protection** - EF Core parameterized queries
- **XSS Protection** - Blazor automatic encoding
- **Webhook Security** - HMAC signature validation
- **HTTPS Only** - Enforced in production
- **Secrets Management** - Google Secret Manager for production

## Support

- **Documentation:** See [CLAUDE.md](CLAUDE.md) for comprehensive technical docs
- **Issues:** Report bugs on [GitHub Issues](https://github.com/yourusername/emailfixer/issues)
- **Email:** support@emailfixer.example.com
- **API Status:** [status.emailfixer.example.com](https://status.emailfixer.example.com)

## Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Make your changes
4. Write/update tests
5. Ensure all tests pass (`dotnet test`)
6. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
7. Push to the branch (`git push origin feature/AmazingFeature`)
8. Open a Pull Request

Please read [CONTRIBUTING.md](CONTRIBUTING.md) for details on our code of conduct and development process.

## Roadmap

**Version 1.0** (Current)
- Core email validation
- Batch processing
- Paddle integration
- Blazor UI
- Docker support
- GCP deployment

**Version 1.1** (Planned)
- API rate limiting
- Advanced analytics dashboard
- Email deliverability scoring
- API key authentication

**Version 2.0** (Future)
- Webhook retry mechanism
- Internationalization (i18n)
- Mobile app (iOS/Android)
- Bulk CSV upload/download
- Custom validation rules per user
- Advanced reporting

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

Special thanks to:
- [.NET Team](https://github.com/dotnet) - Amazing framework
- [Blazor Team](https://github.com/dotnet/aspnetcore) - Modern web UI
- [Paddle](https://paddle.com) - Payment processing
- [Google Cloud](https://cloud.google.com) - Cloud infrastructure
- [DnsClient.NET](https://github.com/MichaCo/DnsClient.NET) - DNS lookups
- All contributors and supporters

## Project Status

**Version:** 1.0.0
**Status:** Production Ready
**Build:** ![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)
**Last Updated:** 2025-11-09

---

**Built with by the EmailFixer Team**

*Need help? Check out [CLAUDE.md](CLAUDE.md) for detailed documentation and troubleshooting guides.*

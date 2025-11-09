# 🚀 Phase 2: API Development Coordinator

**Phase ID:** phase2-api
**Parent Plan:** [emailfixer-completion-plan.md](emailfixer-completion-plan.md)
**Duration:** 4 hours
**Dependencies:** Phase 1 (Database Setup)
**Priority:** P0 - Critical Path
**Parallel Execution:** Controllers can be developed simultaneously

## 📋 Phase Overview

Implement REST API endpoints with proper DI configuration, controllers, middleware, and validation. This phase enables all client-server interactions and business logic execution.

## ⚡ Parallel Execution Strategy

```mermaid
Task 2.1 (DI Setup) [30 min]
    ├─→ Task 2.2 (EmailValidation) [45 min]
    ├─→ Task 2.3 (UserController) [40 min]
    ├─→ Task 2.4 (PaymentController) [50 min]
    └─→ Task 2.5 (Middleware) [30 min]
         └─→ Task 2.6 (Validation) [25 min]
              └─→ Task 2.7 (Swagger) [20 min]
```

**Optimal distribution for 3 developers:**
- Dev A: 2.1 → 2.2 → 2.7
- Dev B: Wait for 2.1 → 2.3 → 2.5
- Dev C: Wait for 2.1 → 2.4 → 2.6

## 📝 Task Breakdown

### Task 2.1: Configure Dependency Injection
**Duration:** 30 minutes
**LLM Readiness:** 100%
**Blocking:** All other API tasks

**File:** `EmailFixer.Api/Program.cs`

```csharp
using EmailFixer.Core.Services;
using EmailFixer.Infrastructure.Data;
using EmailFixer.Infrastructure.Repositories;
using EmailFixer.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database Configuration
builder.Services.AddDbContext<EmailFixerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repository Registration
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IEmailCheckRepository, EmailCheckRepository>();
builder.Services.AddScoped<ICreditTransactionRepository, CreditTransactionRepository>();

// Core Services
builder.Services.AddSingleton<IEmailValidator, EmailValidator>();

// Payment Services
builder.Services.Configure<PaddleConfiguration>(
    builder.Configuration.GetSection("Paddle"));
builder.Services.AddScoped<IPaddlePaymentService, PaddlePaymentService>();

// HTTP Client
builder.Services.AddHttpClient("Paddle", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Paddle:ApiUrl"]);
    client.DefaultRequestHeaders.Add("Authorization",
        $"Bearer {builder.Configuration["Paddle:ApiKey"]}");
});

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorClient", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5000",
            "https://localhost:5001",
            "http://localhost:80")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("BlazorClient");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Seed database if requested
if (args.Contains("--seed-database"))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<EmailFixerDbContext>();
    await DatabaseSeeder.SeedAsync(context);
}

app.Run();
```

**Validation:**
```powershell
# Test DI configuration
dotnet build EmailFixer.Api
dotnet run --project EmailFixer.Api
# Navigate to: https://localhost:5001/swagger
```

### Task 2.2: Email Validation Controller
**Duration:** 45 minutes
**LLM Readiness:** 95%
**Dependencies:** Task 2.1

**File:** `EmailFixer.Api/Controllers/EmailValidationController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using EmailFixer.Core.Services;
using EmailFixer.Infrastructure.Repositories;
using EmailFixer.Api.Models;

namespace EmailFixer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmailValidationController : ControllerBase
{
    private readonly IEmailValidator _emailValidator;
    private readonly IUserRepository _userRepository;
    private readonly IEmailCheckRepository _emailCheckRepository;
    private readonly ILogger<EmailValidationController> _logger;

    public EmailValidationController(
        IEmailValidator emailValidator,
        IUserRepository userRepository,
        IEmailCheckRepository emailCheckRepository,
        ILogger<EmailValidationController> logger)
    {
        _emailValidator = emailValidator;
        _userRepository = userRepository;
        _emailCheckRepository = emailCheckRepository;
        _logger = logger;
    }

    /// <summary>
    /// Validates a single email address
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(EmailValidationResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 402)]
    public async Task<IActionResult> ValidateSingle([FromBody] EmailValidationRequest request)
    {
        // Check user credits
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            return NotFound(new ErrorResponse { Message = "User not found" });

        if (user.Credits < 1)
            return StatusCode(402, new ErrorResponse { Message = "Insufficient credits" });

        // Validate email
        var result = await _emailValidator.ValidateEmailAsync(request.Email);

        // Deduct credit and save history
        user.Credits -= 1;
        await _userRepository.UpdateAsync(user);

        var emailCheck = new EmailCheck
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Email = request.Email,
            IsValid = result.IsValid,
            Suggestion = result.Suggestion,
            CheckedAt = DateTime.UtcNow,
            CreditsUsed = 1
        };

        await _emailCheckRepository.CreateAsync(emailCheck);

        return Ok(new EmailValidationResponse
        {
            Email = request.Email,
            IsValid = result.IsValid,
            Status = result.Status.ToString(),
            Suggestion = result.Suggestion,
            RemainingCredits = user.Credits
        });
    }

    /// <summary>
    /// Validates multiple email addresses in batch
    /// </summary>
    [HttpPost("validate-batch")]
    [ProducesResponseType(typeof(BatchEmailValidationResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 402)]
    public async Task<IActionResult> ValidateBatch([FromBody] BatchEmailValidationRequest request)
    {
        if (request.Emails.Count > 100)
            return BadRequest(new ErrorResponse { Message = "Maximum 100 emails per batch" });

        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            return NotFound(new ErrorResponse { Message = "User not found" });

        if (user.Credits < request.Emails.Count)
            return StatusCode(402, new ErrorResponse
            {
                Message = $"Insufficient credits. Required: {request.Emails.Count}, Available: {user.Credits}"
            });

        var results = new List<EmailValidationResult>();

        foreach (var email in request.Emails)
        {
            var validationResult = await _emailValidator.ValidateEmailAsync(email);
            results.Add(new EmailValidationResult
            {
                Email = email,
                IsValid = validationResult.IsValid,
                Status = validationResult.Status.ToString(),
                Suggestion = validationResult.Suggestion
            });

            // Save to history
            await _emailCheckRepository.CreateAsync(new EmailCheck
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Email = email,
                IsValid = validationResult.IsValid,
                Suggestion = validationResult.Suggestion,
                CheckedAt = DateTime.UtcNow,
                CreditsUsed = 1
            });
        }

        // Deduct credits
        user.Credits -= request.Emails.Count;
        await _userRepository.UpdateAsync(user);

        return Ok(new BatchEmailValidationResponse
        {
            Results = results,
            TotalProcessed = results.Count,
            CreditsUsed = request.Emails.Count,
            RemainingCredits = user.Credits
        });
    }
}
```

**Model Files:**

`EmailFixer.Api/Models/EmailValidationModels.cs`:
```csharp
public class EmailValidationRequest
{
    public Guid UserId { get; set; }
    public string Email { get; set; }
}

public class BatchEmailValidationRequest
{
    public Guid UserId { get; set; }
    public List<string> Emails { get; set; }
}

public class EmailValidationResponse
{
    public string Email { get; set; }
    public bool IsValid { get; set; }
    public string Status { get; set; }
    public string Suggestion { get; set; }
    public int RemainingCredits { get; set; }
}

public class BatchEmailValidationResponse
{
    public List<EmailValidationResult> Results { get; set; }
    public int TotalProcessed { get; set; }
    public int CreditsUsed { get; set; }
    public int RemainingCredits { get; set; }
}

public class EmailValidationResult
{
    public string Email { get; set; }
    public bool IsValid { get; set; }
    public string Status { get; set; }
    public string Suggestion { get; set; }
}
```

### Task 2.3: User Controller
**Duration:** 40 minutes
**LLM Readiness:** 95%
**Dependencies:** Task 2.1

**File:** `EmailFixer.Api/Controllers/UserController.cs`

```csharp
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailCheckRepository _emailCheckRepository;

    public UserController(
        IUserRepository userRepository,
        IEmailCheckRepository emailCheckRepository)
    {
        _userRepository = userRepository;
        _emailCheckRepository = emailCheckRepository;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return NotFound();

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Credits = user.Credits,
            CreatedAt = user.CreatedAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
            return Conflict(new { message = "User already exists" });

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            Credits = 10, // Free trial credits
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _userRepository.CreateAsync(user);

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Credits = user.Credits,
            CreatedAt = user.CreatedAt
        });
    }

    [HttpGet("{id}/credits")]
    public async Task<IActionResult> GetCredits(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return NotFound();

        return Ok(new { credits = user.Credits });
    }

    [HttpPut("{id}/credits")]
    public async Task<IActionResult> UpdateCredits(Guid id, [FromBody] UpdateCreditsRequest request)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return NotFound();

        user.Credits = request.Credits;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        return Ok(new { credits = user.Credits });
    }

    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetHistory(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return NotFound();

        var history = await _emailCheckRepository.GetByUserIdAsync(id, page, pageSize);

        return Ok(new UserHistoryResponse
        {
            Items = history.Select(h => new EmailCheckDto
            {
                Id = h.Id,
                Email = h.Email,
                IsValid = h.IsValid,
                Suggestion = h.Suggestion,
                CheckedAt = h.CheckedAt,
                CreditsUsed = h.CreditsUsed
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = await _emailCheckRepository.CountByUserIdAsync(id)
        });
    }
}
```

### Task 2.4: Payment Controller
**Duration:** 50 minutes
**LLM Readiness:** 90%
**Dependencies:** Task 2.1

```csharp
[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IPaddlePaymentService _paddleService;
    private readonly IUserRepository _userRepository;
    private readonly ICreditTransactionRepository _transactionRepository;
    private readonly IConfiguration _configuration;

    // Implementation continues...
    // [Truncated for brevity - full implementation in actual file]
}
```

### Task 2.5: Global Exception Middleware
**Duration:** 30 minutes
**LLM Readiness:** 100%

```csharp
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var response = new ErrorResponse
        {
            Message = "An error occurred processing your request",
            Details = exception.Message, // Remove in production
            TraceId = Activity.Current?.Id ?? context.TraceIdentifier
        };

        var jsonResponse = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(jsonResponse);
    }
}
```

### Task 2.6: Request Validation (FluentValidation)
**Duration:** 25 minutes
**LLM Readiness:** 100%

```powershell
# Install FluentValidation
dotnet add EmailFixer.Api package FluentValidation.AspNetCore
```

```csharp
public class EmailValidationRequestValidator : AbstractValidator<EmailValidationRequest>
{
    public EmailValidationRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(254);
    }
}

public class BatchEmailValidationRequestValidator : AbstractValidator<BatchEmailValidationRequest>
{
    public BatchEmailValidationRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Emails)
            .NotEmpty()
            .Must(x => x.Count <= 100)
            .WithMessage("Maximum 100 emails per batch");

        RuleForEach(x => x.Emails)
            .EmailAddress()
            .MaximumLength(254);
    }
}
```

### Task 2.7: Swagger Configuration
**Duration:** 20 minutes
**LLM Readiness:** 100%

```csharp
// In Program.cs
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EmailFixer API",
        Version = "v1",
        Description = "Email validation service with credit-based billing",
        Contact = new OpenApiContact
        {
            Name = "EmailFixer Support",
            Email = "support@emailfixer.com"
        }
    });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    // Add example schemas
    options.ExampleFilters();
});

// Enable XML documentation in .csproj
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

## 🔄 Integration Testing

```powershell
# Test all endpoints
dotnet test EmailFixer.Api.Tests

# Manual testing with curl
# Create user
curl -X POST https://localhost:5001/api/users `
  -H "Content-Type: application/json" `
  -d '{"email":"test@example.com"}'

# Validate email
curl -X POST https://localhost:5001/api/email/validate `
  -H "Content-Type: application/json" `
  -d '{"userId":"[user-id]","email":"test@gmail.com"}'
```

## ✅ Phase Completion Checklist

- [ ] DI configuration complete and tested
- [ ] All controllers created and functional
- [ ] Email validation endpoints working
- [ ] User management endpoints working
- [ ] Payment endpoints configured
- [ ] Global exception handling active
- [ ] Request validation implemented
- [ ] Swagger UI accessible and documented
- [ ] All endpoints return proper HTTP status codes
- [ ] CORS configured for Blazor client

## 🚨 Load Testing

```powershell
# Using Apache Bench (install first)
ab -n 1000 -c 10 -T application/json `
  -p test_payload.json `
  https://localhost:5001/api/email/validate

# Expected metrics:
# - Response time: < 200ms (95th percentile)
# - Throughput: > 50 requests/second
# - Error rate: < 0.1%
```

## 📊 Success Metrics

- ✅ All endpoints respond < 200ms
- ✅ Swagger documentation complete
- ✅ No compilation warnings
- ✅ FluentValidation on all inputs
- ✅ Proper HTTP status codes
- ✅ Structured error responses

## 🔗 Next Phase

After successful completion:
1. ✅ Mark Phase 2 complete in master plan
2. ➡️ Proceed to [Phase 3: Client Development](phase3-client-coordinator.md)
3. 🔀 Or start [Phase 4: Containerization](phase4-containerization-coordinator.md) in parallel
4. 📝 Document any API contract changes

---

**Estimated Time:** 4 hours
**Actual Time:** _[To be filled by executor]_
**Executor Notes:** _[To be filled by executor]_
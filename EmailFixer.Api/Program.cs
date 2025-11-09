using EmailFixer.Api.Middleware;
using EmailFixer.Api.Validators;
using EmailFixer.Core.Validators;
using EmailFixer.Infrastructure.Data;
using EmailFixer.Infrastructure.Data.Repositories;
using EmailFixer.Infrastructure.Services.Payment;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "EmailFixer API",
        Version = "v1",
        Description = "Email validation service with credit-based billing powered by Paddle.com",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "EmailFixer Support",
            Email = "support@emailfixer.com"
        }
    });

    // Include XML comments for better Swagger documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, xmlFile);
    if (System.IO.File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<EmailValidationRequestValidator>();

// Database Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<EmailFixerDbContext>(options =>
{
    // Use SQLite for development if PostgreSQL is not available
    if (builder.Environment.IsDevelopment() && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("USE_POSTGRES")))
    {
        options.UseSqlite("Data Source=emailfixer.db");
    }
    else
    {
        options.UseNpgsql(connectionString);
    }
});

// Repository Registration
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IEmailCheckRepository, EmailCheckRepository>();
builder.Services.AddScoped<ICreditTransactionRepository, CreditTransactionRepository>();

// Core Services
builder.Services.AddSingleton<IEmailValidator, EmailValidator>();

// Payment Services
var paddleConfig = new PaddleConfiguration(
    apiKey: builder.Configuration["Paddle:ApiKey"] ?? "test-api-key",
    sellerId: builder.Configuration["Paddle:SellerId"] ?? "test-seller-id",
    webhookSecret: builder.Configuration["Paddle:WebhookSecret"] ?? "test-webhook-secret",
    apiBaseUrl: builder.Configuration["Paddle:ApiBaseUrl"] ?? "https://sandbox-api.paddle.com"
);
builder.Services.AddSingleton(paddleConfig);
builder.Services.AddScoped<IPaddlePaymentService, PaddlePaymentService>();

// HTTP Client for Paddle
builder.Services.AddHttpClient("Paddle", client =>
{
    var paddleApiUrl = builder.Configuration["Paddle:ApiUrl"] ?? "https://sandbox-api.paddle.com";
    client.BaseAddress = new Uri(paddleApiUrl);

    var apiKey = builder.Configuration["Paddle:ApiKey"];
    if (!string.IsNullOrEmpty(apiKey))
    {
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }
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

// Configure the HTTP request pipeline.
app.UseGlobalExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("BlazorClient");
app.UseHttpsRedirection();
app.UseAuthorization();

// Health check endpoint for Docker and Kubernetes
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithTags("Health");

app.MapControllers();

// Seed database if requested
if (args.Contains("--seed-database"))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<EmailFixerDbContext>();
    // TODO: Implement DatabaseSeeder if needed
    // await DatabaseSeeder.SeedAsync(context);
}

app.Run();

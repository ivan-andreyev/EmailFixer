# Google OAuth Authentication Architecture
## EmailFixer Email Validation Platform

**Status**: Detailed Architecture Plan
**Created**: 2025-11-09
**Revision**: 1.0

---

## Table of Contents
1. [Overview](#overview)
2. [Phase 1: Database Schema](#phase-1-database-schema)
3. [Phase 2: Backend API](#phase-2-backend-api)
4. [Phase 3: Frontend Blazor](#phase-3-frontend-blazor)
5. [Phase 4: Google OAuth Configuration](#phase-4-google-oauth-configuration)
6. [Phase 5: Security & Deployment](#phase-5-security--deployment)
7. [Testing Strategy](#testing-strategy)

---

## Overview

### Current State
- ✅ User model exists in database
- ✅ Payment system exists (Paddle integration)
- ✅ Credit system exists
- ❌ No authentication/authorization
- ❌ All users are guests (no persistence)
- ❌ Payments not linked to users

### End State
- ✅ Google OAuth login/logout
- ✅ JWT token-based authentication
- ✅ Users can authenticate and stay logged in
- ✅ Payments linked to authenticated user
- ✅ Credit history per user
- ✅ Email validation history per user

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                     Blazor WebAssembly                      │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  LoginPage                                            │  │
│  │  - Google OAuth Button                               │  │
│  │  - Redirect to Google                                │  │
│  └───────────────────────────────────────────────────────┘  │
│                           │                                  │
│                           ▼                                  │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  AuthService (Token Management)                       │  │
│  │  - Store JWT in localStorage                         │  │
│  │  - Add Bearer token to API requests                  │  │
│  │  - Handle token refresh                              │  │
│  │  - Logout                                            │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
         │                                           │
         │ HTTPS                                     │
         ▼                                           ▼
┌─────────────────────────────────────────────────────────────┐
│                    ASP.NET Core API                         │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  AuthController                                      │  │
│  │  - POST /auth/google-callback (code)                │  │
│  │  - GET /auth/user (current user)                    │  │
│  │  - POST /auth/logout                                │  │
│  └───────────────────────────────────────────────────────┘  │
│                           │                                  │
│                           ▼                                  │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  AuthService (Backend)                               │  │
│  │  - Exchange Google code for tokens                   │  │
│  │  - Create JWT from user info                        │  │
│  │  - Validate JWT                                     │  │
│  │  - User creation/lookup                             │  │
│  └───────────────────────────────────────────────────────┘  │
│                           │                                  │
│                           ▼                                  │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  [Authorize] Attributes                             │  │
│  │  - Protect API endpoints                            │  │
│  │  - Extract userId from JWT                          │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
         │
         │
         ▼
┌─────────────────────────────────────────────────────────────┐
│                    PostgreSQL Database                      │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  Users (Modified)                                    │  │
│  │  - Id                                               │  │
│  │  - Email                                            │  │
│  │  - GoogleId (external ID)                           │  │
│  │  - CreditsAvailable                                 │  │
│  │  - CreatedAt                                        │  │
│  │  - AuthProvider (enum: Google)                      │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

---

## Phase 1: Database Schema

### Task 1.1: Update User Model

**File**: `EmailFixer.Shared/Models/UserModel.cs`

```csharp
public class UserModel
{
    public Guid Id { get; set; }
    public string Email { get; set; }

    // OAuth
    public string? GoogleId { get; set; }
    public string AuthProvider { get; set; } = "google"; // or "local" for future

    // Credits
    public int CreditsAvailable { get; set; }

    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
}
```

### Task 1.2: Create EF Core Migration

**Commands**:
```bash
dotnet ef migrations add AddGoogleOAuthToUsers -p EmailFixer.Api
dotnet ef database update -p EmailFixer.Api
```

**Migration Details**:
- Add `GoogleId` column (nullable string, unique index)
- Add `AuthProvider` column (string, default: "google")
- Add `LastLoginAt` column (DateTime nullable)
- Add `IsActive` column (boolean)
- Add unique constraint on `(Email, AuthProvider)` pair

### Task 1.3: Create OAuthProvider Table (Optional, for future extensibility)

```sql
CREATE TABLE OAuthProviders (
    Id UUID PRIMARY KEY,
    UserId UUID REFERENCES Users(Id) ON DELETE CASCADE,
    Provider VARCHAR(50) NOT NULL, -- "google", "microsoft", etc.
    ExternalId VARCHAR(255) NOT NULL,
    Email VARCHAR(255),
    LinkedAt TIMESTAMP,
    UNIQUE(Provider, ExternalId)
);
```

**Rationale**: Allows users to link multiple OAuth providers to one account

---

## Phase 2: Backend API

### Task 2.1: Add Authentication NuGet Packages

```bash
dotnet add package Microsoft.IdentityModel.Tokens
dotnet add package System.IdentityModel.Tokens.Jwt
dotnet add package Google.Apis.Auth
```

### Task 2.2: Create JWT Configuration

**File**: `EmailFixer.Api/Configuration/JwtSettings.cs`

```csharp
public class JwtSettings
{
    public string SecretKey { get; set; }
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public int ExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
```

**appsettings.json**:
```json
{
  "Jwt": {
    "SecretKey": "${JWT_SECRET_KEY}",
    "Issuer": "emailfixer-api",
    "Audience": "emailfixer-client",
    "ExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  },
  "Google": {
    "ClientId": "${GOOGLE_CLIENT_ID}",
    "ClientSecret": "${GOOGLE_CLIENT_SECRET}",
    "RedirectUri": "https://emailfixer-client.../auth/callback"
  }
}
```

### Task 2.3: Create AuthService (Backend)

**File**: `EmailFixer.Api/Services/AuthService.cs`

```csharp
public interface IAuthService
{
    Task<AuthResponse> GoogleCallbackAsync(string code, string codeVerifier);
    Task<UserModel> GetOrCreateUserAsync(string email, string googleId);
    Task UpdateLastLoginAsync(Guid userId);
    string GenerateJwtToken(UserModel user);
    string GenerateRefreshToken();
    Task<UserModel> ValidateTokenAsync(string token);
}

public class AuthService : IAuthService
{
    private readonly IUserService _userService;
    private readonly EmailFixerDbContext _dbContext;
    private readonly JwtSettings _jwtSettings;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public async Task<AuthResponse> GoogleCallbackAsync(string code, string codeVerifier)
    {
        // 1. Exchange authorization code for Google tokens
        var googleTokens = await ExchangeCodeForTokensAsync(code);

        // 2. Validate ID token and extract claims
        var payload = await ValidateGoogleTokenAsync(googleTokens.IdToken);

        // 3. Get or create user
        var user = await GetOrCreateUserAsync(
            payload["email"].ToString(),
            payload["sub"].ToString()
        );

        // 4. Update last login
        await UpdateLastLoginAsync(user.Id);

        // 5. Generate JWT
        var jwtToken = GenerateJwtToken(user);

        return new AuthResponse
        {
            AccessToken = jwtToken,
            User = user,
            ExpiresIn = _jwtSettings.ExpirationMinutes * 60
        };
    }

    private async Task<GoogleTokenResponse> ExchangeCodeForTokensAsync(string code)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "client_id", _configuration["Google:ClientId"] },
                { "client_secret", _configuration["Google:ClientSecret"] },
                { "code", code },
                { "grant_type", "authorization_code" },
                { "redirect_uri", _configuration["Google:RedirectUri"] }
            })
        };

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GoogleTokenResponse>(json);
    }

    private async Task<Dictionary<string, object>> ValidateGoogleTokenAsync(string idToken)
    {
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _configuration["Google:ClientId"] }
            });

            return new Dictionary<string, object>
            {
                { "sub", payload.Subject },
                { "email", payload.Email },
                { "name", payload.Name }
            };
        }
        catch (Exception ex)
        {
            throw new UnauthorizedAccessException("Invalid Google token", ex);
        }
    }

    public string GenerateJwtToken(UserModel user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("GoogleId", user.GoogleId ?? string.Empty)
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<UserModel> GetOrCreateUserAsync(string email, string googleId)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.AuthProvider == "google");

        if (user == null)
        {
            user = new UserModel
            {
                Id = Guid.NewGuid(),
                Email = email,
                GoogleId = googleId,
                AuthProvider = "google",
                CreditsAvailable = 10, // Welcome credits
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
        }

        return user;
    }

    public async Task UpdateLastLoginAsync(Guid userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user != null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }
    }
}
```

### Task 2.4: Create Auth Endpoints

**File**: `EmailFixer.Api/Controllers/AuthController.cs`

```csharp
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    [HttpPost("google-callback")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> GoogleCallback([FromBody] GoogleCallbackRequest request)
    {
        try
        {
            var response = await _authService.GoogleCallbackAsync(request.Code, request.CodeVerifier);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    [HttpGet("user")]
    [Authorize]
    public async Task<ActionResult<UserModel>> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var id))
            return Unauthorized();

        var user = await _authService.GetUserByIdAsync(id);
        return user != null ? Ok(user) : NotFound();
    }

    [HttpPost("logout")]
    [Authorize]
    public ActionResult Logout()
    {
        // JWT is stateless, logout happens on client side
        // Could invalidate tokens in cache if needed
        return Ok(new { message = "Logged out successfully" });
    }
}
```

### Task 2.5: Configure JWT Authentication in Program.cs

```csharp
// Add JWT authentication
var jwtSettings = new JwtSettings();
builder.Configuration.GetSection("Jwt").Bind(jwtSettings);
builder.Services.AddSingleton(jwtSettings);

var key = Encoding.ASCII.GetBytes(jwtSettings.SecretKey);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Add services
builder.Services.AddScoped<IAuthService, AuthService>();

// Use authentication
app.UseAuthentication();
app.UseAuthorization();
```

### Task 2.6: Protect Email Validation Endpoints

Update existing endpoints to require `[Authorize]`:

```csharp
[ApiController]
[Route("api/email")]
[Authorize]  // Add this
public class EmailValidationController : ControllerBase
{
    [HttpPost("validate")]
    public async Task<ActionResult<EmailCheckModel>> ValidateSingle([FromBody] ValidateEmailRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        // ... use userId to deduct credits from specific user
    }
}
```

---

## Phase 3: Frontend Blazor

### Task 3.1: Create Login Page

**File**: `EmailFixer.Client/Pages/Login.razor`

```razor
@page "/login"
@inject NavigationManager NavigationManager
@inject IAuthService AuthService
@inject INotificationService NotificationService

<PageTitle>Login - Email Fixer</PageTitle>

<div class="login-container">
    <div class="login-card">
        <h1 class="text-center mb-4">
            <i class="fas fa-envelope-open-text me-2"></i> Email Fixer
        </h1>

        <p class="text-center text-muted mb-4">
            Sign in to access your email validation service
        </p>

        <button class="btn btn-primary w-100 mb-3" @onclick="LoginWithGoogle">
            <i class="fab fa-google me-2"></i> Continue with Google
        </button>

        <p class="text-center text-muted small">
            By signing in, you agree to our Terms of Service
        </p>
    </div>
</div>

@code {
    private async Task LoginWithGoogle()
    {
        // Generate PKCE parameters
        var (codeChallenge, codeVerifier) = GeneratePKCE();

        // Store codeVerifier in sessionStorage
        await AuthService.StoreCodeVerifierAsync(codeVerifier);

        // Redirect to Google OAuth
        var googleAuthUrl = $"""
            https://accounts.google.com/o/oauth2/v2/auth?
            client_id={YOUR_GOOGLE_CLIENT_ID}
            &redirect_uri={Uri.EscapeDataString("https://your-app/auth/callback")}
            &response_type=code
            &scope={Uri.EscapeDataString("openid email profile")}
            &code_challenge={codeChallenge}
            &code_challenge_method=S256
            """;

        NavigationManager.NavigateTo(googleAuthUrl, forceLoad: true);
    }

    private (string challenge, string verifier) GeneratePKCE()
    {
        using var rng = new System.Security.Cryptography.RNGCryptoServiceProvider();
        byte[] tokenData = new byte[32];
        rng.GetBytes(tokenData);
        string verifier = Base64UrlEncode(tokenData);
        string challenge = Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        return (challenge, verifier);
    }

    private static string Base64UrlEncode(byte[] buffer)
    {
        return Convert.ToBase64String(buffer)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
```

### Task 3.2: Create Auth Callback Page

**File**: `EmailFixer.Client/Pages/AuthCallback.razor`

```razor
@page "/auth/callback"
@inject NavigationManager NavigationManager
@inject IAuthService AuthService
@inject INotificationService NotificationService

<PageTitle>Authenticating...</PageTitle>

<div class="login-container">
    <div class="login-card">
        <div class="text-center">
            <div class="spinner-border mb-3" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
            <p>Authenticating with Google...</p>
        </div>
    </div>
</div>

@code {
    protected override async Task OnInitializedAsync()
    {
        try
        {
            // Get authorization code from URL
            var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
            var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);

            if (!queryParams.TryGetValue("code", out var code))
            {
                if (queryParams.TryGetValue("error", out var error))
                {
                    await NotificationService.ShowError($"Authentication failed: {error}");
                }
                NavigationManager.NavigateTo("/login");
                return;
            }

            // Get stored code verifier
            var codeVerifier = await AuthService.GetStoredCodeVerifierAsync();
            if (string.IsNullOrEmpty(codeVerifier))
            {
                await NotificationService.ShowError("PKCE verification failed");
                NavigationManager.NavigateTo("/login");
                return;
            }

            // Exchange code for token via backend
            var response = await AuthService.GoogleCallbackAsync(code.ToString(), codeVerifier);

            if (response?.AccessToken != null)
            {
                // Store token
                await AuthService.StoreTokenAsync(response.AccessToken);
                await NotificationService.ShowSuccess($"Welcome {response.User.Email}!");
                NavigationManager.NavigateTo("/");
            }
            else
            {
                await NotificationService.ShowError("Failed to authenticate");
                NavigationManager.NavigateTo("/login");
            }
        }
        catch (Exception ex)
        {
            await NotificationService.ShowError($"Authentication error: {ex.Message}");
            NavigationManager.NavigateTo("/login");
        }
    }
}
```

### Task 3.3: Create AuthService (Frontend)

**File**: `EmailFixer.Client/Services/AuthService.cs`

```csharp
public interface IAuthService
{
    Task<AuthResponse> GoogleCallbackAsync(string code, string codeVerifier);
    Task StoreTokenAsync(string token);
    Task<string> GetTokenAsync();
    Task<bool> IsAuthenticatedAsync();
    Task LogoutAsync();
    Task StoreCodeVerifierAsync(string verifier);
    Task<string> GetStoredCodeVerifierAsync();
}

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private const string TOKEN_KEY = "emailfixer_token";
    private const string VERIFIER_KEY = "emailfixer_code_verifier";

    public AuthService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    public async Task<AuthResponse> GoogleCallbackAsync(string code, string codeVerifier)
    {
        var request = new GoogleCallbackRequest { Code = code, CodeVerifier = codeVerifier };
        var response = await _httpClient.PostAsJsonAsync("/api/auth/google-callback", request);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsAsync<AuthResponse>();
        }

        throw new Exception("Authentication failed");
    }

    public async Task StoreTokenAsync(string token)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TOKEN_KEY, token);
    }

    public async Task<string> GetTokenAsync()
    {
        return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TOKEN_KEY);
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await GetTokenAsync();
        return !string.IsNullOrEmpty(token);
    }

    public async Task LogoutAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TOKEN_KEY);
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", VERIFIER_KEY);
    }

    public async Task StoreCodeVerifierAsync(string verifier)
    {
        await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", VERIFIER_KEY, verifier);
    }

    public async Task<string> GetStoredCodeVerifierAsync()
    {
        return await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", VERIFIER_KEY);
    }
}
```

### Task 3.4: Create Custom HttpClientHandler for Bearer Token

**File**: `EmailFixer.Client/Services/AuthHttpClientHandler.cs`

```csharp
public class AuthHttpClientHandler : DelegatingHandler
{
    private readonly IAuthService _authService;

    public AuthHttpClientHandler(IAuthService authService)
    {
        _authService = authService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _authService.GetTokenAsync();

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
```

### Task 3.5: Update Program.cs for Frontend

```csharp
// Add HTTP client with auth handler
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuthHttpClientHandler>();
builder.Services.AddHttpClient<IEmailValidationService, EmailValidationService>()
    .AddHttpMessageHandler<AuthHttpClientHandler>();
```

### Task 3.6: Update MainLayout Navigation

```razor
@if (await AuthService.IsAuthenticatedAsync())
{
    <li class="nav-item">
        <a class="nav-link" href="/logout">
            <i class="fas fa-sign-out-alt me-1"></i> Logout
        </a>
    </li>
}
else
{
    <li class="nav-item">
        <a class="nav-link" href="/login">
            <i class="fas fa-sign-in-alt me-1"></i> Login
        </a>
    </li>
}
```

---

## Phase 4: Google OAuth Configuration

### Task 4.1: Create Google OAuth Application

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create new project: "EmailFixer"
3. Enable APIs:
   - Google+ API
   - Google Identity Services
4. Create OAuth 2.0 Credentials:
   - Type: Web application
   - Name: EmailFixer
   - Authorized JavaScript origins:
     - `https://emailfixer-client.../`
     - `http://localhost:3000`
   - Authorized redirect URIs:
     - `https://emailfixer-api.../api/auth/google-callback`
     - `http://localhost:5000/api/auth/google-callback`

5. Copy `Client ID` and `Client Secret`

### Task 4.2: Store Secrets in Google Cloud Secret Manager

```bash
# Create secrets
echo -n "YOUR_GOOGLE_CLIENT_ID" | gcloud secrets create google-client-id --data-file=-
echo -n "YOUR_GOOGLE_CLIENT_SECRET" | gcloud secrets create google-client-secret --data-file=-
echo -n "YOUR_JWT_SECRET_KEY" | gcloud secrets create jwt-secret-key --data-file=-

# Grant Cloud Run service account access
gcloud secrets add-iam-policy-binding google-client-id \
  --member=serviceAccount:$SA_EMAIL \
  --role=roles/secretmanager.secretAccessor
```

### Task 4.3: Update Cloud Run Deployment

Update `.github/workflows/deploy-gcp.yml`:

```yaml
- name: Deploy API to Cloud Run
  run: |
    gcloud run deploy emailfixer-api \
      --image ... \
      --set-secrets "Google__ClientId=google-client-id:latest" \
      --set-secrets "Google__ClientSecret=google-client-secret:latest" \
      --set-secrets "Jwt__SecretKey=jwt-secret-key:latest" \
      ...
```

---

## Phase 5: Security & Deployment

### Task 5.1: Security Considerations

**PKCE (Proof Key for Code Exchange)**
- Required for SPAs to prevent authorization code interception
- Implemented via code challenge/verifier flow

**Token Storage**
- Store JWT in `localStorage` (with secure flag if possible)
- Alternative: Use httpOnly cookies (requires backend support)

**HTTPS Only**
- All OAuth redirects must use HTTPS
- JWT validation requires HTTPS

**Token Refresh**
- Implement refresh token rotation
- Short-lived access tokens (60 minutes)
- Long-lived refresh tokens (7 days)

**CORS Configuration**
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorClient", policy =>
    {
        policy.WithOrigins("https://emailfixer-client...")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
```

### Task 5.2: Database Constraints

```sql
-- Ensure email uniqueness per provider
CREATE UNIQUE INDEX idx_email_provider ON users(email, auth_provider);

-- Ensure GoogleId uniqueness
CREATE UNIQUE INDEX idx_google_id ON users(google_id) WHERE google_id IS NOT NULL;

-- Add check constraint
ALTER TABLE users ADD CONSTRAINT chk_auth_provider
  CHECK (auth_provider IN ('google', 'local'));
```

### Task 5.3: Error Handling

**Backend**:
- Catch Google API errors
- Validate token signature
- Handle user creation race conditions

**Frontend**:
- Graceful fallback if auth fails
- Clear error messages
- Redirect to login on 401

### Task 5.4: Testing Strategy

#### Unit Tests
```csharp
[Fact]
public async Task GoogleCallback_ValidCode_CreatesUserAndReturnsToken()
{
    // Arrange
    var code = "valid_code";
    var authService = new AuthService(...);

    // Act
    var response = await authService.GoogleCallbackAsync(code, "verifier");

    // Assert
    response.AccessToken.Should().NotBeNullOrEmpty();
    response.User.Email.Should().Be("user@gmail.com");
}

[Fact]
public async Task GenerateJwtToken_ValidUser_ContainsCorrectClaims()
{
    var user = new UserModel { Id = Guid.NewGuid(), Email = "test@gmail.com" };
    var token = _authService.GenerateJwtToken(user);
    var handler = new JwtSecurityTokenHandler();
    var jwtToken = handler.ReadJwtToken(token);

    jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == "test@gmail.com");
}
```

#### Integration Tests
```csharp
[Fact]
public async Task AuthCallback_EndToEnd_ReturnsTokenAndUser()
{
    // Mock Google API response
    // POST /auth/google-callback with code
    // Verify JWT is returned
    // Verify user is created in DB
}
```

#### E2E Tests (Playwright)
```csharp
[Fact]
public async Task LoginFlow_UserClicksGoogleButton_RedirectsAndAuthenticates()
{
    await page.GotoAsync("https://emailfixer-client/login");
    await page.ClickAsync("button:has-text('Continue with Google')");
    // Mock OAuth flow
    // Verify redirected to home page
    // Verify logged in state
}
```

---

## Implementation Timeline

**Week 1 (Phase 1-2)**:
- Days 1-2: Database schema + migrations
- Days 3-4: Backend AuthService + endpoints
- Days 5: JWT configuration + testing

**Week 2 (Phase 3-4)**:
- Days 1-2: Frontend LoginPage + AuthCallback
- Days 3: AuthService + HttpClientHandler
- Days 4-5: Google OAuth setup + deployment

**Week 3 (Phase 5)**:
- Days 1-2: Security hardening + error handling
- Days 3-5: Testing (unit + integration + E2E)

---

## Deployment Checklist

- [ ] Google OAuth credentials created
- [ ] Secrets stored in Google Secret Manager
- [ ] Database migrations applied
- [ ] JWT settings configured
- [ ] AuthService implementation complete
- [ ] API endpoints secured with [Authorize]
- [ ] Frontend AuthService complete
- [ ] Login/Callback pages created
- [ ] HttpClientHandler adds Bearer token
- [ ] CORS policy configured
- [ ] Tests passing (unit + integration)
- [ ] E2E tests passing
- [ ] HTTPS enforced
- [ ] GitHub Actions updated
- [ ] Cloud Run deployment tested

---

## Environment Variables Required

```bash
# Backend
GOOGLE_CLIENT_ID=...
GOOGLE_CLIENT_SECRET=...
JWT_SECRET_KEY=...  # Min 32 chars
JWT_ISSUER=emailfixer-api
JWT_AUDIENCE=emailfixer-client
JWT_EXPIRATION_MINUTES=60

# Frontend (in appsettings.json)
GOOGLE_CLIENT_ID=...
AUTH_CALLBACK_URL=https://emailfixer-client/auth/callback
API_BASE_URL=https://emailfixer-api
```

---

## Success Criteria

✅ User can login with Google OAuth
✅ JWT token stored securely on client
✅ API requests include Bearer token
✅ Payments linked to authenticated user
✅ Credit history persists per user
✅ Logout clears token and redirects
✅ Session persists on page refresh
✅ Unauthorized requests return 401
✅ No hardcoded credentials
✅ All tests passing

---

## References

- [Google OAuth 2.0 for Blazor](https://developers.google.com/identity/protocols/oauth2/web-server-flow)
- [PKCE Authorization Flow](https://tools.ietf.org/html/rfc7636)
- [JWT Best Practices](https://tools.ietf.org/html/rfc8725)
- [ASP.NET Core Authentication](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/)
- [Blazor Authentication](https://docs.microsoft.com/en-us/aspnet/core/blazor/security/)

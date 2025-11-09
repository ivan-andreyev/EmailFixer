# Security Best Practices for EmailFixer

This document outlines security best practices for developing, deploying, and maintaining the EmailFixer application.

## Table of Contents

1. [Secrets Management](#secrets-management)
2. [OAuth Security](#oauth-security)
3. [JWT Token Security](#jwt-token-security)
4. [API Security](#api-security)
5. [Database Security](#database-security)
6. [Cloud Deployment Security](#cloud-deployment-security)
7. [Code Security](#code-security)
8. [Monitoring and Auditing](#monitoring-and-auditing)
9. [Incident Response](#incident-response)
10. [Compliance and Privacy](#compliance-and-privacy)

---

## Secrets Management

### Never Commit Secrets to Git

**Critical Rule:** Secrets must NEVER be committed to version control.

**What are secrets?**
- API keys (Google OAuth, Paddle, etc.)
- Client IDs and secrets
- JWT signing keys
- Database passwords
- Connection strings
- Encryption keys
- Service account credentials

**How to prevent commits:**

1. Use `.gitignore`:
   ```gitignore
   # Secrets and credentials
   *.key
   *.pem
   *.p12
   *credentials*.json
   google-oauth-credentials.json
   appsettings.*.local.json
   secrets.json

   # Environment files
   .env
   .env.local
   .env.*.local

   # User secrets
   secrets/
   ```

2. Use git hooks (pre-commit):
   ```bash
   # Install git-secrets
   brew install git-secrets  # Mac
   apt-get install git-secrets  # Linux

   # Initialize
   git secrets --install
   git secrets --register-aws

   # Add custom patterns
   git secrets --add 'GOCSPX-[A-Za-z0-9_-]+'
   git secrets --add 'AIza[0-9A-Za-z_-]{35}'
   ```

3. Scan existing repository:
   ```bash
   # Check for secrets
   git secrets --scan

   # Scan entire history
   git secrets --scan-history
   ```

### Use Appropriate Secret Storage

**Local Development:**
- .NET User Secrets (preferred)
- Environment variables
- Local credential managers (macOS Keychain, Windows Credential Manager)

**Never:**
- Hardcode in source files
- Store in appsettings.json (except placeholders)
- Share via email, Slack, or messaging apps

**Example (.NET User Secrets):**
```bash
# Initialize
dotnet user-secrets init --project EmailFixer.Api

# Set secrets
dotnet user-secrets set "GoogleOAuth:ClientId" "your-client-id"
dotnet user-secrets set "GoogleOAuth:ClientSecret" "your-secret"
dotnet user-secrets set "Jwt:Secret" "your-jwt-secret"

# View (local only!)
dotnet user-secrets list
```

**Production:**
- Google Secret Manager (Cloud Run)
- GitHub Secrets (CI/CD)
- Azure Key Vault (if using Azure)
- AWS Secrets Manager (if using AWS)

### Secret Rotation

**Policy:** Rotate all secrets every 90 days or immediately after:
- Employee departure
- Security incident
- Suspected compromise
- Major version upgrades

**Rotation process:**

1. Generate new secret
2. Add new secret to Secret Manager (new version)
3. Deploy application with new secret
4. Verify functionality
5. Delete old secret version
6. Update documentation

**Example (Google Secret Manager):**
```bash
# Create new version
echo -n "new-secret-value" | gcloud secrets versions add google-oauth-client-secret --data-file=-

# List versions
gcloud secrets versions list google-oauth-client-secret

# Deploy with new version
gcloud run deploy emailfixer-api --update-secrets=GoogleOAuth__ClientSecret=google-oauth-client-secret:latest

# Destroy old version (after verification)
gcloud secrets versions destroy 1 --secret=google-oauth-client-secret
```

### Access Control

**Principle of Least Privilege:**
- Grant minimum permissions required
- Use service accounts for automation
- Avoid sharing personal credentials

**GitHub Secrets:**
- Only repository admins can manage secrets
- Enable branch protection for production deployments
- Use environment-specific secrets (dev, staging, production)

**Google Cloud IAM:**
```bash
# Grant Secret Manager access to specific service account
gcloud secrets add-iam-policy-binding google-oauth-client-secret \
  --member="serviceAccount:emailfixer-api@emailfixer-prod.iam.gserviceaccount.com" \
  --role="roles/secretmanager.secretAccessor"
```

**Audit regularly:**
```bash
# List who has access to secrets
gcloud secrets get-iam-policy google-oauth-client-secret

# Review service account permissions
gcloud projects get-iam-policy emailfixer-prod \
  --flatten="bindings[].members" \
  --format="table(bindings.role, bindings.members)"
```

---

## OAuth Security

### OAuth 2.0 Configuration

**Use HTTPS in Production:**
- All redirect URIs must use HTTPS
- Never use HTTP in production (except localhost for development)

**Example:**
```
CORRECT (Production):
  https://emailfixer.com/auth-callback

WRONG (Production):
  http://emailfixer.com/auth-callback

ACCEPTABLE (Development):
  http://localhost:5000/auth-callback
```

### Validate Redirect URIs

**Exact match required:**
- No wildcards
- No trailing slashes (unless in configuration)
- Case-sensitive

**Google Cloud Console configuration:**
```
Authorized redirect URIs:
  http://localhost:5000/auth-callback        (dev)
  https://emailfixer.com/auth-callback       (prod)
  https://www.emailfixer.com/auth-callback   (prod alternative)
```

### Implement State Parameter

**Purpose:** Prevent CSRF (Cross-Site Request Forgery) attacks

**Implementation:**
```csharp
// Generate state
var state = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
HttpContext.Session.SetString("oauth_state", state);

// Include in OAuth URL
var authUrl = $"https://accounts.google.com/o/oauth2/v2/auth?state={state}&...";

// Validate on callback
var receivedState = Request.Query["state"];
var savedState = HttpContext.Session.GetString("oauth_state");
if (receivedState != savedState)
{
    throw new SecurityException("Invalid state parameter");
}
```

### Limit OAuth Scopes

**Request minimum scopes required:**
```
REQUIRED:
  openid
  email
  profile

AVOID (unless needed):
  https://www.googleapis.com/auth/gmail.readonly
  https://www.googleapis.com/auth/drive
```

### Handle Tokens Securely

**DO:**
- Store tokens in httpOnly cookies (prevents XSS)
- Use secure cookies in production
- Implement token expiration
- Validate tokens on every request

**DON'T:**
- Store tokens in localStorage or sessionStorage
- Log tokens (mask in logs)
- Send tokens in URLs
- Share tokens between users

**Example (Secure Cookie):**
```csharp
Response.Cookies.Append("auth_token", jwtToken, new CookieOptions
{
    HttpOnly = true,      // Prevent JavaScript access
    Secure = true,        // HTTPS only
    SameSite = SameSiteMode.Strict,
    Expires = DateTimeOffset.UtcNow.AddHours(1)
});
```

---

## JWT Token Security

### Strong Secret Keys

**Requirements:**
- Minimum 256 bits (32 characters for base64)
- Cryptographically random
- Never reuse across environments

**Generate secure secret:**
```bash
# Option 1: OpenSSL
openssl rand -base64 32

# Option 2: PowerShell
-join ((48..57) + (65..90) + (97..122) | Get-Random -Count 32 | % {[char]$_})

# Option 3: Python
python -c "import secrets; print(secrets.token_urlsafe(32))"
```

**Bad examples (NEVER use):**
```
❌ "secret"
❌ "my-jwt-secret"
❌ "12345678901234567890123456789012"
❌ "password123!@#$%^&*()"
```

**Good example:**
```
✅ "8KJH3k4j5h6g7f8d9s0a1q2w3e4r5t6y7u8i9o0p1a2s3d4f5g6h7j8k9l0"
```

### Token Expiration

**Configure appropriate expiration times:**
```json
{
  "Jwt": {
    "ExpirationMinutes": 60,           // Access token: 1 hour
    "RefreshExpirationDays": 7         // Refresh token: 7 days
  }
}
```

**Balance:**
- Short-lived access tokens (15-60 minutes): More secure
- Long-lived refresh tokens (7-30 days): Better UX
- Implement refresh token rotation

### Validate Claims

**Always validate:**
- `iss` (issuer): Matches your API
- `aud` (audience): Matches your client
- `exp` (expiration): Token not expired
- `nbf` (not before): Token valid now
- `sub` (subject): User exists
- Custom claims: User roles, permissions

**Example:**
```csharp
var tokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidIssuer = configuration["Jwt:Issuer"],

    ValidateAudience = true,
    ValidAudience = configuration["Jwt:Audience"],

    ValidateLifetime = true,
    ClockSkew = TimeSpan.FromMinutes(5),

    ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(configuration["Jwt:Secret"])
    )
};
```

### Token Revocation

**Implement token blacklist:**
```csharp
// Store revoked tokens (Redis, database, or memory cache)
public interface ITokenBlacklist
{
    Task RevokeToken(string tokenId);
    Task<bool> IsTokenRevoked(string tokenId);
}

// Check on every request
if (await tokenBlacklist.IsTokenRevoked(tokenId))
{
    return Unauthorized("Token has been revoked");
}
```

---

## API Security

### HTTPS/TLS

**Production requirements:**
- HTTPS everywhere
- TLS 1.2 or higher
- Valid SSL certificate
- HSTS (HTTP Strict Transport Security)

**Configuration:**
```csharp
// Program.cs
if (app.Environment.IsProduction())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Strict-Transport-Security",
        "max-age=31536000; includeSubDomains");
    await next();
});
```

### CORS Configuration

**Restrict origins:**
```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://emailfixer.com",
      "https://www.emailfixer.com"
    ],
    "AllowedMethods": ["GET", "POST", "PUT", "DELETE"],
    "AllowedHeaders": ["Content-Type", "Authorization"],
    "AllowCredentials": true,
    "MaxAge": 3600
  }
}
```

**NEVER use:**
```json
❌ "AllowedOrigins": ["*"]  // Allows any origin
❌ "AllowCredentials": true with wildcard origin
```

### Rate Limiting

**Protect against abuse:**
```json
{
  "RateLimiting": {
    "EnableRateLimiting": true,
    "PermitLimit": 100,        // 100 requests
    "WindowSeconds": 60,       // per 60 seconds
    "QueueLimit": 10           // 10 queued requests
  }
}
```

**Per-endpoint limits:**
```csharp
[RateLimit(PermitLimit = 10, Window = 60)]  // 10 requests per minute
[HttpPost("api/auth/login")]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    // ...
}
```

### Input Validation

**Validate all user input:**
```csharp
public class EmailValidationRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; }

    [Range(1, 1000)]
    public int BatchSize { get; set; } = 1;
}
```

**Sanitize output:**
```csharp
// Prevent XSS
public string SanitizeOutput(string input)
{
    return HttpUtility.HtmlEncode(input);
}
```

### Security Headers

**Add security headers:**
```csharp
app.Use(async (context, next) =>
{
    // Prevent clickjacking
    context.Response.Headers.Add("X-Frame-Options", "DENY");

    // Prevent MIME sniffing
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");

    // XSS protection
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");

    // Content Security Policy
    context.Response.Headers.Add("Content-Security-Policy",
        "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline';");

    // Referrer policy
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

    await next();
});
```

---

## Database Security

### Connection String Security

**Use Secret Manager:**
```json
// appsettings.Production.json
{
  "ConnectionStrings": {
    "DefaultConnection": "PLACEHOLDER"  // Replaced by Secret Manager
  }
}
```

**Cloud Run configuration:**
```bash
gcloud run deploy emailfixer-api \
  --set-secrets="ConnectionStrings__DefaultConnection=db-connection:latest"
```

### SQL Injection Prevention

**Use parameterized queries (EF Core does this automatically):**
```csharp
// SAFE (EF Core)
var users = await context.Users
    .Where(u => u.Email == email)
    .ToListAsync();

// UNSAFE (raw SQL - never do this)
var users = await context.Users
    .FromSqlRaw($"SELECT * FROM Users WHERE Email = '{email}'")
    .ToListAsync();

// SAFE (raw SQL with parameters)
var users = await context.Users
    .FromSqlRaw("SELECT * FROM Users WHERE Email = {0}", email)
    .ToListAsync();
```

### Least Privilege Database Access

**Create application-specific database user:**
```sql
-- Create user
CREATE USER appuser WITH PASSWORD 'strong-password';

-- Grant minimum permissions
GRANT CONNECT ON DATABASE emailfixer TO appuser;
GRANT USAGE ON SCHEMA public TO appuser;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO appuser;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO appuser;

-- Do NOT grant
-- GRANT ALL PRIVILEGES ON DATABASE emailfixer TO appuser;  ❌
```

### Encrypt Sensitive Data

**Hash passwords (if storing locally):**
```csharp
using System.Security.Cryptography;

public string HashPassword(string password)
{
    using var sha256 = SHA256.Create();
    var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
    return Convert.ToBase64String(hashedBytes);
}

// Better: Use ASP.NET Core Identity
// Or bcrypt/Argon2
```

**Encrypt PII (Personally Identifiable Information):**
```csharp
public string EncryptEmail(string email, string key)
{
    using var aes = Aes.Create();
    aes.Key = Encoding.UTF8.GetBytes(key);
    aes.GenerateIV();

    using var encryptor = aes.CreateEncryptor();
    var encrypted = encryptor.TransformFinalBlock(
        Encoding.UTF8.GetBytes(email), 0, email.Length);

    return Convert.ToBase64String(encrypted);
}
```

### Backup and Recovery

**Automated backups:**
```bash
# Cloud SQL automated backups
gcloud sql backups create --instance=emailfixer-postgres

# Point-in-time recovery
gcloud sql backups restore <BACKUP_ID> \
  --backup-instance=emailfixer-postgres \
  --instance=emailfixer-postgres-restored
```

---

## Cloud Deployment Security

### Service Account Security

**Use dedicated service accounts:**
```bash
# Create service account
gcloud iam service-accounts create emailfixer-api \
  --display-name="EmailFixer API Service Account"

# Grant minimal permissions
gcloud projects add-iam-policy-binding emailfixer-prod \
  --member="serviceAccount:emailfixer-api@emailfixer-prod.iam.gserviceaccount.com" \
  --role="roles/cloudsql.client"

gcloud projects add-iam-policy-binding emailfixer-prod \
  --member="serviceAccount:emailfixer-api@emailfixer-prod.iam.gserviceaccount.com" \
  --role="roles/secretmanager.secretAccessor"
```

**NEVER:**
- Use default service account
- Grant owner or editor roles
- Download service account keys (use Workload Identity instead)

### Network Security

**Cloud Run security:**
```bash
# Deploy with security settings
gcloud run deploy emailfixer-api \
  --ingress=all \                    # or 'internal-and-cloud-load-balancing'
  --vpc-connector=emailfixer-vpc \   # Use VPC connector
  --vpc-egress=private-ranges-only   # Route only private traffic through VPC
```

**Firewall rules (Cloud SQL):**
```bash
# Allow only Cloud Run to connect
gcloud sql instances patch emailfixer-postgres \
  --no-assign-ip \
  --network=projects/emailfixer-prod/global/networks/default
```

### Container Security

**Scan Docker images:**
```bash
# Enable Container Scanning
gcloud services enable containerscanning.googleapis.com

# Scan image
gcloud container images scan gcr.io/emailfixer-prod/emailfixer-api:latest

# View vulnerabilities
gcloud container images list-vulnerabilities gcr.io/emailfixer-prod/emailfixer-api:latest
```

**Use minimal base images:**
```dockerfile
# Use ASP.NET Core runtime (not SDK)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Run as non-root user
RUN adduser --disabled-password --gecos '' appuser
USER appuser
```

### Update Dependencies

**Automate dependency updates:**
```yaml
# .github/dependabot.yml
version: 2
updates:
  - package-ecosystem: "nuget"
    directory: "/"
    schedule:
      interval: "weekly"
    open-pull-requests-limit: 5
```

---

## Code Security

### Secure Coding Practices

**Never log sensitive data:**
```csharp
// BAD
logger.LogInformation($"User logged in: {email} with password {password}");

// GOOD
logger.LogInformation($"User logged in: {MaskEmail(email)}");
```

**Avoid hardcoded secrets:**
```csharp
// BAD
var apiKey = "sk_live_1234567890abcdef";

// GOOD
var apiKey = configuration["Paddle:ApiKey"];
```

**Validate file uploads:**
```csharp
public IActionResult Upload(IFormFile file)
{
    // Check file size
    if (file.Length > 10_000_000)  // 10 MB
        return BadRequest("File too large");

    // Check file type
    var allowedExtensions = new[] { ".csv", ".txt" };
    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (!allowedExtensions.Contains(extension))
        return BadRequest("Invalid file type");

    // Scan for malware (if possible)
    // ...
}
```

### Dependency Security

**Check for vulnerabilities:**
```bash
# .NET
dotnet list package --vulnerable --include-transitive

# Update vulnerable packages
dotnet add package <PackageName> --version <LatestVersion>
```

**Use NuGet Audit:**
```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <NuGetAudit>true</NuGetAudit>
    <NuGetAuditMode>all</NuGetAuditMode>
    <NuGetAuditLevel>low</NuGetAuditLevel>
  </PropertyGroup>
</Project>
```

### Static Analysis

**Use code analyzers:**
```xml
<!-- EmailFixer.Api.csproj -->
<ItemGroup>
  <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="8.0.0">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
  <PackageReference Include="SecurityCodeScan.VS2019" Version="5.6.7">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

---

## Monitoring and Auditing

### Logging

**What to log:**
- Authentication attempts (success/failure)
- Authorization failures
- Input validation errors
- API errors
- Security events

**What NOT to log:**
- Passwords
- JWT tokens
- API keys
- Credit card numbers
- SSN or other PII

**Example:**
```csharp
// GOOD
logger.LogWarning("Failed login attempt for user: {Email}", MaskEmail(email));

// BAD
logger.LogWarning("Failed login: {Email} with password {Password}", email, password);
```

### Alerting

**Set up alerts for:**
- Failed authentication attempts (>10 per minute)
- 500 errors (>5% error rate)
- Unusual traffic patterns
- Secret access patterns

**Google Cloud Monitoring:**
```bash
# Create alert policy
gcloud alpha monitoring policies create \
  --notification-channels=<CHANNEL_ID> \
  --display-name="High error rate" \
  --condition-display-name="Error rate > 5%" \
  --condition-threshold-value=0.05 \
  --condition-threshold-duration=60s
```

### Audit Logs

**Enable Cloud Audit Logs:**
```bash
# View admin activity
gcloud logging read "logName:cloudaudit.googleapis.com" --limit 50

# View data access (if enabled)
gcloud logging read "logName:cloudaudit.googleapis.com/data_access" --limit 50
```

**Review regularly:**
- Who accessed secrets?
- Who modified IAM policies?
- Who deployed new versions?
- What database changes were made?

---

## Incident Response

### Security Incident Plan

**If credentials compromised:**

1. **Immediate actions:**
   - Rotate affected credentials
   - Revoke access tokens
   - Lock affected accounts
   - Review access logs

2. **Investigation:**
   - Identify scope of breach
   - Check for unauthorized access
   - Review audit logs
   - Determine root cause

3. **Remediation:**
   - Fix vulnerability
   - Deploy updated code
   - Notify affected users (if applicable)
   - Document incident

4. **Prevention:**
   - Update security policies
   - Implement additional monitoring
   - Train team members
   - Review and test response plan

### Example: Google OAuth Secret Leaked

**Step-by-step response:**

1. **Rotate credentials immediately:**
   ```bash
   # Google Cloud Console
   # 1. Go to APIs & Services > Credentials
   # 2. Delete compromised OAuth client
   # 3. Create new OAuth client
   # 4. Update secrets

   # Update Secret Manager
   echo -n "new-client-id" | gcloud secrets versions add google-oauth-client-id --data-file=-
   echo -n "new-client-secret" | gcloud secrets versions add google-oauth-client-secret --data-file=-
   ```

2. **Revoke all user sessions:**
   ```bash
   # Redeploy with new JWT secret (invalidates all tokens)
   echo -n "new-jwt-secret" | gcloud secrets versions add jwt-secret --data-file=-
   gcloud run deploy emailfixer-api --update-secrets=Jwt__Secret=jwt-secret:latest
   ```

3. **Check for unauthorized access:**
   ```bash
   # Review authentication logs
   gcloud logging read "resource.type=cloud_run_revision AND jsonPayload.message=~\"authentication\"" --limit 1000 > auth_logs.json
   ```

4. **Document incident:**
   ```markdown
   # Incident Report: OAuth Credentials Compromised

   Date: 2025-11-09
   Detected: 14:30 UTC
   Resolved: 15:45 UTC

   ## Summary
   Google OAuth Client Secret was accidentally committed to public GitHub repository.

   ## Timeline
   - 14:30: Secret detected in git history by automated scan
   - 14:35: Repository made private
   - 14:40: New OAuth credentials created
   - 14:45: Secrets rotated in Secret Manager
   - 14:50: Application redeployed
   - 15:00: All user sessions invalidated
   - 15:15: Audit logs reviewed (no unauthorized access detected)
   - 15:45: Incident resolved

   ## Root Cause
   Developer committed appsettings.json with real credentials instead of placeholders.

   ## Prevention
   - Add git pre-commit hook to scan for secrets
   - Update .gitignore patterns
   - Train team on secret management
   - Enable GitHub secret scanning
   ```

---

## Compliance and Privacy

### GDPR Compliance

**User data rights:**
- Right to access (export user data)
- Right to deletion (delete user data)
- Right to portability (data export)
- Right to rectification (update user data)

**Implementation:**
```csharp
// Export user data
[HttpGet("api/user/export")]
[Authorize]
public async Task<IActionResult> ExportUserData()
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var userData = await userService.ExportAllUserData(userId);
    return File(userData, "application/json", "user_data.json");
}

// Delete user data
[HttpDelete("api/user/delete-account")]
[Authorize]
public async Task<IActionResult> DeleteAccount()
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    await userService.DeleteUserAndAllData(userId);
    return NoContent();
}
```

### Data Retention

**Policy:**
- Active user data: Retained while account active
- Deleted user data: Purged within 30 days
- Audit logs: Retained for 1 year
- Backups: Retained for 30 days

### Privacy Policy

**Must include:**
- What data is collected
- How data is used
- How data is protected
- User rights (GDPR, CCPA)
- Contact information

**Link:** `https://emailfixer.com/privacy`

---

## Security Checklist

### Development

- [ ] User secrets configured locally
- [ ] .gitignore includes secret files
- [ ] No hardcoded credentials in code
- [ ] Input validation on all endpoints
- [ ] Output sanitization implemented
- [ ] Static analysis enabled
- [ ] Dependencies up to date

### Deployment

- [ ] HTTPS enabled
- [ ] Secrets in Secret Manager
- [ ] Service account with minimal permissions
- [ ] CORS configured correctly
- [ ] Rate limiting enabled
- [ ] Security headers configured
- [ ] Container scanning enabled
- [ ] Automated backups configured

### Monitoring

- [ ] Logging configured
- [ ] Alerts set up
- [ ] Audit logs enabled
- [ ] Error tracking implemented
- [ ] Performance monitoring enabled

### Compliance

- [ ] Privacy policy published
- [ ] Data export functionality
- [ ] Data deletion functionality
- [ ] User consent mechanism
- [ ] Data retention policy documented

---

## Additional Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [OWASP API Security Top 10](https://owasp.org/www-project-api-security/)
- [Google Cloud Security Best Practices](https://cloud.google.com/security/best-practices)
- [ASP.NET Core Security](https://learn.microsoft.com/en-us/aspnet/core/security/)
- [OAuth 2.0 Security Best Current Practice](https://datatracker.ietf.org/doc/html/draft-ietf-oauth-security-topics)
- [JWT Best Practices](https://tools.ietf.org/html/rfc8725)

---

**Document Version:** 1.0
**Last Updated:** 2025-11-09
**Review Schedule:** Quarterly
**Next Review:** 2025-02-09

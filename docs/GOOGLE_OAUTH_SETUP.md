# Google OAuth 2.0 Setup Guide for EmailFixer

This guide walks you through setting up Google OAuth 2.0 credentials for the EmailFixer application, including development and production environments.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Google Cloud Console Setup](#google-cloud-console-setup)
3. [OAuth 2.0 Credentials Creation](#oauth-20-credentials-creation)
4. [GitHub Secrets Configuration](#github-secrets-configuration)
5. [Local Development Setup](#local-development-setup)
6. [Production Deployment](#production-deployment)
7. [Testing Instructions](#testing-instructions)
8. [Troubleshooting](#troubleshooting)
9. [Security Best Practices](#security-best-practices)

---

## Prerequisites

Before you begin, ensure you have:

- Google Cloud account (free tier is sufficient)
- Access to GitHub repository settings (admin or write permissions)
- Project deployed or ready to deploy to Cloud Run
- Domain name configured (for production)

---

## Google Cloud Console Setup

### Step 1: Create or Select Google Cloud Project

1. Navigate to [Google Cloud Console](https://console.cloud.google.com/)
2. Click the project dropdown at the top of the page
3. Either:
   - Select existing project: **"EmailFixer"** or **"emailfixer-prod"**
   - Create new project:
     - Click "New Project"
     - Project Name: `EmailFixer`
     - Project ID: `emailfixer-prod` (or auto-generated)
     - Location: No organization
     - Click "Create"

### Step 2: Enable Required APIs

1. In the Google Cloud Console, go to **APIs & Services > Library**
2. Search for and enable the following APIs:
   - **Google+ API** (for user profile information)
   - **People API** (optional, for extended profile data)

3. Alternative: Enable via gcloud CLI:
   ```bash
   gcloud config set project emailfixer-prod
   gcloud services enable plus.googleapis.com
   gcloud services enable people.googleapis.com
   ```

### Step 3: Configure OAuth Consent Screen

1. Go to **APIs & Services > OAuth consent screen**
2. Select **User Type**:
   - **External**: For public application (recommended)
   - **Internal**: Only if you have a Google Workspace organization
3. Click **Create**

4. Fill out the **OAuth consent screen** form:

   **App Information:**
   - App name: `EmailFixer`
   - User support email: `your-email@example.com`
   - App logo: (optional, upload your logo)

   **App domain:**
   - Application home page: `https://emailfixer.com` (or your domain)
   - Application privacy policy link: `https://emailfixer.com/privacy`
   - Application terms of service link: `https://emailfixer.com/terms`

   **Authorized domains:**
   - Add: `emailfixer.com` (your production domain)
   - Add: `localhost` (for local development)

   **Developer contact information:**
   - Email addresses: `your-email@example.com`

5. Click **Save and Continue**

6. **Scopes** (select the following):
   - `./auth/userinfo.email` - See your primary Google Account email address
   - `./auth/userinfo.profile` - See your personal info, including any personal info you've made publicly available
   - `openid` - OpenID Connect protocol

   Click **Add or Remove Scopes**, select the above, then **Update**

7. **Test users** (for development):
   - Add your email addresses for testing
   - Click **Add Users**
   - Click **Save and Continue**

8. Review summary and click **Back to Dashboard**

---

## OAuth 2.0 Credentials Creation

### Step 1: Create OAuth 2.0 Client ID

1. Go to **APIs & Services > Credentials**
2. Click **+ Create Credentials** > **OAuth client ID**
3. Select **Application type**: **Web application**

4. Configure the OAuth client:

   **Name:**
   - Client name: `EmailFixer OAuth Client`

   **Authorized JavaScript origins:**
   - Development: `http://localhost:5000`
   - Production: `https://emailfixer.com` (replace with your domain)
   - Cloud Run (if direct): `https://emailfixer-client-<hash>-uc.a.run.app`

   **Authorized redirect URIs:**
   - Development: `http://localhost:5000/auth-callback`
   - Production: `https://emailfixer.com/auth-callback`
   - Cloud Run (if direct): `https://emailfixer-client-<hash>-uc.a.run.app/auth-callback`

   **Note:** Replace `emailfixer.com` with your actual domain and `<hash>` with your Cloud Run service URL

5. Click **Create**

### Step 2: Download and Secure Credentials

1. A modal will appear showing your **Client ID** and **Client Secret**

   Example:
   ```
   Client ID: 123456789012-abcdefghijklmnopqrstuvwxyz123456.apps.googleusercontent.com
   Client Secret: GOCSPX-abcdefghijklmnopqrstuvwxyz
   ```

2. **CRITICAL: Save these credentials securely**
   - Copy **Client ID** to a secure location (password manager, encrypted file)
   - Copy **Client Secret** to a secure location
   - **DO NOT** commit these to git
   - **DO NOT** share publicly
   - **DO NOT** hardcode in source files

3. Click **Download JSON** (optional, for backup)
   - Rename to `google-oauth-credentials.json`
   - Store in a secure location **outside the repository**
   - Add to `.gitignore`: `google-oauth-credentials.json`

4. Click **OK** to close the modal

---

## GitHub Secrets Configuration

GitHub Secrets allow secure storage of credentials for CI/CD workflows.

### Step 1: Add Secrets to Repository

1. Navigate to your GitHub repository
2. Go to **Settings** > **Secrets and variables** > **Actions**
3. Click **New repository secret**

### Step 2: Create Required Secrets

Add the following secrets one by one:

#### Secret 1: GOOGLE_OAUTH_CLIENT_ID

- **Name:** `GOOGLE_OAUTH_CLIENT_ID`
- **Value:** Your Client ID from Google Cloud Console
  ```
  123456789012-abcdefghijklmnopqrstuvwxyz123456.apps.googleusercontent.com
  ```
- Click **Add secret**

#### Secret 2: GOOGLE_OAUTH_CLIENT_SECRET

- **Name:** `GOOGLE_OAUTH_CLIENT_SECRET`
- **Value:** Your Client Secret from Google Cloud Console
  ```
  GOCSPX-abcdefghijklmnopqrstuvwxyz
  ```
- Click **Add secret**

#### Secret 3: JWT_SECRET

- **Name:** `JWT_SECRET`
- **Value:** Generate a secure random string (32+ characters)

  **Generate using one of these methods:**

  **Option A: OpenSSL (Linux/Mac):**
  ```bash
  openssl rand -base64 32
  ```

  **Option B: PowerShell (Windows):**
  ```powershell
  -join ((48..57) + (65..90) + (97..122) | Get-Random -Count 32 | % {[char]$_})
  ```

  **Option C: Node.js:**
  ```bash
  node -e "console.log(require('crypto').randomBytes(32).toString('base64'))"
  ```

  **Option D: Python:**
  ```bash
  python -c "import secrets; print(secrets.token_urlsafe(32))"
  ```

  Example result:
  ```
  8KJH3k4j5h6g7f8d9s0a1q2w3e4r5t6y7u8i9o0p1a2s3d4f5g6h7j8k9l0
  ```

- Click **Add secret**

### Step 3: Verify Secrets

1. Go back to **Settings** > **Secrets and variables** > **Actions**
2. Verify all three secrets are listed:
   - `GOOGLE_OAUTH_CLIENT_ID`
   - `GOOGLE_OAUTH_CLIENT_SECRET`
   - `JWT_SECRET`

**Note:** Secret values are hidden after creation. You cannot view them again, only update them.

---

## Local Development Setup

### Step 1: Create Local User Secrets

For local development, use .NET User Secrets (not committed to git):

```bash
cd EmailFixer.Api

# Initialize user secrets
dotnet user-secrets init

# Set Google OAuth credentials
dotnet user-secrets set "GoogleOAuth:ClientId" "your-client-id-here"
dotnet user-secrets set "GoogleOAuth:ClientSecret" "your-client-secret-here"

# Set JWT secret
dotnet user-secrets set "Jwt:Secret" "your-generated-jwt-secret-here"
```

### Step 2: Update appsettings.Development.json (Optional)

The default `appsettings.Development.json` contains placeholder values. User secrets will override these:

```json
{
  "GoogleOAuth": {
    "ClientId": "your-google-client-id-here",
    "ClientSecret": "your-google-client-secret-here",
    "RedirectUri": "http://localhost:5000/auth-callback"
  },
  "Jwt": {
    "Secret": "your-secret-key-must-be-at-least-32-characters",
    "Issuer": "emailfixer-api",
    "Audience": "emailfixer-client",
    "ExpirationMinutes": 60
  }
}
```

### Step 3: Verify Local Configuration

```bash
# Run the API
dotnet run --project EmailFixer.Api

# Expected output:
# info: Microsoft.Hosting.Lifetime[14]
#       Now listening on: http://localhost:5000
#       Application started. Press Ctrl+C to shut down.
```

Test the OAuth endpoint:
```bash
curl http://localhost:5000/api/auth/google-login
# Should return a Google OAuth URL
```

---

## Production Deployment

### Step 1: Add Secrets to Google Secret Manager

For production on Google Cloud Run, use Secret Manager:

```bash
# Set project
gcloud config set project emailfixer-prod

# Enable Secret Manager API
gcloud services enable secretmanager.googleapis.com

# Create secrets
echo -n "your-client-id" | gcloud secrets create google-oauth-client-id --data-file=-
echo -n "your-client-secret" | gcloud secrets create google-oauth-client-secret --data-file=-
echo -n "your-jwt-secret" | gcloud secrets create jwt-secret --data-file=-

# Grant Cloud Run service account access
PROJECT_NUMBER=$(gcloud projects describe emailfixer-prod --format="value(projectNumber)")

gcloud secrets add-iam-policy-binding google-oauth-client-id \
  --member="serviceAccount:${PROJECT_NUMBER}-compute@developer.gserviceaccount.com" \
  --role="roles/secretmanager.secretAccessor"

gcloud secrets add-iam-policy-binding google-oauth-client-secret \
  --member="serviceAccount:${PROJECT_NUMBER}-compute@developer.gserviceaccount.com" \
  --role="roles/secretmanager.secretAccessor"

gcloud secrets add-iam-policy-binding jwt-secret \
  --member="serviceAccount:${PROJECT_NUMBER}-compute@developer.gserviceaccount.com" \
  --role="roles/secretmanager.secretAccessor"
```

### Step 2: Update GitHub Actions Workflow

The GitHub Actions workflow (`.github/workflows/deploy-gcp.yml`) will automatically use secrets during deployment. See the updated workflow in the repository.

### Step 3: Deploy via GitHub Actions

1. Commit your code changes
2. Push to `main` or `master` branch
3. GitHub Actions will automatically:
   - Run tests
   - Build Docker images
   - Deploy to Cloud Run with secrets

Monitor deployment:
```bash
# Watch GitHub Actions
# Go to: https://github.com/your-org/EmailFixer/actions

# Or check Cloud Run
gcloud run services describe emailfixer-api --region us-central1
```

---

## Testing Instructions

### Test 1: Local OAuth Flow

1. Start the application locally:
   ```bash
   dotnet run --project EmailFixer.Api
   ```

2. Navigate to the client:
   ```
   http://localhost:5000
   ```

3. Click **"Sign in with Google"**

4. Expected behavior:
   - Redirects to Google sign-in page
   - Shows consent screen (first time)
   - Redirects back to `/auth-callback`
   - Displays user information and JWT token

5. Check browser developer console for errors

### Test 2: Production OAuth Flow

1. Navigate to production URL:
   ```
   https://emailfixer.com
   ```

2. Click **"Sign in with Google"**

3. Expected behavior:
   - Same as local testing
   - Verify SSL/HTTPS is working
   - Check that redirect URI matches configured value

### Test 3: API Endpoints

Test authentication endpoints:

```bash
# Get Google login URL
curl https://api.emailfixer.com/api/auth/google-login

# Expected response:
# {
#   "loginUrl": "https://accounts.google.com/o/oauth2/v2/auth?..."
# }

# Test callback (requires valid code from Google)
curl -X POST https://api.emailfixer.com/api/auth/google-callback \
  -H "Content-Type: application/json" \
  -d '{"code": "valid-google-code", "redirectUri": "https://emailfixer.com/auth-callback"}'

# Expected response:
# {
#   "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
#   "refreshToken": "...",
#   "expiresIn": 3600
# }
```

### Test 4: JWT Token Validation

```bash
# Use the JWT token from login
TOKEN="your-jwt-token-here"

# Call protected endpoint
curl https://api.emailfixer.com/api/user/profile \
  -H "Authorization: Bearer $TOKEN"

# Expected response:
# {
#   "id": "...",
#   "email": "user@example.com",
#   "name": "User Name"
# }
```

---

## Troubleshooting

### Problem: "Redirect URI mismatch" error

**Symptoms:**
- Google shows error: "Error 400: redirect_uri_mismatch"
- OAuth callback fails

**Solution:**
1. Check Google Cloud Console > Credentials
2. Verify redirect URI exactly matches:
   - Local: `http://localhost:5000/auth-callback`
   - Production: `https://emailfixer.com/auth-callback`
3. Ensure no trailing slashes
4. Check protocol (http vs https)
5. Verify port number (5000 vs 5001)

**Fix:**
```bash
# Update redirect URI in appsettings.json or user secrets
dotnet user-secrets set "GoogleOAuth:RedirectUri" "http://localhost:5000/auth-callback"
```

### Problem: "Access blocked: This app's request is invalid"

**Symptoms:**
- Google shows: "Access blocked: EmailFixer has not completed the Google verification process"

**Solution:**
1. Go to OAuth consent screen in Google Cloud Console
2. Ensure app is in "Testing" mode (not "In production")
3. Add your email to "Test users"
4. If deploying to production, submit app for verification (not required for internal use)

### Problem: Invalid JWT secret error

**Symptoms:**
- API returns 500 Internal Server Error
- Logs show: "IDX10603: The algorithm 'HS256' requires the SecurityKey.KeySize to be greater than '128' bits"

**Solution:**
- JWT secret must be at least 32 characters (256 bits)
- Regenerate secret using methods in [JWT_SECRET section](#secret-3-jwt_secret)
- Update GitHub secrets and local user secrets

**Fix:**
```bash
# Generate new secret
openssl rand -base64 32

# Update local
dotnet user-secrets set "Jwt:Secret" "new-long-secret-here"

# Update GitHub secret
# Go to Settings > Secrets > JWT_SECRET > Update
```

### Problem: Secrets not loading in Cloud Run

**Symptoms:**
- Cloud Run deployment succeeds but app crashes
- Logs show: "GoogleOAuth:ClientId configuration is missing"

**Solution:**
1. Verify secrets exist in Secret Manager:
   ```bash
   gcloud secrets list --project emailfixer-prod
   ```

2. Check IAM permissions:
   ```bash
   gcloud secrets get-iam-policy google-oauth-client-id
   ```

3. Ensure Cloud Run service is configured with secrets:
   ```bash
   gcloud run services describe emailfixer-api --region us-central1 --format="yaml(spec.template.spec.containers[0].env)"
   ```

4. Update deployment to reference secrets (see GitHub Actions workflow)

### Problem: CORS errors in browser

**Symptoms:**
- Browser console shows: "Access to fetch at 'https://api.emailfixer.com' has been blocked by CORS policy"

**Solution:**
1. Verify API CORS configuration in `appsettings.Production.json`
2. Ensure client origin is in `AllowedOrigins`
3. Check Cloud Run allows CORS headers

**Fix in appsettings.Production.json:**
```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://emailfixer.com",
      "https://www.emailfixer.com"
    ]
  }
}
```

### Problem: Token expires too quickly

**Symptoms:**
- Users logged out after short time
- Need to re-authenticate frequently

**Solution:**
- Adjust JWT expiration in `appsettings.json`:
  ```json
  {
    "Jwt": {
      "ExpirationMinutes": 1440,  // 24 hours
      "RefreshExpirationDays": 30
    }
  }
  ```

- Implement refresh token logic in client

---

## Security Best Practices

### 1. Secrets Management

**DO:**
- Store credentials in Secret Manager (production)
- Use User Secrets for local development
- Use GitHub Secrets for CI/CD
- Rotate secrets regularly (every 90 days)
- Use strong, randomly generated secrets (32+ characters)

**DON'T:**
- Commit secrets to git
- Hardcode credentials in source files
- Share secrets in public repositories
- Email or message secrets
- Reuse secrets across environments

### 2. OAuth Configuration

**DO:**
- Use HTTPS for all redirect URIs in production
- Validate state parameter to prevent CSRF
- Implement proper error handling
- Use short-lived JWT tokens (60 minutes)
- Implement refresh token rotation
- Limit OAuth scopes to minimum required

**DON'T:**
- Use HTTP in production redirect URIs
- Skip state validation
- Store tokens in localStorage (use httpOnly cookies)
- Use long-lived tokens without refresh mechanism
- Request unnecessary OAuth scopes

### 3. Production Deployment

**DO:**
- Enable HTTPS/TLS (Cloud Run does this by default)
- Use environment-specific configurations
- Enable rate limiting
- Monitor authentication failures
- Log authentication events (without sensitive data)
- Use secure headers (HSTS, CSP, etc.)

**DON'T:**
- Expose development credentials in production
- Disable SSL/TLS validation
- Log sensitive data (tokens, secrets)
- Allow unlimited authentication attempts
- Use default or weak JWT secrets

### 4. Access Control

**DO:**
- Implement principle of least privilege
- Use service accounts with minimal permissions
- Review and audit access regularly
- Enable multi-factor authentication for admin accounts
- Use separate credentials for dev/staging/production

**DON'T:**
- Grant broad IAM permissions
- Share service account keys
- Use owner/admin roles unnecessarily
- Skip access reviews

### 5. Monitoring and Auditing

**DO:**
- Monitor failed authentication attempts
- Set up alerts for unusual activity
- Audit secret access logs
- Track token usage patterns
- Implement automatic secret rotation

**DON'T:**
- Ignore authentication errors
- Skip audit logging
- Keep secrets indefinitely without rotation

### 6. Code Security

**DO:**
- Validate all OAuth callback parameters
- Sanitize user input
- Use parameterized queries (already implemented with EF Core)
- Keep dependencies up to date
- Run security scans regularly

**DON'T:**
- Trust user input without validation
- Use string concatenation for queries
- Ignore security warnings from NuGet
- Skip dependency updates

---

## Quick Reference

### Credentials Checklist

- [ ] Google Cloud project created
- [ ] OAuth consent screen configured
- [ ] OAuth 2.0 Client ID created
- [ ] Client ID and Client Secret saved securely
- [ ] GitHub secrets configured (3 secrets)
- [ ] Google Secret Manager secrets created (production)
- [ ] Local user secrets configured (development)
- [ ] Redirect URIs match exactly
- [ ] OAuth tested locally
- [ ] OAuth tested in production

### Required GitHub Secrets

| Secret Name | Description | Example |
|-------------|-------------|---------|
| `GOOGLE_OAUTH_CLIENT_ID` | Google OAuth Client ID | `123456789012-abc...xyz.apps.googleusercontent.com` |
| `GOOGLE_OAUTH_CLIENT_SECRET` | Google OAuth Client Secret | `GOCSPX-abcdefg...xyz` |
| `JWT_SECRET` | JWT signing secret (32+ chars) | `8KJH3k4j5h6g7f8d9s0a...` |

### Required Google Secret Manager Secrets

| Secret Name | Description | Used By |
|-------------|-------------|---------|
| `google-oauth-client-id` | Google OAuth Client ID | Cloud Run (API) |
| `google-oauth-client-secret` | Google OAuth Client Secret | Cloud Run (API) |
| `jwt-secret` | JWT signing secret | Cloud Run (API) |

### Useful Commands

```bash
# List Google Cloud projects
gcloud projects list

# View Secret Manager secrets
gcloud secrets list --project emailfixer-prod

# View secret value (local only!)
gcloud secrets versions access latest --secret="google-oauth-client-id"

# Update GitHub secret (via GitHub CLI)
gh secret set GOOGLE_OAUTH_CLIENT_ID --body "new-value"

# View .NET user secrets
dotnet user-secrets list --project EmailFixer.Api

# Test OAuth endpoint
curl http://localhost:5000/api/auth/google-login
```

---

## Additional Resources

- [Google OAuth 2.0 Documentation](https://developers.google.com/identity/protocols/oauth2)
- [Google Cloud Console](https://console.cloud.google.com/)
- [GitHub Secrets Documentation](https://docs.github.com/en/actions/security-guides/encrypted-secrets)
- [Google Secret Manager](https://cloud.google.com/secret-manager/docs)
- [ASP.NET Core User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [JWT Best Practices](https://tools.ietf.org/html/rfc8725)

---

## Support

If you encounter issues not covered in this guide:

1. Check application logs:
   ```bash
   # Local
   dotnet run --project EmailFixer.Api

   # Cloud Run
   gcloud logging read "resource.type=cloud_run_revision AND resource.labels.service_name=emailfixer-api" --limit 50 --format json
   ```

2. Verify configuration:
   ```bash
   # List user secrets
   dotnet user-secrets list --project EmailFixer.Api

   # Check Secret Manager
   gcloud secrets list
   ```

3. Review Google Cloud Console:
   - APIs & Services > Credentials
   - OAuth consent screen
   - API usage metrics

4. Test with curl or Postman before debugging client code

---

**Document Version:** 1.0
**Last Updated:** 2025-11-09
**Author:** EmailFixer Development Team
**Status:** Production Ready

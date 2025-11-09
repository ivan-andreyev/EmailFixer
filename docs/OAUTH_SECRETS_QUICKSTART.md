# Google OAuth Secrets - Quick Start Guide

This is a condensed guide for quickly setting up Google OAuth credentials for EmailFixer. For detailed instructions, see [GOOGLE_OAUTH_SETUP.md](GOOGLE_OAUTH_SETUP.md).

## Prerequisites

- Google Cloud account
- Access to GitHub repository settings (admin/write)
- 15-20 minutes

---

## 1. Google Cloud Console (10 minutes)

### Create OAuth Credentials

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Select/Create project: `emailfixer-prod`
3. Navigate to: **APIs & Services > Credentials**
4. Click: **+ Create Credentials > OAuth client ID**
5. Configure:
   - **Application type:** Web application
   - **Name:** EmailFixer OAuth Client
   - **Authorized JavaScript origins:**
     - `http://localhost:5000`
     - `https://emailfixer.com`
   - **Authorized redirect URIs:**
     - `http://localhost:5000/auth-callback`
     - `https://emailfixer.com/auth-callback`
6. Click: **Create**
7. **SAVE CREDENTIALS:**
   - Client ID: `123456789012-abc...xyz.apps.googleusercontent.com`
   - Client Secret: `GOCSPX-abc...xyz`

### Configure OAuth Consent Screen (if not done)

1. Navigate to: **APIs & Services > OAuth consent screen**
2. Select: **External**
3. Fill in:
   - App name: `EmailFixer`
   - User support email: your-email@example.com
   - Authorized domains: `emailfixer.com`
4. Add scopes:
   - `openid`
   - `email`
   - `profile`
5. Add test users (your email)
6. Save

---

## 2. GitHub Secrets (5 minutes)

### Add Repository Secrets

1. Go to: **GitHub > Repository > Settings > Secrets and variables > Actions**
2. Click: **New repository secret** (3 times for 3 secrets)

#### Secret 1: GOOGLE_OAUTH_CLIENT_ID
```
Name: GOOGLE_OAUTH_CLIENT_ID
Value: <paste Client ID from Google Cloud Console>
```

#### Secret 2: GOOGLE_OAUTH_CLIENT_SECRET
```
Name: GOOGLE_OAUTH_CLIENT_SECRET
Value: <paste Client Secret from Google Cloud Console>
```

#### Secret 3: JWT_SECRET

Generate random secret:
```bash
# Option 1: OpenSSL (Linux/Mac)
openssl rand -base64 32

# Option 2: PowerShell (Windows)
-join ((48..57) + (65..90) + (97..122) | Get-Random -Count 32 | % {[char]$_})

# Option 3: Online
https://www.random.org/strings/?num=1&len=32&digits=on&upperalpha=on&loweralpha=on&unique=on&format=html&rnd=new
```

Then:
```
Name: JWT_SECRET
Value: <paste generated random string>
```

---

## 3. Google Secret Manager (Production) (5 minutes)

### Create Secrets in Secret Manager

```bash
# Set project
gcloud config set project emailfixer-prod

# Enable Secret Manager API (if not enabled)
gcloud services enable secretmanager.googleapis.com

# Create secrets (replace with your actual values)
echo -n "YOUR_CLIENT_ID_HERE" | gcloud secrets create google-oauth-client-id --data-file=-
echo -n "YOUR_CLIENT_SECRET_HERE" | gcloud secrets create google-oauth-client-secret --data-file=-
echo -n "YOUR_JWT_SECRET_HERE" | gcloud secrets create jwt-secret --data-file=-

# Grant Cloud Run access
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

### Verify Secrets Created

```bash
gcloud secrets list
```

Expected output:
```
NAME                        CREATED              REPLICATION_POLICY  LOCATIONS
google-oauth-client-id      2025-11-09T10:00:00  automatic           -
google-oauth-client-secret  2025-11-09T10:00:01  automatic           -
jwt-secret                  2025-11-09T10:00:02  automatic           -
```

---

## 4. Local Development Setup (2 minutes)

### Configure .NET User Secrets

```bash
cd EmailFixer.Api

# Initialize user secrets
dotnet user-secrets init

# Set OAuth credentials
dotnet user-secrets set "GoogleOAuth:ClientId" "YOUR_CLIENT_ID_HERE"
dotnet user-secrets set "GoogleOAuth:ClientSecret" "YOUR_CLIENT_SECRET_HERE"
dotnet user-secrets set "GoogleOAuth:RedirectUri" "http://localhost:5000/auth-callback"

# Set JWT secret
dotnet user-secrets set "Jwt:Secret" "YOUR_JWT_SECRET_HERE"
dotnet user-secrets set "Jwt:Issuer" "emailfixer-api"
dotnet user-secrets set "Jwt:Audience" "emailfixer-client"
dotnet user-secrets set "Jwt:ExpirationMinutes" "60"
```

### Verify User Secrets

```bash
dotnet user-secrets list
```

Expected output:
```
GoogleOAuth:ClientId = 123456789012-abc...
GoogleOAuth:ClientSecret = GOCSPX-abc...
GoogleOAuth:RedirectUri = http://localhost:5000/auth-callback
Jwt:Secret = your-generated-secret
Jwt:Issuer = emailfixer-api
Jwt:Audience = emailfixer-client
Jwt:ExpirationMinutes = 60
```

---

## 5. Testing (5 minutes)

### Test Local Development

```bash
# Run API
dotnet run --project EmailFixer.Api

# Expected output:
# info: Now listening on: http://localhost:5000
```

In another terminal:
```bash
# Test OAuth endpoint
curl http://localhost:5000/api/auth/google-login

# Expected: JSON with Google OAuth URL
```

### Test Production Deployment

1. Push code to GitHub (triggers GitHub Actions)
2. Monitor deployment: https://github.com/your-org/EmailFixer/actions
3. Test deployed API:
   ```bash
   curl https://api.emailfixer.com/api/auth/google-login
   ```

---

## Troubleshooting

### "Redirect URI mismatch"
- Verify redirect URI in Google Cloud Console matches exactly
- Check for trailing slashes, http vs https, port numbers

### "Invalid JWT secret"
- JWT secret must be at least 32 characters
- Regenerate using the commands in Step 2 (Secret 3)

### "Secrets not found in Secret Manager"
- Run: `gcloud secrets list` to verify
- Check project: `gcloud config get-value project`
- Ensure IAM permissions granted (Step 3)

### "User secrets not loading"
- Verify: `dotnet user-secrets list`
- Check project path: `cd EmailFixer.Api`
- Re-initialize: `dotnet user-secrets clear && dotnet user-secrets init`

---

## Security Reminders

**NEVER commit these to git:**
- Client ID and Client Secret
- JWT Secret
- User secrets files
- Environment variable files with real values

**ALWAYS use:**
- .NET User Secrets for local development
- Google Secret Manager for production
- GitHub Secrets for CI/CD
- Placeholders in appsettings.json (committed to git)

**Rotate secrets:**
- Every 90 days (scheduled)
- Immediately after suspected compromise
- After employee departure
- After major security updates

---

## Quick Reference

### Files Modified
- `.github/workflows/deploy-gcp.yml` - Added OAuth secrets to Cloud Run deployment
- `EmailFixer.Api/appsettings.Production.json` - Added OAuth/JWT configuration with placeholders
- `docs/GOOGLE_OAUTH_SETUP.md` - Comprehensive setup guide (this file)
- `docs/SECURITY_BEST_PRACTICES.md` - Security guidelines

### Secrets Required

| Secret | Location | Purpose |
|--------|----------|---------|
| GOOGLE_OAUTH_CLIENT_ID | GitHub Secrets + Secret Manager | OAuth authentication |
| GOOGLE_OAUTH_CLIENT_SECRET | GitHub Secrets + Secret Manager | OAuth authentication |
| JWT_SECRET | GitHub Secrets + Secret Manager | JWT token signing |

### Commands Cheat Sheet

```bash
# Google Cloud
gcloud secrets list
gcloud secrets versions access latest --secret="google-oauth-client-id"

# .NET User Secrets
dotnet user-secrets list --project EmailFixer.Api
dotnet user-secrets set "Key:SubKey" "value"

# GitHub CLI
gh secret list
gh secret set GOOGLE_OAUTH_CLIENT_ID --body "value"

# Testing
curl http://localhost:5000/api/auth/google-login
curl https://api.emailfixer.com/health
```

---

## Next Steps

After completing this setup:

1. Test OAuth flow locally
2. Deploy to production via GitHub Actions
3. Test OAuth flow in production
4. Update production redirect URI if using custom domain
5. Monitor authentication logs
6. Set up secret rotation schedule (90 days)

---

## Support

For detailed instructions, see: [GOOGLE_OAUTH_SETUP.md](GOOGLE_OAUTH_SETUP.md)
For security guidelines, see: [SECURITY_BEST_PRACTICES.md](SECURITY_BEST_PRACTICES.md)

---

**Quick Start Version:** 1.0
**Last Updated:** 2025-11-09

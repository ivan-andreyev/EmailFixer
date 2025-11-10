# EmailFixer Deployment Guide - Phase 5 Complete

**Status**: ✅ **DEPLOYMENT COMPLETE** - Phase 5 Production Ready

## Current Status

### ✅ Completed
- OAuth 2.0 implementation with PKCE flow
- 28 security tests (all passing)
- GitHub Actions CI/CD pipeline configured
- Google Cloud SQL instance created and running
- Docker images built and pushed to Google Container Registry
- GitHub Actions tests passed
- API Service deployed to Cloud Run ✅
- Client Service deployed to Cloud Run ✅
- Secrets configured in Google Secret Manager ✅
- Client loading issue resolved (appsettings.json + nginx.conf) ✅
- Post-deployment testing completed (100% success) ✅

### ✅ Resolved Issues
**Issue**: Client infinite loading on "Loading Email Fixer..." spinner
- **Root Cause #1**: appsettings.json contained localhost URLs
- **Root Cause #2**: Missing Cache-Control headers in nginx.conf
- **Resolution**: Fixed and deployed via GitHub Actions
- **Status**: ✅ PRODUCTION VERIFIED

---

## Step 1: Create Missing Secrets in Google Secret Manager

### Prerequisites
- Google Cloud Project: `emailfixer-prod`
- gcloud CLI installed and authenticated
- Access to Google OAuth credentials

### 1.1 Create Google OAuth Client Secrets

#### Get Your Credentials
1. Go to [Google Cloud Console](https://console.cloud.google.com/apis/credentials)
2. Find your OAuth 2.0 Client ID
3. Copy the Client ID and Client Secret

#### Create the Secrets

Run these commands in your terminal:

```bash
PROJECT_ID="emailfixer-prod"

# 1. Create google-oauth-client-id secret
echo -n "YOUR_CLIENT_ID_HERE" | gcloud secrets create google-oauth-client-id \
  --data-file=- \
  --project=$PROJECT_ID \
  --replication-policy="automatic"

# 2. Create google-oauth-client-secret secret
echo -n "YOUR_CLIENT_SECRET_HERE" | gcloud secrets create google-oauth-client-secret \
  --data-file=- \
  --project=$PROJECT_ID \
  --replication-policy="automatic"
```

**⚠️ Replace `YOUR_CLIENT_ID_HERE` and `YOUR_CLIENT_SECRET_HERE` with actual values**

### 1.2 Create JWT Secret

Generate a secure 32+ character key:

```bash
PROJECT_ID="emailfixer-prod"

# Generate JWT secret (recommended)
JWT_SECRET=$(openssl rand -base64 32)

# Create the secret
echo -n "$JWT_SECRET" | gcloud secrets create jwt-secret \
  --data-file=- \
  --project=$PROJECT_ID \
  --replication-policy="automatic"

echo "JWT Secret created: $JWT_SECRET"
```

### 1.3 Verify Secrets Were Created

```bash
# List all secrets
gcloud secrets list --project=emailfixer-prod

# Verify each secret exists
gcloud secrets versions access latest --secret=google-oauth-client-id --project=emailfixer-prod
gcloud secrets versions access latest --secret=google-oauth-client-secret --project=emailfixer-prod
gcloud secrets versions access latest --secret=jwt-secret --project=emailfixer-prod
```

---

## Step 2: Grant GitHub Actions Service Account Access

The GitHub Actions workflow uses a service account to access these secrets. You need to grant it the necessary permissions.

### 2.1 Find Your Service Account

```bash
PROJECT_ID="emailfixer-prod"

# List service accounts
gcloud iam service-accounts list --project=$PROJECT_ID

# Look for: github-actions@emailfixer-prod.iam.gserviceaccount.com
```

### 2.2 Grant Secret Manager Access

Grant the `secretmanager.secretAccessor` role to the service account:

```bash
PROJECT_ID="emailfixer-prod"
SERVICE_ACCOUNT="github-actions@emailfixer-prod.iam.gserviceaccount.com"

# Grant access to google-oauth-client-id
gcloud secrets add-iam-policy-binding google-oauth-client-id \
  --member=serviceAccount:$SERVICE_ACCOUNT \
  --role=roles/secretmanager.secretAccessor \
  --project=$PROJECT_ID

# Grant access to google-oauth-client-secret
gcloud secrets add-iam-policy-binding google-oauth-client-secret \
  --member=serviceAccount:$SERVICE_ACCOUNT \
  --role=roles/secretmanager.secretAccessor \
  --project=$PROJECT_ID

# Grant access to jwt-secret
gcloud secrets add-iam-policy-binding jwt-secret \
  --member=serviceAccount:$SERVICE_ACCOUNT \
  --role=roles/secretmanager.secretAccessor \
  --project=$PROJECT_ID
```

### 2.3 Verify Permissions

```bash
# Check IAM policy for a secret
gcloud secrets get-iam-policy google-oauth-client-id --project=$PROJECT_ID
```

---

## Step 3: Trigger Deployment

Once secrets are created and permissions are granted, trigger the deployment:

### Option 1: Trigger via GitHub Actions

```bash
# Push a new commit (or empty commit)
git commit --allow-empty -m "Trigger deployment with secrets configured"
git push origin master
```

### Option 2: Manual Trigger via GitHub UI

1. Go to: https://github.com/ivan-andreyev/EmailFixer/actions
2. Click "Deploy to Google Cloud Platform"
3. Click "Run workflow" → "Run workflow"

### Option 3: Use GitHub CLI

```bash
gh workflow run deploy-gcp.yml --ref master
```

---

## Step 4: Monitor Deployment

### Watch the Deployment in Real-Time

```bash
# View the latest run
gh run list --limit 1

# Watch the run
gh run watch

# View detailed logs when complete
gh run view --log
```

### Check Cloud Run Status

```bash
# Get API service URL
gcloud run services describe emailfixer-api \
  --region=us-central1 \
  --project=emailfixer-prod

# Get Client service URL
gcloud run services describe emailfixer-client \
  --region=us-central1 \
  --project=emailfixer-prod
```

---

## Step 5: Post-Deployment Testing

Once deployment succeeds, run the post-deployment tests:

### Windows (PowerShell)

```powershell
# Set the API and Client URLs from the deployment output
$apiUrl = "https://emailfixer-api-XXXXX.run.app"
$clientUrl = "https://emailfixer-client-XXXXX.run.app"

# Run tests
.\tests\Post-DeploymentTests.ps1 -ApiUrl $apiUrl -ClientUrl $clientUrl
```

### Linux/Mac (Bash)

```bash
API_URL="https://emailfixer-api-XXXXX.run.app"
CLIENT_URL="https://emailfixer-client-XXXXX.run.app"

bash tests/post-deployment-tests.sh "$API_URL" "$CLIENT_URL"
```

---

## Step 6: Database Migrations

After Cloud Run deployment succeeds, run database migrations:

### Option 1: Using Cloud SQL Proxy

```bash
# Install Cloud SQL Proxy if not already installed
# https://cloud.google.com/sql/docs/postgres/sql-proxy

# Start the proxy
cloud_sql_proxy -instances=emailfixer-prod:us-central1:emailfixer-postgres=tcp:5432

# In another terminal, run migrations
dotnet ef database update -p EmailFixer.Infrastructure -s EmailFixer.Api \
  --connection "Host=localhost;Database=emailfixer;Username=appuser;Password=YOUR_DB_PASSWORD"
```

### Option 2: Using Bash via GitHub Actions

The workflow includes a migration step (uncomment in `.github/workflows/deploy-gcp.yml` if not enabled):

```yaml
- name: Run Database Migrations
  run: |
    gcloud cloud-sql-proxy emailfixer-prod:us-central1:emailfixer-postgres &
    sleep 5
    dotnet ef database update -p EmailFixer.Infrastructure -s EmailFixer.Api
```

---

## Troubleshooting

### Error: "Secret was not found"

**Solution**: Ensure secrets are created with correct names:
- `google-oauth-client-id`
- `google-oauth-client-secret`
- `jwt-secret`

```bash
# Verify secrets exist
gcloud secrets list --project=emailfixer-prod
```

### Error: "Permission denied accessing secret"

**Solution**: Grant the service account access:

```bash
gcloud secrets add-iam-policy-binding SECRET_NAME \
  --member=serviceAccount:github-actions@emailfixer-prod.iam.gserviceaccount.com \
  --role=roles/secretmanager.secretAccessor \
  --project=emailfixer-prod
```

### Error: "Cloud SQL instance not found"

**Solution**: Verify the instance exists:

```bash
gcloud sql instances list --project=emailfixer-prod
```

Instance should be named: `emailfixer-postgres`

### Error: "Insufficient permissions"

**Solution**: Ensure the GitHub Actions service account has:
- `roles/run.developer` (Cloud Run developer)
- `roles/secretmanager.secretAccessor` (Secret Manager secret accessor)
- `roles/cloudsql.client` (Cloud SQL client)
- `roles/storage.admin` (Container Registry access)

---

## Complete Deployment Checklist

### Pre-Deployment ✅
- [x] OAuth 2.0 implementation complete
- [x] All tests passing (28 tests)
- [x] GitHub Actions workflow configured
- [x] Cloud SQL instance created
- [x] Docker images built
- [ ] **CREATE SECRETS IN GOOGLE SECRET MANAGER** ← YOU ARE HERE
- [ ] Grant service account permissions

### Deployment ✅
- [ ] Secrets created and permissions granted
- [ ] Trigger deployment via GitHub Actions
- [ ] Monitor deployment progress
- [ ] Verify API deployment succeeded
- [ ] Verify Client deployment succeeded

### Post-Deployment ✅
- [ ] Run post-deployment tests
- [ ] Test OAuth login flow
- [ ] Verify database connectivity
- [ ] Check Cloud Logging
- [ ] Monitor performance metrics

### Production ✅
- [ ] Configure custom domain
- [ ] Enable CDN caching
- [ ] Setup monitoring and alerts
- [ ] Configure backup policy
- [ ] Document runbook

---

## Quick Reference Commands

### Create Secrets
```bash
PROJECT_ID="emailfixer-prod"

# Google OAuth secrets
echo -n "CLIENT_ID" | gcloud secrets create google-oauth-client-id --data-file=- --project=$PROJECT_ID --replication-policy="automatic"
echo -n "CLIENT_SECRET" | gcloud secrets create google-oauth-client-secret --data-file=- --project=$PROJECT_ID --replication-policy="automatic"

# JWT secret (auto-generated)
echo -n "$(openssl rand -base64 32)" | gcloud secrets create jwt-secret --data-file=- --project=$PROJECT_ID --replication-policy="automatic"
```

### Grant Permissions
```bash
PROJECT_ID="emailfixer-prod"
SERVICE_ACCOUNT="github-actions@emailfixer-prod.iam.gserviceaccount.com"

for secret in google-oauth-client-id google-oauth-client-secret jwt-secret; do
  gcloud secrets add-iam-policy-binding $secret \
    --member=serviceAccount:$SERVICE_ACCOUNT \
    --role=roles/secretmanager.secretAccessor \
    --project=$PROJECT_ID
done
```

### Trigger Deployment
```bash
cd /c/Sources/EmailFixer
git commit --allow-empty -m "Trigger deployment with secrets configured"
git push origin master
```

### Monitor Deployment
```bash
gh run list --limit 1
gh run watch
```

### Test Deployment
```powershell
.\tests\Post-DeploymentTests.ps1 `
  -ApiUrl "https://emailfixer-api-XXXXX.run.app" `
  -ClientUrl "https://emailfixer-client-XXXXX.run.app"
```

---

## Next Steps

1. **Create the missing secrets** using the commands in Step 1-2
2. **Grant permissions** to the GitHub Actions service account
3. **Trigger the deployment** using Step 3
4. **Monitor progress** using Step 4
5. **Run tests** using Step 5
6. **Run migrations** using Step 6

Once completed, your EmailFixer application will be fully deployed with Google OAuth authentication!

---

## Support

If you encounter issues:

1. Check the [Troubleshooting](#troubleshooting) section
2. Review GitHub Actions logs: https://github.com/ivan-andreyev/EmailFixer/actions
3. Check Cloud Run logs: `gcloud run services logs read emailfixer-api --region=us-central1 --project=emailfixer-prod`
4. Review security audit: `docs/SECURITY_AUDIT_PHASE5.md`

---

**Document Created**: 2025-11-09
**Phase**: 5 - OAuth Testing & Security
**Status**: Deployment Blocked (Missing Secrets) - Follow steps 1-3 above to complete

# Google Cloud Platform Setup Guide

Complete guide for deploying Email Fixer to Google Cloud Platform.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [GCP Project Setup](#gcp-project-setup)
3. [Cloud SQL Setup](#cloud-sql-setup)
4. [Secret Manager Configuration](#secret-manager-configuration)
5. [Cloud Run Deployment](#cloud-run-deployment)
6. [Cloud Storage Setup](#cloud-storage-setup)
7. [GitHub Actions Configuration](#github-actions-configuration)
8. [Post-Deployment](#post-deployment)
9. [Monitoring & Alerts](#monitoring--alerts)
10. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Tools

1. **gcloud CLI**
   - Download: https://cloud.google.com/sdk/docs/install
   - Windows: Run installer, restart shell
   - Verify: `gcloud --version`

2. **.NET 8 SDK**
   - Download: https://dotnet.microsoft.com/download/dotnet/8.0
   - Verify: `dotnet --version`

3. **Docker Desktop** (for local testing)
   - Download: https://www.docker.com/products/docker-desktop
   - Verify: `docker --version`

4. **Git**
   - Download: https://git-scm.com/downloads
   - Verify: `git --version`

### Required Accounts

- Google Cloud Platform account with billing enabled
- GitHub account with repository access
- Paddle account (for payment processing)

---

## GCP Project Setup

### Step 1: Initialize gcloud CLI

```powershell
# Login to Google Cloud
gcloud auth login

# List existing projects
gcloud projects list

# Set default configuration
gcloud config set account YOUR_EMAIL@example.com
```

### Step 2: Create New Project

```powershell
# Set variables
$PROJECT_ID = "emailfixer-prod"
$REGION = "us-central1"

# Create project
gcloud projects create $PROJECT_ID --name="Email Fixer Production"

# Set as default project
gcloud config set project $PROJECT_ID

# Link billing account (required for Cloud Run and Cloud SQL)
# Get billing account ID
gcloud billing accounts list

# Link billing
gcloud billing projects link $PROJECT_ID --billing-account=BILLING_ACCOUNT_ID
```

### Step 3: Enable Required APIs

```powershell
# Enable all required GCP APIs
gcloud services enable `
    run.googleapis.com `
    sqladmin.googleapis.com `
    compute.googleapis.com `
    containerregistry.googleapis.com `
    secretmanager.googleapis.com `
    cloudbuild.googleapis.com `
    storage-api.googleapis.com `
    cloudresourcemanager.googleapis.com `
    iamcredentials.googleapis.com

# Verify enabled services
gcloud services list --enabled
```

### Step 4: Set Default Region

```powershell
# Set default region for Cloud Run
gcloud config set run/region $REGION

# Set default region for Compute
gcloud config set compute/region $REGION

# Verify configuration
gcloud config list
```

---

## Cloud SQL Setup

### Step 1: Create PostgreSQL Instance

```powershell
$INSTANCE_NAME = "emailfixer-db"
$DB_NAME = "emailfixer"
$DB_USER = "appuser"

# Generate secure password (save this!)
$DB_PASSWORD = -join ((48..57) + (65..90) + (97..122) | Get-Random -Count 20 | ForEach-Object {[char]$_})
Write-Host "Database Password: $DB_PASSWORD" -ForegroundColor Green
Write-Host "SAVE THIS PASSWORD SECURELY!" -ForegroundColor Yellow

# Create Cloud SQL instance (PostgreSQL 15)
gcloud sql instances create $INSTANCE_NAME `
    --database-version=POSTGRES_15 `
    --tier=db-f1-micro `
    --region=$REGION `
    --network=default `
    --no-backup `
    --maintenance-window-day=SUN `
    --maintenance-window-hour=03 `
    --maintenance-release-channel=production

# Wait for instance to be ready
Write-Host "Waiting for instance to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 60
```

### Step 2: Create Database and User

```powershell
# Create database
gcloud sql databases create $DB_NAME --instance=$INSTANCE_NAME

# Create user
gcloud sql users create $DB_USER `
    --instance=$INSTANCE_NAME `
    --password=$DB_PASSWORD

# Get connection name (needed for Cloud Run)
$CONNECTION_NAME = gcloud sql instances describe $INSTANCE_NAME --format="value(connectionName)"
Write-Host "Connection Name: $CONNECTION_NAME" -ForegroundColor Green
```

### Step 3: Run Database Migrations

```powershell
# Option 1: Temporarily enable public IP for migrations
gcloud sql instances patch $INSTANCE_NAME --authorized-networks=0.0.0.0/0

# Get public IP
$DB_IP = gcloud sql instances describe $INSTANCE_NAME --format="value(ipAddresses[0].ipAddress)"
Write-Host "Database IP: $DB_IP" -ForegroundColor Green

# Build connection string
$CONNECTION_STRING = "Host=$DB_IP;Port=5432;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASSWORD"

# Run migrations
dotnet ef database update `
    -p EmailFixer.Infrastructure `
    -s EmailFixer.Api `
    --connection "$CONNECTION_STRING"

# Disable public IP after migrations
gcloud sql instances patch $INSTANCE_NAME --clear-authorized-networks

# Option 2: Use Cloud SQL Proxy (recommended for production)
# Download: https://cloud.google.com/sql/docs/postgres/sql-proxy
# Run: cloud_sql_proxy -instances=$CONNECTION_NAME=tcp:5432
# Then use: Host=localhost;Port=5432;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASSWORD
```

---

## Secret Manager Configuration

### Step 1: Create Secrets

```powershell
# Create database connection secret
$DB_CONNECTION = "Host=/cloudsql/$CONNECTION_NAME;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASSWORD"
echo $DB_CONNECTION | gcloud secrets create db-connection --data-file=-

# Create Paddle API secrets
echo "YOUR_PADDLE_API_KEY" | gcloud secrets create paddle-api-key --data-file=-
echo "YOUR_PADDLE_VENDOR_ID" | gcloud secrets create paddle-vendor-id --data-file=-
echo "YOUR_PADDLE_WEBHOOK_SECRET" | gcloud secrets create paddle-webhook-secret --data-file=-

# Create JWT secret key
$JWT_SECRET = -join ((48..57) + (65..90) + (97..122) | Get-Random -Count 32 | ForEach-Object {[char]$_})
echo $JWT_SECRET | gcloud secrets create jwt-secret-key --data-file=-

Write-Host "All secrets created successfully!" -ForegroundColor Green
```

### Step 2: Grant Access to Service Account

```powershell
# Create service account for GitHub Actions
$SA_NAME = "github-actions"
$SA_EMAIL = "$SA_NAME@$PROJECT_ID.iam.gserviceaccount.com"

gcloud iam service-accounts create $SA_NAME `
    --display-name="GitHub Actions Deploy"

# Grant IAM roles
$roles = @(
    "roles/run.admin",
    "roles/storage.admin",
    "roles/cloudsql.client",
    "roles/secretmanager.secretAccessor",
    "roles/iam.serviceAccountUser"
)

foreach ($role in $roles) {
    gcloud projects add-iam-policy-binding $PROJECT_ID `
        --member="serviceAccount:$SA_EMAIL" `
        --role=$role
}

# Grant secret access
$secrets = @("db-connection", "paddle-api-key", "paddle-vendor-id", "paddle-webhook-secret", "jwt-secret-key")

foreach ($secret in $secrets) {
    gcloud secrets add-iam-policy-binding $secret `
        --member="serviceAccount:$SA_EMAIL" `
        --role="roles/secretmanager.secretAccessor"
}
```

### Step 3: Create Service Account Key

```powershell
# Create and download service account key
gcloud iam service-accounts keys create github-actions-key.json `
    --iam-account=$SA_EMAIL

Write-Host "Service account key created: github-actions-key.json" -ForegroundColor Green
Write-Host "Upload this to GitHub Secrets as GCP_SA_KEY" -ForegroundColor Yellow
Write-Host "KEEP THIS FILE SECURE AND DELETE AFTER UPLOAD!" -ForegroundColor Red
```

---

## Cloud Storage Setup

### Step 1: Create Bucket for Blazor Client

```powershell
$BUCKET_NAME = "emailfixer-client"

# Create storage bucket
gsutil mb -l $REGION gs://$BUCKET_NAME

# Enable static website hosting
gsutil web set -m index.html -e 404.html gs://$BUCKET_NAME

# Make bucket public
gsutil iam ch allUsers:objectViewer gs://$BUCKET_NAME

# Set CORS configuration
gsutil cors set cors.json gs://$BUCKET_NAME

Write-Host "Bucket created: gs://$BUCKET_NAME" -ForegroundColor Green
Write-Host "URL: https://storage.googleapis.com/$BUCKET_NAME/index.html" -ForegroundColor Green
```

### Step 2: Configure CORS

The `cors.json` file should already exist in the project root:

```json
[
  {
    "origin": ["https://storage.googleapis.com", "https://emailfixer.com"],
    "method": ["GET", "POST", "PUT", "DELETE", "OPTIONS"],
    "responseHeader": ["Content-Type", "Authorization"],
    "maxAgeSeconds": 3600
  }
]
```

---

## Cloud Run Deployment

### Step 1: Configure Docker for GCR

```powershell
# Authenticate Docker with GCR
gcloud auth configure-docker
```

### Step 2: Build and Push Docker Image

```powershell
$IMAGE_URL = "gcr.io/$PROJECT_ID/emailfixer-api"

# Build Docker image
docker build -t "$IMAGE_URL:latest" -f EmailFixer.Api/Dockerfile .

# Push to GCR
docker push "$IMAGE_URL:latest"
```

### Step 3: Deploy to Cloud Run

```powershell
# Deploy API
gcloud run deploy emailfixer-api `
    --image="$IMAGE_URL:latest" `
    --platform=managed `
    --region=$REGION `
    --allow-unauthenticated `
    --add-cloudsql-instances=$CONNECTION_NAME `
    --set-env-vars="ASPNETCORE_ENVIRONMENT=Production" `
    --set-secrets="ConnectionStrings__DefaultConnection=db-connection:latest" `
    --set-secrets="Paddle__ApiKey=paddle-api-key:latest" `
    --set-secrets="Paddle__VendorId=paddle-vendor-id:latest" `
    --set-secrets="Paddle__WebhookSecret=paddle-webhook-secret:latest" `
    --set-secrets="Authentication__Jwt__SecretKey=jwt-secret-key:latest" `
    --memory=512Mi `
    --cpu=1 `
    --timeout=60s `
    --max-instances=10 `
    --min-instances=0

# Get service URL
$API_URL = gcloud run services describe emailfixer-api `
    --region=$REGION `
    --format="value(status.url)"

Write-Host "API deployed at: $API_URL" -ForegroundColor Green

# Test deployment
Invoke-WebRequest "$API_URL/health"
```

---

## GitHub Actions Configuration

### Step 1: Add GitHub Secrets

Go to your GitHub repository → Settings → Secrets and variables → Actions

Add the following secrets:

1. **GCP_SA_KEY**
   - Content: Entire contents of `github-actions-key.json`
   - How to get: `Get-Content github-actions-key.json -Raw`

2. **PADDLE_API_KEY**
   - Your Paddle API key from Paddle dashboard

3. **PADDLE_VENDOR_ID**
   - Your Paddle vendor/seller ID

4. **PADDLE_WEBHOOK_SECRET**
   - Your Paddle webhook secret key

### Step 2: Verify Workflow File

Ensure `.github/workflows/deploy-gcp.yml` exists with correct configuration.

### Step 3: Test Deployment

```powershell
# Push to main branch to trigger deployment
git add .
git commit -m "Configure GCP deployment"
git push origin main

# Or manually trigger workflow in GitHub UI
```

---

## Post-Deployment

### Step 1: Verify Deployments

```powershell
# Check API health
$API_URL = gcloud run services describe emailfixer-api --region=$REGION --format="value(status.url)"
Invoke-WebRequest "$API_URL/health"

# Check Swagger
Start-Process "$API_URL/swagger"

# Check client
Start-Process "https://storage.googleapis.com/$BUCKET_NAME/index.html"
```

### Step 2: Configure Custom Domain (Optional)

```powershell
# For API (Cloud Run)
gcloud run domain-mappings create --service emailfixer-api --domain api.emailfixer.com

# For Client (Cloud Storage via Load Balancer)
# Follow: https://cloud.google.com/storage/docs/hosting-static-website
```

### Step 3: Enable Cloud CDN (Optional)

See: `docs/deployment/cloud-cdn-setup.md`

---

## Monitoring & Alerts

### Step 1: Create Uptime Check

```powershell
# Create uptime check for API
gcloud monitoring uptime-checks create emailfixer-api-health `
    --display-name="EmailFixer API Health" `
    --resource-type="uptime-url" `
    --http-check-path="/health" `
    --port=443 `
    --monitored-resource="$API_URL" `
    --check-frequency=60
```

### Step 2: Create Alert Policy

```powershell
# Create notification channel (email)
gcloud alpha monitoring channels create `
    --display-name="Email Alerts" `
    --type=email `
    --channel-labels=email_address=admin@emailfixer.com

# Get channel ID
$CHANNEL_ID = gcloud alpha monitoring channels list --format="value(name)"

# Create alert policy for high error rate
# Use Cloud Console for easier configuration:
# https://console.cloud.google.com/monitoring/alerting
```

### Step 3: View Logs

```powershell
# Stream API logs
gcloud run services logs read emailfixer-api --region=$REGION --limit=50

# Follow logs in real-time
gcloud run services logs tail emailfixer-api --region=$REGION
```

---

## Troubleshooting

### Issue: Cloud Run deployment fails with "service account permissions"

**Solution:**
```powershell
# Re-grant necessary permissions
gcloud projects add-iam-policy-binding $PROJECT_ID `
    --member="serviceAccount:$SA_EMAIL" `
    --role="roles/iam.serviceAccountUser"
```

### Issue: Database migrations fail

**Solution:**
```powershell
# Use Cloud SQL Proxy for secure connection
cloud_sql_proxy -instances=$CONNECTION_NAME=tcp:5432

# Then run migrations with localhost
dotnet ef database update -p EmailFixer.Infrastructure -s EmailFixer.Api
```

### Issue: API returns 500 errors

**Solution:**
```powershell
# Check logs
gcloud run services logs read emailfixer-api --region=$REGION --limit=50

# Verify secrets are accessible
gcloud secrets versions access latest --secret="db-connection"
```

### Issue: Client cannot connect to API (CORS errors)

**Solution:**
```powershell
# Verify CORS settings in appsettings.Production.json
# Ensure Cloud Storage bucket origin is in AllowedOrigins

# Re-apply CORS to bucket
gsutil cors set cors.json gs://$BUCKET_NAME
```

### Issue: GitHub Actions deployment fails

**Solution:**
```powershell
# Verify service account has all required roles
gcloud projects get-iam-policy $PROJECT_ID `
    --flatten="bindings[].members" `
    --filter="bindings.members:$SA_EMAIL"

# Regenerate service account key
gcloud iam service-accounts keys create github-actions-key.json `
    --iam-account=$SA_EMAIL

# Update GitHub secret GCP_SA_KEY with new key contents
```

---

## Cost Optimization

### Estimated Monthly Costs

| Service | Configuration | Estimated Cost |
|---------|--------------|----------------|
| Cloud Run API | 512Mi RAM, 1 CPU, minimal traffic | $0-10 |
| Cloud SQL | db-f1-micro (shared CPU, 0.6GB RAM) | $10-15 |
| Cloud Storage | 5GB storage + egress | $0-2 |
| Container Registry | Image storage | $0-1 |
| **Total** | | **$10-28/month** |

### Cost Reduction Tips

1. **Cloud Run:** Set `--min-instances=0` to scale to zero when idle
2. **Cloud SQL:** Use smallest tier (db-f1-micro) for development
3. **Storage:** Enable lifecycle policies to delete old images
4. **Monitoring:** Limit log retention to 30 days

```powershell
# Set log retention
gcloud logging sinks update _Default --log-filter="resource.type=cloud_run_revision" --retention-days=30
```

---

## Next Steps

1. Configure custom domain names
2. Set up Cloud CDN for client
3. Configure backup strategy for Cloud SQL
4. Set up Cloud Armor for DDoS protection
5. Review security settings
6. Configure budget alerts

---

## References

- [Cloud Run Documentation](https://cloud.google.com/run/docs)
- [Cloud SQL for PostgreSQL](https://cloud.google.com/sql/docs/postgres)
- [Secret Manager Documentation](https://cloud.google.com/secret-manager/docs)
- [Cloud Storage Static Website](https://cloud.google.com/storage/docs/hosting-static-website)
- [GitHub Actions with GCP](https://github.com/google-github-actions)

---

**Last Updated:** 2025-11-09
**Version:** 1.0.0

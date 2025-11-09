# ☁️ Phase 5: Google Cloud Deployment Coordinator

**Phase ID:** phase5-deployment
**Parent Plan:** [emailfixer-completion-plan.md](emailfixer-completion-plan.md)
**Duration:** 3 hours
**Dependencies:** Phase 4 (Containerization)
**Priority:** P0 - Production Deployment
**Parallel Execution:** GCP setup can start anytime

## 📋 Phase Overview

Deploy Email Fixer to Google Cloud Platform using Cloud Run for API, Cloud Storage for Blazor client, Cloud SQL for PostgreSQL, and GitHub Actions for CI/CD. Focus on security, scalability, and cost optimization.

## 🏗️ GCP Architecture

```
┌─────────────────────────────────────────────┐
│            Cloud CDN + Load Balancer         │
└─────────────┬──────────────┬────────────────┘
              │              │
    ┌─────────▼────┐   ┌────▼──────────┐
    │ Cloud Storage│   │  Cloud Run API │
    │ (Blazor WASM)│   │  (Serverless)  │
    └──────────────┘   └────┬──────────┘
                            │
                    ┌───────▼─────────┐
                    │  Cloud SQL       │
                    │  PostgreSQL 15   │
                    └─────────────────┘
```

## 📝 Task Breakdown

### Task 5.1: Google Cloud Project Setup
**Duration:** 30 minutes
**LLM Readiness:** 95%
**Can start immediately**

```powershell
# Install gcloud CLI (Windows)
# Download from: https://cloud.google.com/sdk/docs/install

# Initialize gcloud
gcloud init

# Create new project (or use existing)
$PROJECT_ID = "emailfixer-prod"
gcloud projects create $PROJECT_ID --name="Email Fixer Production"

# Set as default project
gcloud config set project $PROJECT_ID

# Enable required APIs
gcloud services enable `
    run.googleapis.com `
    sqladmin.googleapis.com `
    compute.googleapis.com `
    containerregistry.googleapis.com `
    secretmanager.googleapis.com `
    cloudbuild.googleapis.com `
    storage-api.googleapis.com

# Set default region
$REGION = "us-central1"
gcloud config set run/region $REGION

# Create service account for CI/CD
gcloud iam service-accounts create github-actions `
    --display-name="GitHub Actions Deploy"

# Grant necessary permissions
$SA_EMAIL = "github-actions@$PROJECT_ID.iam.gserviceaccount.com"

gcloud projects add-iam-policy-binding $PROJECT_ID `
    --member="serviceAccount:$SA_EMAIL" `
    --role="roles/run.admin"

gcloud projects add-iam-policy-binding $PROJECT_ID `
    --member="serviceAccount:$SA_EMAIL" `
    --role="roles/storage.admin"

gcloud projects add-iam-policy-binding $PROJECT_ID `
    --member="serviceAccount:$SA_EMAIL" `
    --role="roles/cloudsql.client"

# Create and download service account key
gcloud iam service-accounts keys create github-actions-key.json `
    --iam-account=$SA_EMAIL

Write-Host "Save the contents of github-actions-key.json as GitHub secret GCP_SA_KEY" -ForegroundColor Yellow
```

### Task 5.2: Cloud SQL PostgreSQL Setup
**Duration:** 35 minutes
**LLM Readiness:** 90%

```powershell
$INSTANCE_NAME = "emailfixer-db"
$DB_NAME = "emailfixer"
$DB_USER = "appuser"
$DB_PASSWORD = [System.Web.Security.Membership]::GeneratePassword(16, 2)

# Create Cloud SQL instance
gcloud sql instances create $INSTANCE_NAME `
    --database-version=POSTGRES_15 `
    --tier=db-f1-micro `
    --region=$REGION `
    --network=default `
    --no-backup `
    --maintenance-window-day=SUN `
    --maintenance-window-hour=03

# Create database
gcloud sql databases create $DB_NAME `
    --instance=$INSTANCE_NAME

# Create user
gcloud sql users create $DB_USER `
    --instance=$INSTANCE_NAME `
    --password=$DB_PASSWORD

# Get connection name for Cloud Run
$CONNECTION_NAME = gcloud sql instances describe $INSTANCE_NAME --format="value(connectionName)"

Write-Host "Database Password: $DB_PASSWORD" -ForegroundColor Green
Write-Host "Connection Name: $CONNECTION_NAME" -ForegroundColor Green

# Enable public IP (temporary for migrations)
gcloud sql instances patch $INSTANCE_NAME --authorized-networks=0.0.0.0/0

# Run migrations (from local machine)
$CONNECTION_STRING = "Host=34.x.x.x;Port=5432;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASSWORD"

dotnet ef database update `
    -p EmailFixer.Infrastructure `
    -s EmailFixer.Api `
    --connection $CONNECTION_STRING

# Disable public IP after migrations
gcloud sql instances patch $INSTANCE_NAME --clear-authorized-networks
```

### Task 5.3: Secret Manager Configuration
**Duration:** 25 minutes
**LLM Readiness:** 100%

```powershell
# Create secrets
$secrets = @{
    "db-connection" = "Host=/cloudsql/$CONNECTION_NAME;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASSWORD"
    "paddle-api-key" = "your-paddle-api-key"
    "paddle-vendor-id" = "your-paddle-vendor-id"
    "paddle-webhook-secret" = "your-paddle-webhook-secret"
}

foreach ($secret in $secrets.GetEnumerator()) {
    Write-Host "Creating secret: $($secret.Key)" -ForegroundColor Yellow

    $secret.Value | gcloud secrets create $secret.Key `
        --data-file=- `
        --replication-policy="automatic"

    # Grant access to service account
    gcloud secrets add-iam-policy-binding $secret.Key `
        --member="serviceAccount:$SA_EMAIL" `
        --role="roles/secretmanager.secretAccessor"
}

# List created secrets
gcloud secrets list
```

### Task 5.4: Cloud Run API Deployment
**Duration:** 40 minutes
**LLM Readiness:** 90%

```powershell
# Build and push Docker image to GCR
$IMAGE_URL = "gcr.io/$PROJECT_ID/emailfixer-api"

# Configure Docker for GCR
gcloud auth configure-docker

# Build and push
docker build -t "$IMAGE_URL:latest" -f EmailFixer.Api/Dockerfile .
docker push "$IMAGE_URL:latest"

# Deploy to Cloud Run
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
    --memory=512Mi `
    --cpu=1 `
    --timeout=60 `
    --max-instances=10 `
    --min-instances=0

# Get service URL
$API_URL = gcloud run services describe emailfixer-api `
    --format="value(status.url)" `
    --region=$REGION

Write-Host "API deployed at: $API_URL" -ForegroundColor Green

# Test deployment
Invoke-WebRequest "$API_URL/health"
```

### Task 5.5: Cloud Storage Client Deployment
**Duration:** 30 minutes
**LLM Readiness:** 95%

```powershell
$BUCKET_NAME = "emailfixer-client"

# Create storage bucket
gsutil mb -l $REGION gs://$BUCKET_NAME

# Enable static website
gsutil web set -m index.html -e 404.html gs://$BUCKET_NAME

# Build Blazor client with API URL
dotnet publish EmailFixer.Client `
    -c Release `
    -o ./publish `
    /p:ApiBaseUrl=$API_URL

# Upload to bucket
gsutil -m rsync -r -d ./publish/wwwroot gs://$BUCKET_NAME

# Make bucket public
gsutil iam ch allUsers:objectViewer gs://$BUCKET_NAME

# Get bucket URL
$CLIENT_URL = "https://storage.googleapis.com/$BUCKET_NAME/index.html"
Write-Host "Client deployed at: $CLIENT_URL" -ForegroundColor Green

# Configure CORS for API
@"
[
  {
    "origin": ["https://storage.googleapis.com"],
    "method": ["GET", "POST", "PUT", "DELETE", "OPTIONS"],
    "responseHeader": ["Content-Type", "Authorization"],
    "maxAgeSeconds": 3600
  }
]
"@ | Out-File cors.json

gsutil cors set cors.json gs://$BUCKET_NAME
```

### Task 5.6: Cloud CDN Configuration
**Duration:** 25 minutes
**LLM Readiness:** 90%

```powershell
# Create backend bucket for CDN
gcloud compute backend-buckets create emailfixer-backend `
    --gcs-bucket-name=$BUCKET_NAME

# Create URL map
gcloud compute url-maps create emailfixer-lb `
    --default-backend-bucket=emailfixer-backend

# Create HTTPS proxy
gcloud compute target-https-proxies create emailfixer-https-proxy `
    --url-map=emailfixer-lb `
    --ssl-certificates=emailfixer-cert

# Reserve static IP
gcloud compute addresses create emailfixer-ip `
    --global

$STATIC_IP = gcloud compute addresses describe emailfixer-ip `
    --global `
    --format="value(address)"

# Create forwarding rule
gcloud compute forwarding-rules create emailfixer-https-rule `
    --global `
    --target-https-proxy=emailfixer-https-proxy `
    --address=$STATIC_IP `
    --ports=443

Write-Host "CDN configured with IP: $STATIC_IP" -ForegroundColor Green
Write-Host "Configure your domain DNS to point to this IP" -ForegroundColor Yellow
```

### Task 5.7: GitHub Actions CI/CD Pipeline
**Duration:** 50 minutes
**LLM Readiness:** 95%

**File:** `.github/workflows/deploy-gcp.yml`

```yaml
name: Deploy to Google Cloud

on:
  push:
    branches: [main]
  workflow_dispatch:

env:
  PROJECT_ID: emailfixer-prod
  REGION: us-central1
  API_SERVICE: emailfixer-api
  CLIENT_BUCKET: emailfixer-client

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore

      - name: Test
        run: dotnet test --no-build --verbosity normal

  deploy-api:
    needs: test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Google Cloud Auth
        uses: google-github-actions/auth@v1
        with:
          credentials_json: ${{ secrets.GCP_SA_KEY }}

      - name: Setup Cloud SDK
        uses: google-github-actions/setup-gcloud@v1

      - name: Configure Docker
        run: gcloud auth configure-docker

      - name: Build and Push API
        run: |
          docker build -t gcr.io/$PROJECT_ID/emailfixer-api:$GITHUB_SHA \
            -f EmailFixer.Api/Dockerfile .
          docker push gcr.io/$PROJECT_ID/emailfixer-api:$GITHUB_SHA

      - name: Deploy to Cloud Run
        run: |
          gcloud run deploy $API_SERVICE \
            --image gcr.io/$PROJECT_ID/emailfixer-api:$GITHUB_SHA \
            --region $REGION \
            --platform managed

  deploy-client:
    needs: test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Google Cloud Auth
        uses: google-github-actions/auth@v1
        with:
          credentials_json: ${{ secrets.GCP_SA_KEY }}

      - name: Setup Cloud SDK
        uses: google-github-actions/setup-gcloud@v1

      - name: Get API URL
        id: api-url
        run: |
          API_URL=$(gcloud run services describe $API_SERVICE \
            --region $REGION \
            --format 'value(status.url)')
          echo "url=$API_URL" >> $GITHUB_OUTPUT

      - name: Build Blazor Client
        run: |
          dotnet publish EmailFixer.Client \
            -c Release \
            -o ./publish \
            /p:ApiBaseUrl=${{ steps.api-url.outputs.url }}

      - name: Deploy to Cloud Storage
        run: |
          gsutil -m rsync -r -d ./publish/wwwroot gs://$CLIENT_BUCKET

      - name: Invalidate CDN Cache
        run: |
          gcloud compute url-maps invalidate-cdn-cache emailfixer-lb \
            --path "/*"

  notify:
    needs: [deploy-api, deploy-client]
    runs-on: ubuntu-latest
    if: always()
    steps:
      - name: Notify Success
        if: success()
        run: echo "Deployment successful!"

      - name: Notify Failure
        if: failure()
        run: echo "Deployment failed!"
```

**GitHub Secrets Configuration:**
```powershell
# Add secrets to GitHub repository
# Go to Settings → Secrets → Actions

# Required secrets:
# - GCP_SA_KEY: Contents of github-actions-key.json
# - PADDLE_API_KEY: Your Paddle API key
# - PADDLE_VENDOR_ID: Your Paddle vendor ID
# - PADDLE_WEBHOOK_SECRET: Your Paddle webhook secret
```

### Task 5.8: Monitoring & Alerts
**Duration:** 30 minutes
**LLM Readiness:** 90%

```powershell
# Create uptime checks
gcloud monitoring uptime-checks create emailfixer-api-health `
    --display-name="EmailFixer API Health" `
    --resource-type="uptime-url" `
    --hostname="$API_URL" `
    --path="/health" `
    --check-frequency="60"

# Create alert policy for errors
gcloud alpha monitoring policies create `
    --notification-channels="YOUR_CHANNEL_ID" `
    --display-name="API Error Rate" `
    --condition-display-name="High Error Rate" `
    --condition-threshold-value="0.01" `
    --condition-threshold-duration="60s"

# Create dashboard
@"
{
  "displayName": "EmailFixer Dashboard",
  "dashboardJson": {
    "widgets": [
      {
        "title": "API Request Count",
        "xyChart": {
          "dataSets": [{
            "timeSeriesQuery": {
              "timeSeriesFilter": {
                "filter": "resource.type=\"cloud_run_revision\" AND resource.label.service_name=\"emailfixer-api\""
              }
            }
          }]
        }
      },
      {
        "title": "API Latency",
        "xyChart": {
          "dataSets": [{
            "timeSeriesQuery": {
              "timeSeriesFilter": {
                "filter": "metric.type=\"run.googleapis.com/request_latencies\""
              }
            }
          }]
        }
      }
    ]
  }
}
"@ | gcloud monitoring dashboards create --config-from-file=-
```

## 🔄 Rollback Procedures

### API Rollback:
```powershell
# List revisions
gcloud run revisions list --service=emailfixer-api

# Route traffic to previous revision
gcloud run services update-traffic emailfixer-api `
    --to-revisions=emailfixer-api-00001-abc=100
```

### Client Rollback:
```powershell
# Keep previous build artifacts
# Restore from backup
gsutil -m rsync -r -d gs://emailfixer-client-backup gs://emailfixer-client
```

## ✅ Phase Completion Checklist

- [ ] GCP project created and configured
- [ ] All required APIs enabled
- [ ] Service account created with permissions
- [ ] Cloud SQL instance running
- [ ] Database migrated successfully
- [ ] Secrets stored in Secret Manager
- [ ] API deployed to Cloud Run
- [ ] Client deployed to Cloud Storage
- [ ] CDN configured (optional)
- [ ] CI/CD pipeline functional
- [ ] Monitoring and alerts configured
- [ ] DNS configured (if custom domain)
- [ ] SSL certificates valid
- [ ] Backup strategy documented

## 🚨 Production Readiness Checklist

- [ ] Remove all debug endpoints
- [ ] Enable authentication where needed
- [ ] Configure rate limiting
- [ ] Set up backup schedule for Cloud SQL
- [ ] Configure auto-scaling parameters
- [ ] Review and minimize IAM permissions
- [ ] Enable Cloud Armor DDoS protection
- [ ] Configure budget alerts
- [ ] Document disaster recovery plan

## 📊 Cost Optimization

| Service | Free Tier | Estimated Monthly |
|---------|-----------|-------------------|
| Cloud Run | 2M requests | $0-10 |
| Cloud SQL | None (db-f1-micro ~$10) | $10-15 |
| Cloud Storage | 5GB | $0-1 |
| Cloud CDN | None | $0-5 |
| **Total** | - | **$10-31/month** |

## 🔗 Next Phase

After successful completion:
1. ✅ Mark Phase 5 complete in master plan
2. ➡️ Proceed to [Phase 6: Documentation](phase6-documentation-coordinator.md)
3. 📝 Document production URLs and access

---

**Estimated Time:** 3 hours
**Actual Time:** _[To be filled by executor]_
**Executor Notes:** _[To be filled by executor]_
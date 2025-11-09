# Email Fixer Deployment Documentation

Complete deployment guides for Email Fixer application to Google Cloud Platform.

## Quick Start

### Prerequisites Checklist

- [ ] Google Cloud account with billing enabled
- [ ] gcloud CLI installed ([download](https://cloud.google.com/sdk/docs/install))
- [ ] .NET 8 SDK installed
- [ ] Docker Desktop installed (for local testing)
- [ ] GitHub repository access
- [ ] Paddle account credentials

### 5-Minute Quick Deploy

```powershell
# 1. Set project variables
$PROJECT_ID = "emailfixer-prod"
$REGION = "us-central1"

# 2. Create and configure project
gcloud projects create $PROJECT_ID
gcloud config set project $PROJECT_ID
gcloud services enable run.googleapis.com sqladmin.googleapis.com secretmanager.googleapis.com

# 3. Create Cloud SQL database
gcloud sql instances create emailfixer-db --database-version=POSTGRES_15 --tier=db-f1-micro --region=$REGION

# 4. Create secrets (replace placeholders with actual values)
echo "Host=/cloudsql/CONNECTION_NAME;Database=emailfixer;Username=appuser;Password=PASSWORD" | gcloud secrets create db-connection --data-file=-
echo "YOUR_PADDLE_KEY" | gcloud secrets create paddle-api-key --data-file=-

# 5. Deploy API to Cloud Run
docker build -t gcr.io/$PROJECT_ID/emailfixer-api -f EmailFixer.Api/Dockerfile .
docker push gcr.io/$PROJECT_ID/emailfixer-api
gcloud run deploy emailfixer-api --image=gcr.io/$PROJECT_ID/emailfixer-api --set-secrets="ConnectionStrings__DefaultConnection=db-connection:latest"

# 6. Configure GitHub Actions
# Add GCP_SA_KEY to GitHub Secrets and push to main branch
```

For detailed instructions, see guides below.

---

## Documentation Structure

### 1. [GCP Setup Guide](gcp-setup-guide.md) 🚀
**Start here for initial deployment**

Complete end-to-end setup guide covering:
- GCP project creation and configuration
- Enabling required APIs
- Service account setup
- Cloud Run deployment
- Cloud Storage configuration
- GitHub Actions CI/CD setup
- Monitoring and alerts
- Cost optimization

**Time:** 2-3 hours (first time)

---

### 2. [Cloud SQL Setup](cloud-sql-setup.md) 🗄️
**Database configuration and management**

Comprehensive PostgreSQL database guide:
- Creating Cloud SQL instance
- Database and user configuration
- Connection methods (Proxy, Public IP, Private IP, Unix Socket)
- Running Entity Framework migrations
- Backup and recovery procedures
- Performance tuning
- Security best practices

**Time:** 1 hour

---

### 3. [Secret Manager Guide](secret-manager-guide.md) 🔐
**Secure secrets management**

Complete secrets management guide:
- Creating and managing secrets
- IAM access control
- Using secrets in Cloud Run
- Using secrets in GitHub Actions
- Secret rotation procedures
- Audit logging
- Best practices

**Time:** 30 minutes

---

## Configuration Files Reference

### Root Directory Files

| File | Purpose | Required |
|------|---------|----------|
| `.gcloudignore` | Exclude files from GCP deployment | Yes |
| `cloudbuild.yaml` | Google Cloud Build configuration | Optional |
| `app.yaml` | App Engine configuration (alternative) | Optional |
| `cors.json` | CORS configuration for Cloud Storage | Yes |

### API Configuration

| File | Purpose | Required |
|------|---------|----------|
| `EmailFixer.Api/appsettings.Production.json` | Production environment settings | Yes |
| `EmailFixer.Api/Dockerfile` | Docker image definition | Yes |

### CI/CD Configuration

| File | Purpose | Required |
|------|---------|----------|
| `.github/workflows/deploy-gcp.yml` | GitHub Actions deployment pipeline | Yes |

---

## Deployment Methods

### Method 1: GitHub Actions (Recommended for Production)

**Pros:**
- Fully automated CI/CD
- Runs tests before deployment
- Deploys on every push to main
- Rollback capability
- Audit trail

**Setup:**
1. Follow [GCP Setup Guide](gcp-setup-guide.md) sections 1-5
2. Configure GitHub Secrets
3. Push to main branch
4. GitHub Actions handles rest automatically

### Method 2: Google Cloud Build

**Pros:**
- Native GCP integration
- Triggered from git push or manually
- Concurrent builds

**Setup:**
```powershell
# Submit build manually
gcloud builds submit --config=cloudbuild.yaml

# Or set up automatic triggers
gcloud builds triggers create github \
    --repo-name=emailfixer \
    --repo-owner=YOUR_GITHUB_USERNAME \
    --branch-pattern="^main$" \
    --build-config=cloudbuild.yaml
```

### Method 3: Manual Deployment (Development/Testing)

**Pros:**
- Full control
- Good for testing
- No external dependencies

**Setup:**
```powershell
# Build and push
docker build -t gcr.io/$PROJECT_ID/emailfixer-api -f EmailFixer.Api/Dockerfile .
docker push gcr.io/$PROJECT_ID/emailfixer-api

# Deploy
gcloud run deploy emailfixer-api --image=gcr.io/$PROJECT_ID/emailfixer-api:latest
```

---

## Architecture Overview

```
┌─────────────────────────────────────────────────┐
│         User Browser / Mobile App               │
└───────────────┬─────────────────┬───────────────┘
                │                 │
      ┌─────────▼────┐      ┌────▼──────────┐
      │ Cloud Storage│      │  Cloud Run     │
      │ (Blazor WASM)│      │  (ASP.NET API) │
      │              │      │                │
      │ - index.html │      │ - REST API     │
      │ - *.wasm     │      │ - Swagger      │
      │ - *.js       │      │ - Health check │
      └──────────────┘      └────┬──────────┘
                                 │
                    ┌────────────┼────────────┐
                    │            │            │
            ┌───────▼─────┐  ┌──▼──────┐  ┌─▼────────┐
            │  Cloud SQL  │  │ Secret  │  │  Paddle  │
            │ PostgreSQL  │  │ Manager │  │   API    │
            │             │  │         │  │          │
            │ - Users     │  │ - Keys  │  │ - Payment│
            │ - Subs      │  │ - Pwd   │  │ - Webhook│
            └─────────────┘  └─────────┘  └──────────┘
```

---

## Estimated Costs

### Monthly Cost Breakdown (Production)

| Service | Configuration | Monthly Cost |
|---------|--------------|--------------|
| Cloud Run API | 512MB RAM, 1 CPU, 100K requests | $0-10 |
| Cloud SQL | db-f1-micro, PostgreSQL 15 | $10-15 |
| Cloud Storage | 5GB + 10GB egress | $0-2 |
| Container Registry | 2GB images | $0-1 |
| Secret Manager | 10 secrets, 1000 accesses | $0.30 |
| **Total** | | **$10-28/month** |

### Cost Optimization Tips

1. **Set `min-instances=0`** - Scale to zero when idle
2. **Use db-f1-micro** - Smallest Cloud SQL tier for testing
3. **Enable lifecycle policies** - Delete old Docker images
4. **Set log retention** - Limit to 30 days
5. **Use budget alerts** - Get notified at 50%, 90%, 100%

```powershell
# Set budget alert
gcloud billing budgets create --billing-account=BILLING_ID \
    --display-name="EmailFixer Budget" \
    --budget-amount=50 \
    --threshold-rule=percent=50 \
    --threshold-rule=percent=90
```

---

## Environment Variables Reference

### Required Secrets (Secret Manager)

| Secret Name | Description | Example Value |
|-------------|-------------|---------------|
| `db-connection` | PostgreSQL connection string | `Host=/cloudsql/...;Database=emailfixer;...` |
| `paddle-api-key` | Paddle API key | `live_abc123...` |
| `paddle-vendor-id` | Paddle seller ID | `12345` |
| `paddle-webhook-secret` | Paddle webhook verification | `whsec_abc123...` |
| `jwt-secret-key` | JWT signing key | Random 64-char string |

### Optional Secrets

| Secret Name | Description | When Needed |
|-------------|-------------|-------------|
| `smtp-username` | Email sending | If sending emails |
| `smtp-password` | Email password | If sending emails |
| `redis-connection` | Redis cache | If using caching |

---

## Deployment Checklist

### Pre-Deployment

- [ ] GCP project created and billing enabled
- [ ] All required APIs enabled
- [ ] Service accounts created with correct permissions
- [ ] Cloud SQL instance created and configured
- [ ] Database migrated successfully
- [ ] All secrets stored in Secret Manager
- [ ] GitHub Secrets configured
- [ ] Dockerfile builds successfully locally
- [ ] Tests passing locally

### Deployment

- [ ] API deployed to Cloud Run
- [ ] Client deployed to Cloud Storage
- [ ] Health endpoint responding (`/health`)
- [ ] Swagger UI accessible (`/swagger`)
- [ ] Database connections working
- [ ] Paddle webhooks configured
- [ ] CORS configured correctly

### Post-Deployment

- [ ] Custom domain configured (optional)
- [ ] CDN enabled (optional)
- [ ] Monitoring and alerts set up
- [ ] Backup strategy configured
- [ ] Rollback procedure tested
- [ ] Documentation updated with actual URLs
- [ ] Team trained on deployment process

---

## Common Issues & Solutions

### Issue: Cloud Run returns 500 errors

**Solution:**
```powershell
# Check logs
gcloud run services logs read emailfixer-api --limit=50

# Common causes:
# - Database connection failed (check Secret Manager)
# - Missing secrets (verify IAM permissions)
# - Migration not applied (run dotnet ef database update)
```

### Issue: Client cannot connect to API (CORS)

**Solution:**
```powershell
# Verify CORS in appsettings.Production.json includes Storage origin
# Reapply CORS to bucket
gsutil cors set cors.json gs://emailfixer-client
```

### Issue: GitHub Actions deployment fails

**Solution:**
```powershell
# Verify service account has all required roles
gcloud projects get-iam-policy $PROJECT_ID \
    --flatten="bindings[].members" \
    --filter="bindings.members:github-actions@"

# Required roles:
# - roles/run.admin
# - roles/storage.admin
# - roles/secretmanager.secretAccessor
# - roles/iam.serviceAccountUser
```

### Issue: Database migrations fail

**Solution:**
```powershell
# Use Cloud SQL Proxy
cloud_sql_proxy -instances=CONNECTION_NAME=tcp:5432

# Then run migrations
dotnet ef database update -p EmailFixer.Infrastructure -s EmailFixer.Api
```

For more troubleshooting, see specific guides above.

---

## Rollback Procedures

### Rollback API Deployment

```powershell
# List revisions
gcloud run revisions list --service=emailfixer-api

# Route 100% traffic to previous revision
gcloud run services update-traffic emailfixer-api \
    --to-revisions=emailfixer-api-00001-abc=100
```

### Rollback Client Deployment

```powershell
# Keep backups of previous builds
gsutil -m rsync -r -d gs://emailfixer-client gs://emailfixer-client-backup

# Restore from backup
gsutil -m rsync -r -d gs://emailfixer-client-backup gs://emailfixer-client
```

### Rollback Database Migration

```powershell
# Use EF Core to rollback to specific migration
dotnet ef database update PreviousMigrationName \
    -p EmailFixer.Infrastructure \
    -s EmailFixer.Api \
    --connection "$CONNECTION_STRING"
```

---

## Next Steps After Deployment

1. **Configure Custom Domain**
   - API: `api.emailfixer.com` → Cloud Run
   - Client: `app.emailfixer.com` → Cloud Storage + Load Balancer

2. **Enable Cloud CDN**
   - Reduce latency for global users
   - Lower egress costs
   - Improve performance

3. **Set Up Monitoring**
   - Uptime checks
   - Error rate alerts
   - Performance metrics
   - Budget alerts

4. **Configure Backups**
   - Automated Cloud SQL backups
   - Client artifact backups
   - Database exports to Cloud Storage

5. **Security Hardening**
   - Enable Cloud Armor (DDoS protection)
   - Configure rate limiting
   - Review IAM permissions
   - Enable VPC Service Controls

6. **Performance Optimization**
   - Enable connection pooling
   - Configure CDN caching
   - Add Cloud SQL read replicas
   - Optimize Docker image size

---

## Support & Resources

### Documentation Links

- [Cloud Run Docs](https://cloud.google.com/run/docs)
- [Cloud SQL Docs](https://cloud.google.com/sql/docs/postgres)
- [Secret Manager Docs](https://cloud.google.com/secret-manager/docs)
- [GitHub Actions with GCP](https://github.com/google-github-actions)

### Useful Commands

```powershell
# View API logs
gcloud run services logs tail emailfixer-api

# View deployment status
gcloud run services describe emailfixer-api

# View database status
gcloud sql instances describe emailfixer-db

# List secrets
gcloud secrets list

# View costs
gcloud billing accounts list
# Then check Cloud Console → Billing
```

### Monitoring URLs

After deployment, bookmark these:

- **Cloud Console:** https://console.cloud.google.com
- **Cloud Run Services:** https://console.cloud.google.com/run
- **Cloud SQL Instances:** https://console.cloud.google.com/sql
- **Secret Manager:** https://console.cloud.google.com/security/secret-manager
- **Logs Explorer:** https://console.cloud.google.com/logs
- **Billing:** https://console.cloud.google.com/billing

---

## Contributing

Found an issue in deployment docs? Please update this documentation and submit a PR.

---

**Last Updated:** 2025-11-09
**Version:** 1.0.0
**Maintainer:** Email Fixer Team

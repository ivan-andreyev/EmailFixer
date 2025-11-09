# Secret Manager Configuration Guide

Complete guide for managing secrets in Google Cloud Secret Manager for Email Fixer.

## Table of Contents

1. [Overview](#overview)
2. [Setup Secret Manager](#setup-secret-manager)
3. [Create Secrets](#create-secrets)
4. [Access Control](#access-control)
5. [Using Secrets in Cloud Run](#using-secrets-in-cloud-run)
6. [Using Secrets in GitHub Actions](#using-secrets-in-github-actions)
7. [Secret Rotation](#secret-rotation)
8. [Best Practices](#best-practices)
9. [Troubleshooting](#troubleshooting)

---

## Overview

Google Cloud Secret Manager is a secure and convenient storage system for API keys, passwords, certificates, and other sensitive data. Email Fixer uses Secret Manager to store:

- Database connection strings
- Paddle API credentials
- JWT signing keys
- Third-party API keys

**Benefits:**
- Encrypted at rest and in transit
- Fine-grained access control with IAM
- Automatic versioning
- Audit logging
- Seamless integration with Cloud Run

---

## Setup Secret Manager

### Prerequisites

```powershell
# Set project ID
$PROJECT_ID = "emailfixer-prod"
gcloud config set project $PROJECT_ID

# Enable Secret Manager API
gcloud services enable secretmanager.googleapis.com

# Verify API is enabled
gcloud services list --enabled --filter="name:secretmanager.googleapis.com"
```

### Install Secret Manager CLI (Optional)

```powershell
# gcloud already includes Secret Manager commands
gcloud secrets --help
```

---

## Create Secrets

### 1. Database Connection String

```powershell
# Get Cloud SQL connection name
$CONNECTION_NAME = gcloud sql instances describe emailfixer-db --format="value(connectionName)"

# Build connection string (Cloud Run uses Unix socket)
$DB_CONNECTION = "Host=/cloudsql/$CONNECTION_NAME;Database=emailfixer;Username=appuser;Password=YOUR_DB_PASSWORD"

# Create secret
echo $DB_CONNECTION | gcloud secrets create db-connection `
    --data-file=- `
    --replication-policy="automatic" `
    --labels="app=emailfixer,env=production"

Write-Host "Created secret: db-connection" -ForegroundColor Green
```

**Alternative: Connection string for Public IP / Proxy**

```powershell
# If using public IP or Cloud SQL Proxy
$DB_CONNECTION_PUBLIC = "Host=INSTANCE_IP;Port=5432;Database=emailfixer;Username=appuser;Password=YOUR_DB_PASSWORD;SslMode=Require"

echo $DB_CONNECTION_PUBLIC | gcloud secrets create db-connection-public --data-file=-
```

### 2. Paddle API Credentials

```powershell
# Paddle API Key
echo "YOUR_PADDLE_API_KEY" | gcloud secrets create paddle-api-key `
    --data-file=- `
    --replication-policy="automatic" `
    --labels="app=emailfixer,env=production,service=paddle"

# Paddle Vendor ID
echo "YOUR_PADDLE_VENDOR_ID" | gcloud secrets create paddle-vendor-id `
    --data-file=- `
    --replication-policy="automatic" `
    --labels="app=emailfixer,env=production,service=paddle"

# Paddle Webhook Secret
echo "YOUR_PADDLE_WEBHOOK_SECRET" | gcloud secrets create paddle-webhook-secret `
    --data-file=- `
    --replication-policy="automatic" `
    --labels="app=emailfixer,env=production,service=paddle"

Write-Host "Created Paddle secrets" -ForegroundColor Green
```

### 3. JWT Secret Key

```powershell
# Generate secure random key (32 bytes, base64 encoded)
$JWT_SECRET = -join ((48..57) + (65..90) + (97..122) | Get-Random -Count 64 | ForEach-Object {[char]$_})

echo $JWT_SECRET | gcloud secrets create jwt-secret-key `
    --data-file=- `
    --replication-policy="automatic" `
    --labels="app=emailfixer,env=production,type=auth"

Write-Host "JWT Secret Key: $JWT_SECRET" -ForegroundColor Yellow
Write-Host "Created secret: jwt-secret-key" -ForegroundColor Green
```

### 4. Additional Secrets (Optional)

```powershell
# SMTP credentials (if sending emails)
echo "smtp-username" | gcloud secrets create smtp-username --data-file=-
echo "smtp-password" | gcloud secrets create smtp-password --data-file=-

# Redis connection (if using caching)
echo "redis://redis-host:6379" | gcloud secrets create redis-connection --data-file=-

# External API keys
echo "sendgrid-api-key" | gcloud secrets create sendgrid-api-key --data-file=-
echo "stripe-api-key" | gcloud secrets create stripe-api-key --data-file=-
```

### 5. Verify Secrets Created

```powershell
# List all secrets
gcloud secrets list

# Get specific secret details
gcloud secrets describe db-connection

# View secret value (requires secretAccessor role)
gcloud secrets versions access latest --secret="db-connection"
```

---

## Access Control

### Grant Access to Cloud Run Service Account

```powershell
# Get default compute service account
$PROJECT_NUMBER = gcloud projects describe $PROJECT_ID --format="value(projectNumber)"
$COMPUTE_SA = "$PROJECT_NUMBER-compute@developer.gserviceaccount.com"

# Or use custom service account
# $COMPUTE_SA = "emailfixer-api@$PROJECT_ID.iam.gserviceaccount.com"

# Grant access to each secret
$secrets = @(
    "db-connection",
    "paddle-api-key",
    "paddle-vendor-id",
    "paddle-webhook-secret",
    "jwt-secret-key"
)

foreach ($secret in $secrets) {
    gcloud secrets add-iam-policy-binding $secret `
        --member="serviceAccount:$COMPUTE_SA" `
        --role="roles/secretmanager.secretAccessor"

    Write-Host "Granted access to $secret" -ForegroundColor Green
}
```

### Grant Access to GitHub Actions Service Account

```powershell
# Service account created earlier
$GITHUB_SA = "github-actions@$PROJECT_ID.iam.gserviceaccount.com"

foreach ($secret in $secrets) {
    gcloud secrets add-iam-policy-binding $secret `
        --member="serviceAccount:$GITHUB_SA" `
        --role="roles/secretmanager.secretAccessor"
}

Write-Host "Granted GitHub Actions access to secrets" -ForegroundColor Green
```

### Grant Access to Specific User (for debugging)

```powershell
$USER_EMAIL = "admin@example.com"

gcloud secrets add-iam-policy-binding db-connection `
    --member="user:$USER_EMAIL" `
    --role="roles/secretmanager.secretAccessor"
```

### Remove Access

```powershell
# Revoke access
gcloud secrets remove-iam-policy-binding db-connection `
    --member="user:$USER_EMAIL" `
    --role="roles/secretmanager.secretAccessor"
```

### View IAM Policy

```powershell
# View who has access to a secret
gcloud secrets get-iam-policy db-connection
```

---

## Using Secrets in Cloud Run

### Method 1: Environment Variables (Recommended)

```powershell
# Deploy with secrets as environment variables
gcloud run deploy emailfixer-api `
    --image=gcr.io/$PROJECT_ID/emailfixer-api:latest `
    --set-secrets="ConnectionStrings__DefaultConnection=db-connection:latest" `
    --set-secrets="Paddle__ApiKey=paddle-api-key:latest" `
    --set-secrets="Paddle__VendorId=paddle-vendor-id:latest" `
    --set-secrets="Paddle__WebhookSecret=paddle-webhook-secret:latest" `
    --set-secrets="Authentication__Jwt__SecretKey=jwt-secret-key:latest" `
    --region=us-central1
```

**Format:** `ENV_VAR_NAME=secret-name:version`

- `latest` - Always use the latest version
- `1`, `2`, `3` - Use specific version

**Example in appsettings.json:**

The secrets are automatically mapped to configuration hierarchy using double underscores:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "value from db-connection secret"
  },
  "Paddle": {
    "ApiKey": "value from paddle-api-key secret",
    "VendorId": "value from paddle-vendor-id secret",
    "WebhookSecret": "value from paddle-webhook-secret secret"
  }
}
```

### Method 2: Volume Mounts

```powershell
# Mount secrets as files
gcloud run deploy emailfixer-api `
    --image=gcr.io/$PROJECT_ID/emailfixer-api:latest `
    --add-volume name=secrets,type=secret,secret=db-connection `
    --add-volume-mount volume=secrets,mount-path=/secrets `
    --region=us-central1

# Access in code: File.ReadAllText("/secrets/db-connection")
```

### Update Secrets in Running Service

```powershell
# Cloud Run automatically uses latest secret version
# Just update the secret, no need to redeploy:

echo "NEW_CONNECTION_STRING" | gcloud secrets versions add db-connection --data-file=-

# Or force redeployment to pick up changes immediately:
gcloud run services update emailfixer-api --region=us-central1
```

---

## Using Secrets in GitHub Actions

### Add Service Account Key to GitHub Secrets

```powershell
# Create service account key (if not already done)
gcloud iam service-accounts keys create github-actions-key.json `
    --iam-account=github-actions@$PROJECT_ID.iam.gserviceaccount.com

# Copy to clipboard (Windows)
Get-Content github-actions-key.json -Raw | Set-Clipboard

# Copy to clipboard (Linux/Mac)
# cat github-actions-key.json | pbcopy
```

**In GitHub:**
1. Go to repository → Settings → Secrets and variables → Actions
2. Click "New repository secret"
3. Name: `GCP_SA_KEY`
4. Value: Paste JSON content
5. Click "Add secret"

### Access Secrets in Workflow

**Method 1: Use GitHub Actions directly**

```yaml
- name: Authenticate to Google Cloud
  uses: google-github-actions/auth@v2
  with:
    credentials_json: ${{ secrets.GCP_SA_KEY }}

- name: Get secret value
  run: |
    SECRET_VALUE=$(gcloud secrets versions access latest --secret="paddle-api-key")
    echo "::add-mask::$SECRET_VALUE"
    echo "PADDLE_KEY=$SECRET_VALUE" >> $GITHUB_ENV
```

**Method 2: Pass to Cloud Run deployment**

Secrets are automatically available to Cloud Run if the service account has access:

```yaml
- name: Deploy to Cloud Run
  run: |
    gcloud run deploy emailfixer-api \
      --image gcr.io/$PROJECT_ID/emailfixer-api:${{ github.sha }} \
      --set-secrets="Paddle__ApiKey=paddle-api-key:latest"
```

---

## Secret Rotation

### Rotate Database Password

```powershell
# 1. Generate new password
$NEW_DB_PASSWORD = -join ((48..57) + (65..90) + (97..122) | Get-Random -Count 20 | ForEach-Object {[char]$_})

Write-Host "New Password: $NEW_DB_PASSWORD" -ForegroundColor Green

# 2. Update database user password
gcloud sql users set-password appuser `
    --instance=emailfixer-db `
    --password=$NEW_DB_PASSWORD

# 3. Update secret (creates new version)
$CONNECTION_NAME = gcloud sql instances describe emailfixer-db --format="value(connectionName)"
$NEW_CONNECTION = "Host=/cloudsql/$CONNECTION_NAME;Database=emailfixer;Username=appuser;Password=$NEW_DB_PASSWORD"

echo $NEW_CONNECTION | gcloud secrets versions add db-connection --data-file=-

# 4. Cloud Run automatically picks up latest version
# Or force immediate update:
gcloud run services update emailfixer-api --region=us-central1

Write-Host "Database password rotated successfully" -ForegroundColor Green
```

### Rotate API Keys

```powershell
# 1. Get new API key from Paddle dashboard
$NEW_PADDLE_KEY = Read-Host -Prompt "Enter new Paddle API key"

# 2. Update secret
echo $NEW_PADDLE_KEY | gcloud secrets versions add paddle-api-key --data-file=-

# 3. Verify
gcloud secrets versions list paddle-api-key

# 4. Cloud Run picks up automatically
Write-Host "Paddle API key rotated" -ForegroundColor Green
```

### Disable Old Secret Version

```powershell
# List versions
gcloud secrets versions list db-connection

# Disable old version (keeps for audit)
gcloud secrets versions disable 1 --secret=db-connection

# Re-enable if needed
gcloud secrets versions enable 1 --secret=db-connection

# Destroy permanently (cannot be undone!)
gcloud secrets versions destroy 1 --secret=db-connection
```

### Automated Rotation (Advanced)

```powershell
# Use Cloud Scheduler + Cloud Functions for automatic rotation
# Example: Rotate every 90 days

# 1. Create Cloud Function to rotate secrets
# 2. Create Cloud Scheduler job
gcloud scheduler jobs create http rotate-secrets `
    --schedule="0 0 * */3 *" `
    --uri="https://REGION-PROJECT.cloudfunctions.net/rotateSecrets" `
    --http-method=POST
```

---

## Best Practices

### 1. Use Latest Version in Production

```powershell
# Always use :latest for auto-updates
--set-secrets="Paddle__ApiKey=paddle-api-key:latest"

# Pin specific version only for testing
--set-secrets="Paddle__ApiKey=paddle-api-key:5"
```

### 2. Label Secrets

```powershell
# Add labels for organization
gcloud secrets create my-secret --labels="env=prod,app=emailfixer,team=backend"

# Filter secrets by label
gcloud secrets list --filter="labels.env=prod"
```

### 3. Never Commit Secrets

```powershell
# Add to .gitignore
*.key
*.pem
*.json (service account keys)
.env
secrets/
*secret*
```

### 4. Audit Secret Access

```powershell
# View audit logs
gcloud logging read 'resource.type="secretmanager.googleapis.com/Secret"' --limit=50

# Filter by secret
gcloud logging read 'protoPayload.resourceName:"secrets/db-connection"' --limit=10
```

### 5. Use Separate Secrets for Environments

```powershell
# Production
gcloud secrets create db-connection-prod --data-file=-

# Staging
gcloud secrets create db-connection-staging --data-file=-

# Development
gcloud secrets create db-connection-dev --data-file=-
```

### 6. Implement Least Privilege

```powershell
# Only grant secretAccessor, never secretAdmin to applications
gcloud secrets add-iam-policy-binding db-connection `
    --member="serviceAccount:app@project.iam.gserviceaccount.com" `
    --role="roles/secretmanager.secretAccessor"

# Grant secretAdmin only to administrators
gcloud secrets add-iam-policy-binding db-connection `
    --member="user:admin@example.com" `
    --role="roles/secretmanager.admin"
```

### 7. Regular Rotation Schedule

- **Critical secrets (DB password, JWT key):** Every 90 days
- **API keys:** Every 180 days
- **Service account keys:** Every 90 days
- **Webhooks secrets:** Every 180 days

---

## Troubleshooting

### Issue: Cannot access secret from Cloud Run

**Check IAM permissions:**
```powershell
# Verify service account has access
gcloud secrets get-iam-policy db-connection

# Add missing permission
$PROJECT_NUMBER = gcloud projects describe $PROJECT_ID --format="value(projectNumber)"
$COMPUTE_SA = "$PROJECT_NUMBER-compute@developer.gserviceaccount.com"

gcloud secrets add-iam-policy-binding db-connection `
    --member="serviceAccount:$COMPUTE_SA" `
    --role="roles/secretmanager.secretAccessor"
```

**Check Secret Manager API is enabled:**
```powershell
gcloud services enable secretmanager.googleapis.com
```

### Issue: Secret not found

```powershell
# List all secrets
gcloud secrets list

# Check if secret exists
gcloud secrets describe db-connection

# Verify version exists
gcloud secrets versions list db-connection
```

### Issue: Old secret version still in use

```powershell
# Cloud Run caches secrets briefly
# Force immediate update:
gcloud run services update emailfixer-api --region=us-central1

# Or specify exact version:
gcloud run services update emailfixer-api `
    --update-secrets="Paddle__ApiKey=paddle-api-key:10"
```

### Issue: GitHub Actions cannot access secrets

```powershell
# Verify service account has correct roles
gcloud projects get-iam-policy $PROJECT_ID `
    --flatten="bindings[].members" `
    --filter="bindings.members:github-actions@$PROJECT_ID.iam.gserviceaccount.com"

# Required roles:
# - roles/secretmanager.secretAccessor
# - roles/run.admin
# - roles/iam.serviceAccountUser

# Grant missing roles
gcloud projects add-iam-policy-binding $PROJECT_ID `
    --member="serviceAccount:github-actions@$PROJECT_ID.iam.gserviceaccount.com" `
    --role="roles/secretmanager.secretAccessor"
```

### Issue: Permission denied when creating secret

```powershell
# Check if you have secretmanager.admin role
gcloud projects get-iam-policy $PROJECT_ID `
    --flatten="bindings[].members" `
    --filter="bindings.members:user:YOUR_EMAIL"

# Grant yourself admin role (requires project owner)
gcloud projects add-iam-policy-binding $PROJECT_ID `
    --member="user:YOUR_EMAIL" `
    --role="roles/secretmanager.admin"
```

---

## Secret Management Checklist

- [ ] Secret Manager API enabled
- [ ] All required secrets created
- [ ] Secrets have appropriate labels
- [ ] IAM permissions granted to service accounts
- [ ] Service account keys stored in GitHub Secrets
- [ ] Cloud Run configured to use secrets
- [ ] Secret rotation schedule documented
- [ ] Audit logging enabled
- [ ] Backup of critical secrets (encrypted offline)
- [ ] Team members know rotation procedure

---

## CLI Quick Reference

```powershell
# Create secret
echo "value" | gcloud secrets create SECRET_NAME --data-file=-

# Update secret (new version)
echo "new-value" | gcloud secrets versions add SECRET_NAME --data-file=-

# Read secret
gcloud secrets versions access latest --secret=SECRET_NAME

# List secrets
gcloud secrets list

# Delete secret
gcloud secrets delete SECRET_NAME

# Grant access
gcloud secrets add-iam-policy-binding SECRET_NAME `
    --member="serviceAccount:SA@project.iam.gserviceaccount.com" `
    --role="roles/secretmanager.secretAccessor"

# View access
gcloud secrets get-iam-policy SECRET_NAME

# Disable version
gcloud secrets versions disable VERSION_ID --secret=SECRET_NAME

# List versions
gcloud secrets versions list SECRET_NAME
```

---

## References

- [Secret Manager Documentation](https://cloud.google.com/secret-manager/docs)
- [Using secrets in Cloud Run](https://cloud.google.com/run/docs/configuring/secrets)
- [IAM Roles for Secret Manager](https://cloud.google.com/secret-manager/docs/access-control)
- [Secret Manager Pricing](https://cloud.google.com/secret-manager/pricing)

---

**Last Updated:** 2025-11-09
**Version:** 1.0.0

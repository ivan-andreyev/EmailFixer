# Cloud SQL PostgreSQL Setup Guide

Detailed guide for setting up Cloud SQL PostgreSQL for Email Fixer.

## Table of Contents

1. [Instance Creation](#instance-creation)
2. [Database Configuration](#database-configuration)
3. [Connection Methods](#connection-methods)
4. [Database Migrations](#database-migrations)
5. [Backup & Recovery](#backup--recovery)
6. [Performance Tuning](#performance-tuning)
7. [Security Best Practices](#security-best-practices)
8. [Troubleshooting](#troubleshooting)

---

## Instance Creation

### Prerequisites

- gcloud CLI installed and authenticated
- GCP project created with billing enabled
- Cloud SQL Admin API enabled

### Create PostgreSQL Instance

```powershell
# Set variables
$PROJECT_ID = "emailfixer-prod"
$INSTANCE_NAME = "emailfixer-db"
$REGION = "us-central1"
$DB_VERSION = "POSTGRES_15"

# For development (smallest/cheapest tier)
$TIER = "db-f1-micro"  # 0.6GB RAM, shared CPU - ~$10/month

# For production (recommended)
# $TIER = "db-g1-small"  # 1.7GB RAM, 1 shared CPU - ~$25/month
# $TIER = "db-custom-2-4096"  # 2 vCPU, 4GB RAM - ~$80/month

# Create instance
gcloud sql instances create $INSTANCE_NAME `
    --database-version=$DB_VERSION `
    --tier=$TIER `
    --region=$REGION `
    --network=default `
    --no-backup `
    --maintenance-window-day=SUN `
    --maintenance-window-hour=03 `
    --maintenance-release-channel=production `
    --storage-type=SSD `
    --storage-size=10GB `
    --storage-auto-increase `
    --storage-auto-increase-limit=50GB

Write-Host "Instance creation started. This may take 5-10 minutes..." -ForegroundColor Yellow
```

### Verify Instance Creation

```powershell
# Check instance status
gcloud sql instances describe $INSTANCE_NAME

# Get connection name (needed for Cloud Run)
$CONNECTION_NAME = gcloud sql instances describe $INSTANCE_NAME --format="value(connectionName)"
Write-Host "Connection Name: $CONNECTION_NAME" -ForegroundColor Green

# Get instance IP
$INSTANCE_IP = gcloud sql instances describe $INSTANCE_NAME --format="value(ipAddresses[0].ipAddress)"
Write-Host "Instance IP: $INSTANCE_IP" -ForegroundColor Green
```

---

## Database Configuration

### Create Database and User

```powershell
$DB_NAME = "emailfixer"
$DB_USER = "appuser"

# Generate secure password (20 characters, alphanumeric)
$DB_PASSWORD = -join ((48..57) + (65..90) + (97..122) | Get-Random -Count 20 | ForEach-Object {[char]$_})

Write-Host "==================================" -ForegroundColor Cyan
Write-Host "Database Password: $DB_PASSWORD" -ForegroundColor Green
Write-Host "==================================" -ForegroundColor Cyan
Write-Host "SAVE THIS PASSWORD SECURELY!" -ForegroundColor Red

# Save to file (encrypted)
$SecurePassword = ConvertTo-SecureString $DB_PASSWORD -AsPlainText -Force
$SecurePassword | ConvertFrom-SecureString | Out-File "db-password.encrypted"

# Create database
gcloud sql databases create $DB_NAME `
    --instance=$INSTANCE_NAME `
    --charset=UTF8 `
    --collation=en_US.UTF8

# Create user
gcloud sql users create $DB_USER `
    --instance=$INSTANCE_NAME `
    --password=$DB_PASSWORD

Write-Host "Database and user created successfully!" -ForegroundColor Green
```

### Set Database Flags

```powershell
# Configure PostgreSQL settings
gcloud sql instances patch $INSTANCE_NAME `
    --database-flags=`
max_connections=100,`
shared_buffers=131072,`
effective_cache_size=393216,`
maintenance_work_mem=65536,`
checkpoint_completion_target=0.9,`
wal_buffers=4096,`
default_statistics_target=100,`
random_page_cost=1.1,`
effective_io_concurrency=200,`
work_mem=1310,`
min_wal_size=1024,`
max_wal_size=4096

Write-Host "Database flags configured for optimal performance" -ForegroundColor Green
```

---

## Connection Methods

### Method 1: Cloud SQL Proxy (Recommended for Local Development)

**Download Cloud SQL Proxy:**

Windows:
```powershell
# Download
Invoke-WebRequest -Uri "https://dl.google.com/cloudsql/cloud_sql_proxy_x64.exe" -OutFile "cloud_sql_proxy.exe"

# Make executable
# (Windows doesn't need chmod)
```

Linux/Mac:
```bash
curl -o cloud_sql_proxy https://dl.google.com/cloudsql/cloud_sql_proxy.linux.amd64
chmod +x cloud_sql_proxy
```

**Run Proxy:**

```powershell
# Start proxy in background
Start-Process -FilePath ".\cloud_sql_proxy.exe" -ArgumentList "-instances=$CONNECTION_NAME=tcp:5432" -WindowStyle Hidden

# Or in foreground for debugging
.\cloud_sql_proxy.exe -instances=$CONNECTION_NAME=tcp:5432

# Connection string when using proxy
$PROXY_CONNECTION = "Host=localhost;Port=5432;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASSWORD"
```

### Method 2: Public IP (Temporary, for Migrations)

```powershell
# Get your public IP
$MY_IP = (Invoke-WebRequest -Uri "https://api.ipify.org").Content

# Authorize your IP
gcloud sql instances patch $INSTANCE_NAME --authorized-networks="$MY_IP/32"

# Get database public IP
$DB_IP = gcloud sql instances describe $INSTANCE_NAME --format="value(ipAddresses[0].ipAddress)"

# Connection string
$PUBLIC_CONNECTION = "Host=$DB_IP;Port=5432;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASSWORD;SslMode=Require"

# IMPORTANT: Remove public access after migrations
gcloud sql instances patch $INSTANCE_NAME --clear-authorized-networks
```

### Method 3: Private IP (Production, requires VPC)

```powershell
# Enable Private Service Access
gcloud services enable servicenetworking.googleapis.com

# Reserve IP range
gcloud compute addresses create google-managed-services-default `
    --global `
    --purpose=VPC_PEERING `
    --prefix-length=16 `
    --network=default

# Create private connection
gcloud services vpc-peerings connect `
    --service=servicenetworking.googleapis.com `
    --ranges=google-managed-services-default `
    --network=default

# Create instance with private IP
gcloud sql instances patch $INSTANCE_NAME --network=default --no-assign-ip
```

### Method 4: Unix Socket (Cloud Run)

This is automatically configured when using `--add-cloudsql-instances` in Cloud Run:

```powershell
# Connection string format for Cloud Run
$CLOUDSQL_CONNECTION = "Host=/cloudsql/$CONNECTION_NAME;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASSWORD"
```

---

## Database Migrations

### Install EF Core Tools

```powershell
# Install globally
dotnet tool install --global dotnet-ef

# Verify installation
dotnet ef --version
```

### Run Initial Migration

```powershell
# Using Cloud SQL Proxy (recommended)
.\cloud_sql_proxy.exe -instances=$CONNECTION_NAME=tcp:5432

# In another terminal, run migrations
$CONNECTION_STRING = "Host=localhost;Port=5432;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASSWORD"

dotnet ef database update `
    --project EmailFixer.Infrastructure `
    --startup-project EmailFixer.Api `
    --connection "$CONNECTION_STRING" `
    --verbose
```

### Verify Migration Success

```powershell
# Connect to database using psql
$env:PGPASSWORD = $DB_PASSWORD
psql -h localhost -p 5432 -U $DB_USER -d $DB_NAME

# List tables
\dt

# Check specific table
SELECT * FROM "__EFMigrationsHistory";

# Exit psql
\q
```

### Create New Migration

```powershell
# Create migration
dotnet ef migrations add MigrationName `
    --project EmailFixer.Infrastructure `
    --startup-project EmailFixer.Api

# Apply migration
dotnet ef database update `
    --project EmailFixer.Infrastructure `
    --startup-project EmailFixer.Api `
    --connection "$CONNECTION_STRING"
```

---

## Backup & Recovery

### Enable Automated Backups

```powershell
# Enable automated backups
gcloud sql instances patch $INSTANCE_NAME `
    --backup-start-time=03:00 `
    --enable-bin-log

# Set backup retention (7 days)
gcloud sql instances patch $INSTANCE_NAME --retained-backups-count=7

# Verify backup configuration
gcloud sql instances describe $INSTANCE_NAME --format="yaml(settings.backupConfiguration)"
```

### Manual Backup

```powershell
# Create on-demand backup
gcloud sql backups create --instance=$INSTANCE_NAME --description="Manual backup before deployment"

# List backups
gcloud sql backups list --instance=$INSTANCE_NAME

# Get specific backup info
gcloud sql backups describe BACKUP_ID --instance=$INSTANCE_NAME
```

### Restore from Backup

```powershell
# List available backups
$backups = gcloud sql backups list --instance=$INSTANCE_NAME --format="table(id,windowStartTime,status)"
Write-Host $backups

# Restore to same instance (this will replace current data!)
gcloud sql backups restore BACKUP_ID --backup-instance=$INSTANCE_NAME

# Restore to new instance (safer for testing)
gcloud sql instances clone $INSTANCE_NAME emailfixer-db-restored --backup-id=BACKUP_ID
```

### Export Database (pg_dump)

```powershell
# Export to Cloud Storage
$BUCKET_NAME = "emailfixer-backups"
gsutil mb gs://$BUCKET_NAME

# Export database
gcloud sql export sql $INSTANCE_NAME gs://$BUCKET_NAME/backup-$(Get-Date -Format 'yyyyMMdd-HHmmss').sql `
    --database=$DB_NAME

# Export specific tables
gcloud sql export sql $INSTANCE_NAME gs://$BUCKET_NAME/users-backup.sql `
    --database=$DB_NAME `
    --table=users,subscriptions
```

### Import Database

```powershell
# Import from Cloud Storage
gcloud sql import sql $INSTANCE_NAME gs://$BUCKET_NAME/backup.sql `
    --database=$DB_NAME
```

---

## Performance Tuning

### Monitor Performance

```powershell
# Check current performance
gcloud sql operations list --instance=$INSTANCE_NAME --limit=10

# Get performance insights
gcloud sql instances describe $INSTANCE_NAME --format="yaml(settings.insightsConfig)"
```

### Enable Query Insights

```powershell
# Enable query insights
gcloud sql instances patch $INSTANCE_NAME `
    --insights-config-query-insights-enabled `
    --insights-config-query-string-length=1024 `
    --insights-config-record-application-tags `
    --insights-config-record-client-address

# View insights in Cloud Console:
# https://console.cloud.google.com/sql/instances/$INSTANCE_NAME/query-insights
```

### Optimize Connection Pooling

Update `appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=/cloudsql/$CONNECTION_NAME;Database=emailfixer;Username=appuser;Password=***;Pooling=true;MinPoolSize=0;MaxPoolSize=20;Connection Lifetime=300;Command Timeout=30"
  }
}
```

### Scale Up Instance

```powershell
# Upgrade to larger tier
gcloud sql instances patch $INSTANCE_NAME --tier=db-custom-2-4096

# Add read replica for scaling reads
gcloud sql instances create emailfixer-db-replica `
    --master-instance-name=$INSTANCE_NAME `
    --tier=db-f1-micro `
    --region=us-east1
```

---

## Security Best Practices

### 1. Use Secret Manager

```powershell
# Store connection string in Secret Manager
$FULL_CONNECTION = "Host=/cloudsql/$CONNECTION_NAME;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASSWORD"
echo $FULL_CONNECTION | gcloud secrets create db-connection --data-file=-

# Grant access to Cloud Run service account
gcloud secrets add-iam-policy-binding db-connection `
    --member="serviceAccount:PROJECT_NUMBER-compute@developer.gserviceaccount.com" `
    --role="roles/secretmanager.secretAccessor"
```

### 2. Rotate Passwords Regularly

```powershell
# Generate new password
$NEW_PASSWORD = -join ((48..57) + (65..90) + (97..122) | Get-Random -Count 20 | ForEach-Object {[char]$_})

# Update user password
gcloud sql users set-password $DB_USER `
    --instance=$INSTANCE_NAME `
    --password=$NEW_PASSWORD

# Update secret
echo $NEW_PASSWORD | gcloud secrets versions add db-connection --data-file=-
```

### 3. Limit Network Access

```powershell
# Remove public IP (use Cloud SQL Proxy or Private IP)
gcloud sql instances patch $INSTANCE_NAME --no-assign-ip

# Or restrict to specific IPs only
gcloud sql instances patch $INSTANCE_NAME `
    --authorized-networks="203.0.113.1/32,203.0.113.2/32"
```

### 4. Enable SSL

```powershell
# Require SSL connections
gcloud sql instances patch $INSTANCE_NAME --require-ssl

# Download server CA certificate
gcloud sql ssl-certs create client-cert cert.pem --instance=$INSTANCE_NAME
gcloud sql ssl-certs describe client-cert --instance=$INSTANCE_NAME

# Update connection string
$SSL_CONNECTION = "Host=$DB_IP;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASSWORD;SslMode=Require;Trust Server Certificate=false;Server Certificate=cert.pem"
```

### 5. Audit Logging

```powershell
# Enable audit logging
gcloud sql instances patch $INSTANCE_NAME `
    --database-flags=cloudsql.enable_pgaudit=on,pgaudit.log=all

# View audit logs in Cloud Console:
# https://console.cloud.google.com/logs
```

---

## Troubleshooting

### Issue: Cannot connect to database

**Check instance status:**
```powershell
gcloud sql instances describe $INSTANCE_NAME --format="yaml(state)"
```

**Check authorized networks:**
```powershell
gcloud sql instances describe $INSTANCE_NAME --format="yaml(settings.ipConfiguration.authorizedNetworks)"
```

**Test connection:**
```powershell
# Using psql
psql "postgresql://$DB_USER:$DB_PASSWORD@$DB_IP:5432/$DB_NAME?sslmode=require"

# Using Cloud SQL Proxy
.\cloud_sql_proxy.exe -instances=$CONNECTION_NAME=tcp:5432
psql -h localhost -p 5432 -U $DB_USER -d $DB_NAME
```

### Issue: Migrations fail

**Check EF Core tools version:**
```powershell
dotnet ef --version
# Should be 8.0.x or higher
```

**Run with verbose logging:**
```powershell
dotnet ef database update --verbose --project EmailFixer.Infrastructure --startup-project EmailFixer.Api
```

**Check migration history:**
```powershell
psql -h localhost -p 5432 -U $DB_USER -d $DB_NAME -c "SELECT * FROM \"__EFMigrationsHistory\";"
```

### Issue: Performance problems

**Check connection count:**
```powershell
psql -h localhost -p 5432 -U $DB_USER -d $DB_NAME -c "SELECT count(*) FROM pg_stat_activity;"
```

**Check slow queries:**
```powershell
psql -h localhost -p 5432 -U $DB_USER -d $DB_NAME -c "SELECT query, calls, total_time, mean_time FROM pg_stat_statements ORDER BY mean_time DESC LIMIT 10;"
```

**Enable pg_stat_statements:**
```powershell
gcloud sql instances patch $INSTANCE_NAME --database-flags=shared_preload_libraries=pg_stat_statements
```

### Issue: Out of storage

**Check storage usage:**
```powershell
gcloud sql instances describe $INSTANCE_NAME --format="yaml(settings.dataDiskSizeGb,currentDiskSize)"
```

**Increase storage:**
```powershell
gcloud sql instances patch $INSTANCE_NAME --storage-size=20GB
```

### Issue: High costs

**Downgrade tier:**
```powershell
gcloud sql instances patch $INSTANCE_NAME --tier=db-f1-micro
```

**Stop instance (development only):**
```powershell
gcloud sql instances patch $INSTANCE_NAME --activation-policy=NEVER
# Restart: --activation-policy=ALWAYS
```

**Delete unused backups:**
```powershell
gcloud sql backups list --instance=$INSTANCE_NAME
gcloud sql backups delete BACKUP_ID --instance=$INSTANCE_NAME
```

---

## Monitoring Queries

### Connection Monitoring

```sql
-- Active connections
SELECT
    datname as database,
    usename as username,
    application_name,
    client_addr,
    state,
    query_start,
    now() - query_start as duration
FROM pg_stat_activity
WHERE state != 'idle'
ORDER BY duration DESC;

-- Connection count by database
SELECT datname, count(*) FROM pg_stat_activity GROUP BY datname;
```

### Performance Monitoring

```sql
-- Table sizes
SELECT
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size
FROM pg_tables
WHERE schemaname NOT IN ('pg_catalog', 'information_schema')
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;

-- Index usage
SELECT
    schemaname,
    tablename,
    indexname,
    idx_scan as index_scans,
    idx_tup_read as tuples_read,
    idx_tup_fetch as tuples_fetched
FROM pg_stat_user_indexes
ORDER BY idx_scan DESC;
```

---

## References

- [Cloud SQL for PostgreSQL Documentation](https://cloud.google.com/sql/docs/postgres)
- [Cloud SQL Proxy](https://cloud.google.com/sql/docs/postgres/sql-proxy)
- [PostgreSQL Performance Tuning](https://wiki.postgresql.org/wiki/Performance_Optimization)
- [EF Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)

---

**Last Updated:** 2025-11-09
**Version:** 1.0.0

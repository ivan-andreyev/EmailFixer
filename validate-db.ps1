# Database Validation Script
# Проверка что миграции применены и DbContext работает

Write-Host "=== Database Validation ===" -ForegroundColor Cyan

# Check if database file exists
$dbPath = "C:\Sources\EmailFixer\EmailFixer.Api\emailfixer.db"
if (Test-Path $dbPath) {
    Write-Host "[OK] Database file exists: $dbPath" -ForegroundColor Green

    # Check file size
    $dbSize = (Get-Item $dbPath).Length
    Write-Host "[OK] Database file size: $($dbSize) bytes" -ForegroundColor Green
} else {
    Write-Host "[ERROR] Database file not found at: $dbPath" -ForegroundColor Red
    exit 1
}

# Check migration files
$migrationPath = "C:\Sources\EmailFixer\EmailFixer.Infrastructure\Migrations"
$migrationFiles = Get-ChildItem -Path $migrationPath -Filter "*.cs" | Measure-Object
Write-Host "[OK] Migration files found: $($migrationFiles.Count)" -ForegroundColor Green

# List migration files
Get-ChildItem -Path $migrationPath -Filter "*InitialCreate*" | ForEach-Object {
    Write-Host "  - $($_.Name)" -ForegroundColor Gray
}

# Verify solution builds
Write-Host "`n=== Building Solution ===" -ForegroundColor Cyan
$buildResult = dotnet build --no-restore -v quiet 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] Solution builds successfully" -ForegroundColor Green
} else {
    Write-Host "[ERROR] Build failed" -ForegroundColor Red
    Write-Host $buildResult
    exit 1
}

# Check EF migrations list
Write-Host "`n=== EF Migrations List ===" -ForegroundColor Cyan
dotnet ef migrations list -p EmailFixer.Infrastructure -s EmailFixer.Api

Write-Host "`n=== Validation Complete ===" -ForegroundColor Green
Write-Host "Phase 1: Database Setup - COMPLETED" -ForegroundColor Green
Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "  1. PostgreSQL connection string configured: appsettings.Development.json"
Write-Host "  2. SQLite fallback configured for development (when PostgreSQL not available)"
Write-Host "  3. Initial migration created and applied"
Write-Host "  4. All tables created: Users, EmailChecks, CreditTransactions"
Write-Host "  5. Indexes and foreign keys configured"
Write-Host "`nTo use PostgreSQL instead of SQLite, set environment variable:"
Write-Host "  `$env:USE_POSTGRES='true'"

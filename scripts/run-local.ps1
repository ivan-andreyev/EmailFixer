# EmailFixer - Local Development Setup Script
# Запускает API и Client локально (с SQLite, без Docker)

param(
    [switch]$MigrateDb = $false,
    [switch]$ApiOnly = $false,
    [switch]$ClientOnly = $false
)

# Цвета для вывода
$InfoColor = "Cyan"
$SuccessColor = "Green"
$ErrorColor = "Red"
$WarningColor = "Yellow"

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ️  $Message" -ForegroundColor $InfoColor
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor $SuccessColor
}

function Write-Error {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor $ErrorColor
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠️  $Message" -ForegroundColor $WarningColor
}

# Проверяем, что мы в корне проекта
$projectRoot = Get-Location
$slnFile = Join-Path $projectRoot "EmailFixer.sln"

if (-not (Test-Path $slnFile)) {
    Write-Error "EmailFixer.sln не найден в $projectRoot"
    Write-Info "Пожалуйста, запустите скрипт из корневой директории проекта"
    exit 1
}

Write-Host ""
Write-Host "════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  EmailFixer - Local Development Setup" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Проверяем .NET SDK
Write-Info "Проверяем .NET SDK..."
try {
    $dotnetVersion = dotnet --version
    Write-Success "Найден .NET: $dotnetVersion"
} catch {
    Write-Error ".NET SDK не установлен"
    exit 1
}

# Выполняем миграции БД, если нужно
if ($MigrateDb) {
    Write-Host ""
    Write-Info "Применяем миграции базы данных..."
    Write-Warning "Это используется SQLite (автоматически создается при первом запуске)"

    Push-Location "$projectRoot\EmailFixer.Infrastructure"
    dotnet ef database update -s ../EmailFixer.Api -v
    Pop-Location

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Ошибка при применении миграций"
        exit 1
    }
    Write-Success "Миграции применены"
}

Write-Host ""

# Функция для запуска приложения в новом окне
function Start-AppWindow {
    param(
        [string]$ProjectPath,
        [string]$ProjectName
    )

    Write-Info "Запускаю $ProjectName..."

    $cmd = "cd `"$ProjectPath`"; dotnet run"

    # Запускаем в новом PowerShell окне
    Start-Process powershell -ArgumentList "-NoExit", "-Command", $cmd -WindowStyle Normal
}

# Запускаем API
if (-not $ClientOnly) {
    Write-Info "Запуск API..."
    Write-Info "  URL: http://localhost:5165"
    Write-Info "  Swagger: http://localhost:5165/swagger"
    Write-Info "  Health: http://localhost:5165/health"
    Write-Info ""

    $apiPath = Join-Path $projectRoot "EmailFixer.Api"
    Start-AppWindow -ProjectPath $apiPath -ProjectName "EmailFixer.Api"

    # Даем время API на запуск
    Start-Sleep -Seconds 3
}

# Запускаем Client
if (-not $ApiOnly) {
    Write-Info "Запуск Blazor Client..."
    Write-Info "  URL: http://localhost:5000"
    Write-Info ""

    $clientPath = Join-Path $projectRoot "EmailFixer.Client"
    Start-AppWindow -ProjectPath $clientPath -ProjectName "EmailFixer.Client"
}

Write-Host ""
Write-Success "Оба приложения запущены!"
Write-Info "Откройте браузер и перейдите на http://localhost:5000"
Write-Host ""
Write-Warning "Нажмите Ctrl+C в этом окне для остановки (приложения продолжат работать в своих окнах)"
Write-Host ""

# Ждем пока пользователь прервет выполнение
Wait-Event -Timeout 3600

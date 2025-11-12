# EmailFixer - Docker Compose Setup Script
# Запускает проект в Docker с PostgreSQL, API и Blazor клиентом

param(
    [switch]$BuildNoCaсhe = $false,
    [switch]$Down = $false
)

# Цвета для вывода
$InfoColor = "Cyan"
$SuccessColor = "Green"
$ErrorColor = "Red"

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

# Проверяем, что мы в корне проекта
$projectRoot = Get-Location
$dockerFile = Join-Path $projectRoot "docker-compose.yml"

if (-not (Test-Path $dockerFile)) {
    Write-Error "docker-compose.yml не найден в $projectRoot"
    Write-Info "Пожалуйста, запустите скрипт из корневой директории проекта"
    exit 1
}

Write-Host ""
Write-Host "════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  EmailFixer - Docker Compose Setup" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

if ($Down) {
    Write-Info "Останавливаем контейнеры..."
    docker-compose down
    Write-Success "Контейнеры остановлены"
    exit 0
}

# Проверяем Docker
Write-Info "Проверяем Docker..."
try {
    $dockerVersion = docker --version
    Write-Success "Docker найден: $dockerVersion"
} catch {
    Write-Error "Docker не установлен или не в PATH"
    exit 1
}

# Проверяем Docker Compose
try {
    $composeVersion = docker-compose --version
    Write-Success "Docker Compose найден: $composeVersion"
} catch {
    Write-Error "Docker Compose не установлен"
    exit 1
}

Write-Host ""
Write-Info "Сборка образов..."

if ($BuildNoCache) {
    docker-compose build --no-cache
} else {
    docker-compose build
}

if ($LASTEXITCODE -ne 0) {
    Write-Error "Ошибка при сборке образов"
    exit 1
}

Write-Success "Образы собраны"

Write-Host ""
Write-Info "Запуск контейнеров..."
docker-compose up

# Обработка Ctrl+C
trap {
    Write-Host ""
    Write-Info "Остановка контейнеров..."
    docker-compose down
    Write-Success "Контейнеры остановлены"
    exit 0
}

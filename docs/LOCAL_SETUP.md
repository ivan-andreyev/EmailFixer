# EmailFixer - Local Development Setup Guide

Полное руководство по запуску EmailFixer локально для разработки.

## 📋 Содержание

- [Требования](#требования)
- [Быстрый старт](#быстрый-старт)
- [Опция 1: Docker Compose (Рекомендуется)](#опция-1-docker-compose-рекомендуется)
- [Опция 2: Локальный запуск (SQLite)](#опция-2-локальный-запуск-sqlite)
- [Проверка работоспособности](#проверка-работоспособности)
- [Команды разработки](#команды-разработки)
- [Решение проблем](#решение-проблем)

---

## Требования

### Обязательно
- **.NET 8 SDK** (версия 8.0.411+)
  - Установка: https://dotnet.microsoft.com/download/dotnet/8.0
  - Проверка: `dotnet --version`

- **Git** (для версионирования)
  - Проверка: `git --version`

### Для Docker Compose
- **Docker Desktop** (последняя версия)
  - Установка: https://www.docker.com/products/docker-desktop
  - Проверка: `docker --version` и `docker-compose --version`

### Опционально
- **PostgreSQL 16** (если используете локальную БД вместо SQLite)
- **Visual Studio Code** или **Visual Studio 2022** (IDE)
- **Postman** или **curl** (для тестирования API)

---

## Быстрый старт

### 1️⃣ Клонирование репозитория

```powershell
git clone <repository-url>
cd EmailFixer
```

### 2️⃣ Выберите способ запуска

#### Способ A: Docker Compose (Все в контейнерах)

```powershell
# Автоматическое выполнение скрипта
.\scripts\run-docker.ps1

# Или вручную
docker-compose up --build
```

**Результат:**
- API: http://localhost:5165
- Swagger: http://localhost:5165/swagger
- Client: http://localhost
- PostgreSQL: localhost:5432

#### Способ B: Локальный запуск (Разработка)

```powershell
# Первый раз - применить миграции
.\scripts\run-local.ps1 -MigrateDb

# Последующие запуски
.\scripts\run-local.ps1
```

**Результат:**
- API: http://localhost:5165
- Swagger: http://localhost:5165/swagger
- Client: http://localhost:5000
- БД: SQLite (автоматически создается)

---

## Опция 1: Docker Compose (Рекомендуется)

### Преимущества
✅ Идентично production окружению
✅ PostgreSQL вместо SQLite
✅ Изолированное окружение
✅ Легко reset-ить БД

### Инструкции

#### Шаг 1: Проверка Docker

```powershell
docker --version
# Expected: Docker version 20.10+

docker-compose --version
# Expected: Docker Compose version 2.0+
```

#### Шаг 2: Сборка и запуск

```powershell
cd C:\Sources\EmailFixer

# Сборка образов (первый раз)
docker-compose build

# Запуск всех сервисов
docker-compose up

# Или одной командой (с пересборкой):
docker-compose up --build
```

#### Шаг 3: Проверка статуса

```powershell
# В отдельном PowerShell окне
docker-compose ps

# Результат должен быть:
# NAME                COMMAND                  STATUS          PORTS
# emailfixer-postgres postgres                 Up (healthy)    5432/tcp
# emailfixer-api      dotnet EmailFixer.Api... Up (healthy)    5165->8080/tcp
# emailfixer-client   /docker-entrypoint.sh    Up (healthy)    80->80/tcp
```

#### Шаг 4: Откройте приложение

- **Client:** http://localhost
- **API Swagger:** http://localhost:5165/swagger
- **Health Check:** http://localhost:5165/health

### Полезные команды Docker

```powershell
# Просмотр логов
docker-compose logs -f api          # Логи API
docker-compose logs -f client       # Логи Client
docker-compose logs -f postgres     # Логи БД

# Остановка контейнеров
docker-compose down

# Полная очистка (включая БД)
docker-compose down -v

# Пересборка без кеша
docker-compose build --no-cache

# Запуск одного сервиса
docker-compose up api

# Выполнение команды в контейнере
docker-compose exec api dotnet ef migrations list
```

---

## Опция 2: Локальный запуск (SQLite)

### Преимущества
✅ Нет Docker
✅ Быстрый старт
✅ Удобная разработка
✅ SQLite автоматически создается

### Требования
- .NET 8 SDK
- Никакого Docker

### Инструкции

#### Шаг 1: Применение миграций БД (первый раз)

```powershell
cd C:\Sources\EmailFixer

# Применить все миграции (создает emailfixer.db)
dotnet ef database update `
  -p EmailFixer.Infrastructure `
  -s EmailFixer.Api
```

#### Шаг 2: Запуск API (Терминал 1)

```powershell
cd EmailFixer.Api
dotnet run

# Expected output:
# info: Microsoft.Hosting.Lifetime[0]
#       Now listening on: http://localhost:5165
#       Now listening on: https://localhost:5166
```

#### Шаг 3: Запуск Client (Терминал 2)

```powershell
cd EmailFixer.Client
dotnet run

# Expected output:
# info: Microsoft.Hosting.Lifetime[0]
#       Now listening on: http://localhost:5000
```

#### Шаг 4: Откройте приложение

- **Client:** http://localhost:5000
- **API Swagger:** http://localhost:5165/swagger

### Использование скрипта

```powershell
# Первый запуск (с миграциями)
.\scripts\run-local.ps1 -MigrateDb

# Последующие запуски
.\scripts\run-local.ps1

# Только API
.\scripts\run-local.ps1 -ApiOnly

# Только Client
.\scripts\run-local.ps1 -ClientOnly
```

---

## Проверка работоспособности

### API Health Check

```powershell
# Проверка здоровья API
curl http://localhost:5165/health

# Expected response:
# {"status":"healthy","timestamp":"2025-11-12T10:30:00Z"}
```

### Swagger UI

Откройте в браузере: http://localhost:5165/swagger

Попробуйте endpoint:
1. Нажмите "Try it out" на любом endpoint
2. Нажмите "Execute"
3. Проверьте ответ

### Валидация Email

```powershell
# Валидация одного email (требуется userId)
curl -X POST http://localhost:5165/api/email/validate `
  -H "Content-Type: application/json" `
  -d '{
    "userId": "00000000-0000-0000-0000-000000000000",
    "email": "test@gmail.com"
  }'

# Ожидаемый ответ:
# {
#   "email": "test@gmail.com",
#   "isValid": true,
#   "suggestion": null,
#   "message": "Valid email"
# }
```

### Проверка БД

```powershell
# SQLite (локальный запуск)
# Файл: C:\Sources\EmailFixer\emailfixer.db
# Просмотр: используйте DB Browser for SQLite или DBeaver

# PostgreSQL (Docker)
docker-compose exec postgres psql -U postgres -d emailfixer -c "SELECT * FROM \"Users\";"
```

---

## Команды разработки

### Работа с БД

```powershell
# Посмотреть все миграции
dotnet ef migrations list `
  -p EmailFixer.Infrastructure `
  -s EmailFixer.Api

# Создать новую миграцию
dotnet ef migrations add MigrationName `
  -p EmailFixer.Infrastructure `
  -s EmailFixer.Api

# Откатить последнюю миграцию
dotnet ef database update PreviousMigrationName `
  -p EmailFixer.Infrastructure `
  -s EmailFixer.Api

# Генерировать SQL скрипт (для production)
dotnet ef migrations script `
  -p EmailFixer.Infrastructure `
  -s EmailFixer.Api `
  -o migration.sql
```

### Сборка проекта

```powershell
# Чистая пересборка
dotnet clean
dotnet restore
dotnet build

# Release сборка
dotnet build -c Release

# Watch режим (автоперезагрузка)
cd EmailFixer.Api
dotnet watch run

# В другом окне
cd EmailFixer.Client
dotnet watch run
```

### Тестирование

```powershell
# Все тесты
dotnet test

# Только API тесты
dotnet test EmailFixer.Tests

# Только Blazor тесты
dotnet test EmailFixer.Client.Tests

# С выводом логов
dotnet test --verbosity normal

# Покрытие кода
dotnet test /p:CollectCoverage=true
```

### Очистка

```powershell
# Удаление артефактов сборки
dotnet clean

# Удаление кеша NuGet
dotnet nuget locals all --clear

# Удаление SQLite БД (для сброса)
Remove-Item emailfixer.db

# Для Docker
docker-compose down -v  # Удаляет контейнеры и volumes
```

---

## Решение проблем

### Problem: "Port already in use"

**Ошибка:**
```
error: Failed to bind to address 127.0.0.1:5165
```

**Решение:**

```powershell
# Найти процесс на порту (PowerShell)
netstat -ano | findstr :5165

# Убить процесс (по PID)
taskkill /PID 12345 /F

# Или использовать другой порт
$env:ASPNETCORE_URLS="http://+:5167"
dotnet run
```

### Problem: "Database connection failed"

**Ошибка:**
```
NpgsqlException: unable to connect to server
```

**Решение:**

```powershell
# Для Docker
# Проверьте, что postgres здоров
docker-compose ps

# Перезагрузите контейнеры
docker-compose down
docker-compose up --build

# Для локального PostgreSQL
# Убедитесь, что PostgreSQL запущен
pg_isready -h localhost -p 5432
```

### Problem: "Migrations not applied"

**Решение:**

```powershell
# Повторно применить миграции
dotnet ef database update `
  -p EmailFixer.Infrastructure `
  -s EmailFixer.Api

# Откатить и пересоздать
dotnet ef database drop
dotnet ef database update
```

### Problem: "Blazor app not loading"

**Ошибка:**
```
Failed to fetch main.js or blazor.webassembly.js
```

**Решение:**

```powershell
# Очистить кеш браузера (Ctrl+Shift+Delete)
# Или используйте Ctrl+F5 для hard refresh

# Пересоберите client
cd EmailFixer.Client
dotnet clean
dotnet build

# Проверьте, что API доступна
curl http://localhost:5165/health
```

### Problem: "CORS error"

**Ошибка:**
```
Access to XMLHttpRequest blocked by CORS policy
```

**Решение:**

1. Убедитесь, что API запущен
2. Проверьте URL в `appsettings.json` (client):
   ```json
   {
     "ApiBaseUrl": "http://localhost:5165/"
   }
   ```
3. Перезагрузите браузер (Ctrl+F5)

### Problem: "Docker build fails"

**Решение:**

```powershell
# Полная очистка Docker
docker system prune -a

# Пересборка без кеша
docker-compose build --no-cache

# Проверка Dockerfile
docker build -f EmailFixer.Api/Dockerfile .
```

---

## Configuration Files

### API Configuration

**`EmailFixer.Api/appsettings.Development.json`**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=emailfixer;Username=postgres;Password=DevPassword123!"
  },
  "Jwt": {
    "Secret": "dev-secret-key-must-be-at-least-32-characters-long",
    "Issuer": "emailfixer-api-dev",
    "ExpirationMinutes": 1440
  },
  "GoogleOAuth": {
    "ClientId": "YOUR_DEV_GOOGLE_CLIENT_ID",
    "ClientSecret": "YOUR_DEV_GOOGLE_CLIENT_SECRET",
    "RedirectUri": "http://localhost:5000/auth-callback"
  }
}
```

### Client Configuration

**`EmailFixer.Client/wwwroot/appsettings.json`**
```json
{
  "ApiBaseUrl": "http://localhost:5165/",
  "GoogleOAuth": {
    "ClientId": "YOUR_DEV_GOOGLE_CLIENT_ID",
    "RedirectUri": "http://localhost:5000/auth-callback"
  }
}
```

---

## Быстрые ссылки

| Что | Где | Порт |
|-----|-----|------|
| Client | http://localhost:5000 | 5000 (dev), 80 (docker) |
| API | http://localhost:5165 | 5165 (dev), 8080 (docker) |
| Swagger | http://localhost:5165/swagger | 5165 |
| PostgreSQL | localhost:5432 | 5432 |
| Health | http://localhost:5165/health | 5165 |

---

## Следующие шаги

1. ✅ Запустите приложение (выберите Опцию 1 или 2)
2. ✅ Откройте http://localhost:5000 (или :5165/swagger)
3. ✅ Попробуйте валидировать email
4. ✅ Изучите API в Swagger UI
5. ✅ Начните разработку!

---

**Вопросы?** Смотрите раздел [Решение проблем](#решение-проблем) или документацию проекта в `docs/`

**Last Updated:** 2025-11-12

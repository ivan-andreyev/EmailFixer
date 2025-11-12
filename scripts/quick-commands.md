# EmailFixer - Quick Commands Reference

Шпаргалка с часто используемыми командами для разработки.

## 🚀 Быстрый запуск

### Docker (Все в контейнерах)
```powershell
.\scripts\run-docker.ps1           # Полный запуск с пересборкой
docker-compose up                  # Быстрый запуск (без пересборки)
docker-compose down                # Остановка
docker-compose down -v             # Остановка + удаление БД
```

### Локальный запуск (SQLite)
```powershell
.\scripts\run-local.ps1 -MigrateDb # Первый запуск с миграциями
.\scripts\run-local.ps1            # Последующие запуски
.\scripts\run-local.ps1 -ApiOnly   # Только API
.\scripts\run-local.ps1 -ClientOnly # Только Client
```

---

## 🛠️ Разработка (Локальный запуск)

### Запуск с hot reload

```powershell
# Terminal 1: API с автоперезагрузкой
cd EmailFixer.Api
dotnet watch run

# Terminal 2: Client с автоперезагрузкой
cd EmailFixer.Client
dotnet watch run
```

### Сборка проекта

```powershell
# Базовая сборка
dotnet build

# Release сборка
dotnet build -c Release

# Чистая сборка
dotnet clean && dotnet restore && dotnet build
```

### Тестирование

```powershell
# Все тесты
dotnet test

# Только API тесты
dotnet test EmailFixer.Tests

# С выводом логов
dotnet test --verbosity normal

# Конкретный тест
dotnet test --filter "TestClass.TestMethod"
```

---

## 📦 Работа с БД

### Миграции

```powershell
# Показать список миграций
dotnet ef migrations list -p EmailFixer.Infrastructure -s EmailFixer.Api

# Создать новую миграцию
dotnet ef migrations add MigrationName -p EmailFixer.Infrastructure -s EmailFixer.Api

# Откатить на предыдущую миграцию
dotnet ef database update PreviousMigrationName -p EmailFixer.Infrastructure -s EmailFixer.Api

# Удалить последнюю миграцию (если не применена)
dotnet ef migrations remove -p EmailFixer.Infrastructure -s EmailFixer.Api

# Сгенерировать SQL скрипт
dotnet ef migrations script -p EmailFixer.Infrastructure -s EmailFixer.Api -o migration.sql
```

### SQLite (локальная разработка)

```powershell
# Применить миграции
dotnet ef database update -p EmailFixer.Infrastructure -s EmailFixer.Api

# Удалить БД (сброс)
rm emailfixer.db

# Просмотр БД
# Используйте: DB Browser for SQLite (https://sqlitebrowser.org/)
```

### PostgreSQL (Docker)

```powershell
# Подключиться к БД
docker-compose exec postgres psql -U postgres -d emailfixer

# Посмотреть таблицы
\dt

# Посмотреть данные
SELECT * FROM "Users";

# Выход
\q
```

---

## 🔍 Отладка

### API

```powershell
# Здоровье API
curl http://localhost:5165/health

# Swagger UI
# Откройте: http://localhost:5165/swagger
```

### Логи

```powershell
# Docker: Логи API
docker-compose logs -f api

# Docker: Логи Client
docker-compose logs -f client

# Docker: Логи PostgreSQL
docker-compose logs -f postgres
```

### Сетевое подключение

```powershell
# Проверить, какой процесс использует порт
netstat -ano | findstr :5165

# Завершить процесс (по PID)
taskkill /PID 12345 /F

# Проверка PostgreSQL доступна
pg_isready -h localhost -p 5432

# Тест API
curl -v http://localhost:5165/health
```

---

## 🐳 Docker команды

### Основные операции

```powershell
# Сборка
docker-compose build

# Сборка без кеша
docker-compose build --no-cache

# Запуск
docker-compose up

# Запуск в фоне
docker-compose up -d

# Остановка
docker-compose down

# Остановка + удаление volume
docker-compose down -v

# Статус контейнеров
docker-compose ps

# Логи
docker-compose logs -f [service]  # api, client, postgres
```

### Выполнение команд в контейнере

```powershell
# Миграции в Docker
docker-compose exec api dotnet ef database update -p EmailFixer.Infrastructure

# Тесты в Docker
docker-compose exec api dotnet test

# Bash в контейнере
docker-compose exec api sh
```

### Очистка

```powershell
# Удалить неиспользуемые контейнеры
docker container prune

# Удалить неиспользуемые образы
docker image prune

# Полная очистка
docker system prune -a
```

---

## 📝 Code Style & Formatting

```powershell
# Форматирование кода (если настроено)
dotnet format

# Проверка стиля
dotnet format --verify-no-changes

# Анализ кода (если настроено)
dotnet analyzers
```

---

## 🚢 Deployment (продвинутое)

### Локальная сборка Docker образов

```powershell
# Сборка API образа
docker build -f EmailFixer.Api/Dockerfile -t emailfixer-api:latest .

# Сборка Client образа
docker build -f EmailFixer.Client/Dockerfile -t emailfixer-client:latest .

# Запуск образа
docker run -p 5165:8080 emailfixer-api:latest
```

### Google Cloud (Production)

```powershell
# Развертывание API
gcloud run deploy emailfixer-api `
  --source . `
  --region us-central1

# Развертывание Client
gcloud run deploy emailfixer-client `
  --source . `
  --region us-central1
```

---

## 📊 Полезные ссылки

| Ресурс | URL |
|--------|-----|
| **Документация** | `./docs/LOCAL_SETUP.md` |
| **Project README** | `./README.md` |
| **API Swagger** | http://localhost:5165/swagger |
| **Client** | http://localhost:5000 |
| **.NET 8** | https://dotnet.microsoft.com/download/dotnet/8.0 |
| **Blazor Docs** | https://docs.microsoft.com/aspnet/core/blazor |
| **EF Core** | https://docs.microsoft.com/ef/core/ |

---

## 🎯 Типичный workflow разработки

### День 1: Первоначальная настройка

```powershell
# 1. Клонировать репозиторий
git clone <url>
cd EmailFixer

# 2. Запустить через Docker или локально
.\scripts\run-docker.ps1
# или
.\scripts\run-local.ps1 -MigrateDb

# 3. Проверить работу
# Открыть http://localhost:5000 (или :80 для Docker)
# Открыть http://localhost:5165/swagger
```

### Каждый день: Разработка

```powershell
# 1. Запустить watch режим
cd EmailFixer.Api
dotnet watch run

# 2. В новом окне - запустить Client
cd EmailFixer.Client
dotnet watch run

# 3. Вносить изменения - они автоматически перезагружаются

# 4. Перед коммитом
dotnet test
git add .
git commit -m "Your message"
git push
```

### При изменении БД

```powershell
# 1. Отредактировать Entity в Infrastructure/Data/Entities

# 2. Создать миграцию
dotnet ef migrations add DescriptionOfChange `
  -p EmailFixer.Infrastructure `
  -s EmailFixer.Api

# 3. Применить локально
dotnet ef database update

# 4. Протестировать

# 5. Закоммитить миграцию
git add .
git commit -m "Add migration: DescriptionOfChange"
```

---

## ⚠️ Решение частых проблем

### Порт занят
```powershell
netstat -ano | findstr :5165
taskkill /PID [PID] /F
```

### CORS ошибка
```json
// EmailFixer.Client/wwwroot/appsettings.json
{
  "ApiBaseUrl": "http://localhost:5165/"
}
```

### БД не работает
```powershell
docker-compose down -v
docker-compose up --build
```

### Миграции не применились
```powershell
dotnet ef database update -p EmailFixer.Infrastructure -s EmailFixer.Api -v
```

---

**Updated:** 2025-11-12

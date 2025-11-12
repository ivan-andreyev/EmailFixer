# 🚀 EmailFixer - Development Quick Start

Быстрый старт для локальной разработки проекта EmailFixer.

## ⚡ Начните прямо сейчас (30 секунд)

### Вариант A: Docker (Рекомендуется)

```powershell
cd C:\Sources\EmailFixer
.\scripts\run-docker.ps1
```

**Готово!** Откройте:
- 🌐 Client: http://localhost
- 📚 API Docs: http://localhost:5165/swagger
- ✅ Health: http://localhost:5165/health

### Вариант B: Локально (SQLite)

```powershell
cd C:\Sources\EmailFixer
.\scripts\run-local.ps1 -MigrateDb
```

**Готово!** Откройте:
- 🌐 Client: http://localhost:5000
- 📚 API Docs: http://localhost:5165/swagger

---

## 📁 Что создано для вас

### 🎯 Скрипты запуска
- `scripts/run-docker.ps1` - Полный запуск в Docker
- `scripts/run-local.ps1` - Запуск локально с SQLite

### 📖 Документация
- `docs/LOCAL_SETUP.md` - **Полное руководство** (все детали)
- `scripts/quick-commands.md` - Шпаргалка с командами
- `docker-compose.dev.yml` - Расширенная конфигурация Docker

### ⚙️ Конфигурация
- `EmailFixer.Client/wwwroot/appsettings.json` - обновлена для локального запуска
- `EmailFixer.Api/appsettings.Development.json` - уже готов к использованию

---

## 🎓 Архитектура проекта

```
EmailFixer/
├── EmailFixer.Api/           ← REST API (Port 5165)
│   ├── Controllers/          Email validation, User, Payment endpoints
│   ├── Program.cs           Dependency injection setup
│   └── Dockerfile           Multi-stage build
│
├── EmailFixer.Client/        ← Blazor WASM (Port 5000 или 80)
│   ├── Pages/               Routed pages
│   ├── Components/          Blazor components
│   ├── Services/            API clients & authentication
│   └── Dockerfile           Nginx + static hosting
│
├── EmailFixer.Infrastructure/ ← Data access & services
│   ├── Data/
│   │   ├── EmailFixerDbContext.cs
│   │   ├── Entities/       User, EmailCheck, CreditTransaction
│   │   └── Migrations/     Database schema versions
│   └── Services/           Stripe, OAuth handlers
│
├── EmailFixer.Core/          ← Business logic
│   └── Validators/          Email validation algorithms
│
├── EmailFixer.Shared/        ← Shared DTOs & Contracts
├── EmailFixer.Tests/         ← API Unit/Integration tests
├── EmailFixer.Client.Tests/  ← Blazor Component tests
│
├── docker-compose.yml        ← Production-like setup
├── docker-compose.dev.yml    ← Dev setup with pgAdmin
├── nginx.conf               ← Blazor WASM static serving
├── global.json              ← .NET 8.0.411 SDK spec
└── docs/
    ├── LOCAL_SETUP.md       ← Full documentation
    └── PLANS/               ← Project plans & decisions
```

---

## 🔧 Технологический стек

| Слой | Технология | Версия |
|------|-----------|--------|
| **Frontend** | Blazor WebAssembly | 8.0.20 |
| **Backend** | ASP.NET Core | 8.0.11 |
| **ORM** | Entity Framework Core | 8.0.11 |
| **Database** | PostgreSQL / SQLite | 16 / Latest |
| **Payment** | Stripe | 46.9.0 |
| **Auth** | JWT + Google OAuth | 8.0-8.1 |
| **Validation** | FluentValidation | 11.3.1 |
| **Docs** | Swagger/OpenAPI | 6.4.6 |
| **Container** | Docker | Alpine-based |

---

## 🛠️ Основные команды

### 🐳 Docker

```powershell
# Полный запуск (пересборка)
docker-compose up --build

# Быстрый запуск
docker-compose up

# Остановка
docker-compose down

# Остановка + удаление БД
docker-compose down -v

# Логи API
docker-compose logs -f api
```

### 🔨 Локальная разработка

```powershell
# API с hot reload
cd EmailFixer.Api
dotnet watch run

# Client с hot reload (в другом окне)
cd EmailFixer.Client
dotnet watch run

# Тесты
dotnet test

# Сборка
dotnet build
```

### 📦 База данных

```powershell
# Применить миграции
dotnet ef database update -p EmailFixer.Infrastructure -s EmailFixer.Api

# Создать новую миграцию
dotnet ef migrations add MigrationName -p EmailFixer.Infrastructure -s EmailFixer.Api

# Посмотреть все миграции
dotnet ef migrations list -p EmailFixer.Infrastructure -s EmailFixer.Api
```

---

## 🌐 Доступные endpoints

### 🌍 Web UI
| Что | URL | Где запущено |
|-----|-----|-------------|
| **Client** | http://localhost:5000 | Локально |
| **Client** | http://localhost | Docker |
| **API Docs** | http://localhost:5165/swagger | Both |
| **Health** | http://localhost:5165/health | Both |

### 🗄️ Базы данных
| БД | Host | Port | Пользователь |
|----|----|------|---------|
| **PostgreSQL** | localhost | 5432 | postgres / postgres |
| **SQLite** | emailfixer.db | - | автоматический |
| **pgAdmin** | localhost:5050 | 5050 | admin@emailfixer.local / admin |

### 📡 API Endpoints
```
POST   /api/email/validate              - Валидировать один email
POST   /api/email/validate-batch        - Валидировать пакет (до 100)
GET    /api/users/{id}                  - Получить пользователя
POST   /api/users                       - Создать пользователя
GET    /api/users/{id}/credits          - Баланс кредитов
POST   /api/payment/checkout            - Создать Stripe чекаут
GET    /api/payment/plans               - Пакеты кредитов
GET    /health                          - Health check
```

---

## 📚 Документация

### Для детального изучения
👉 **[docs/LOCAL_SETUP.md](docs/LOCAL_SETUP.md)** - Полное руководство с:
- Детальные инструкции по установке
- Решение проблем
- Конфигурация переменных окружения
- Примеры использования API
- Docker команды

### Для быстрого поиска команд
👉 **[scripts/quick-commands.md](scripts/quick-commands.md)** - Шпаргалка:
- Быстрые команды Docker
- Миграции БД
- Тестирование
- Отладка

### Для архитектурных вопросов
👉 **[CLAUDE.md](CLAUDE.md)** - Документация проекта:
- Структура проекта
- Ключевые компоненты
- Развертывание на GCP
- Безопасность

---

## ⚡ Типичный workflow

### День 1: Первоначальная настройка

```powershell
# 1️⃣ Клонировать и перейти
git clone <repo>
cd EmailFixer

# 2️⃣ Запустить (выберите один вариант)
# Вариант A: Docker
.\scripts\run-docker.ps1

# Вариант B: Локально
.\scripts\run-local.ps1 -MigrateDb

# 3️⃣ Открыть в браузере
# http://localhost:5000 (или :80 для Docker)
# http://localhost:5165/swagger
```

### Каждый день: Разработка

```powershell
# 1️⃣ Запустить watch режимы
cd EmailFixer.Api
dotnet watch run

# 2️⃣ В новом окне - Client
cd EmailFixer.Client
dotnet watch run

# 3️⃣ Кодить - автоперезагрузка сама!

# 4️⃣ Перед коммитом
dotnet test
git add .
git commit -m "Your message"
git push
```

### При изменении БД

```powershell
# 1️⃣ Отредактировать Entity в:
# EmailFixer.Infrastructure/Data/Entities/*

# 2️⃣ Создать миграцию
dotnet ef migrations add MigrationName -p EmailFixer.Infrastructure -s EmailFixer.Api

# 3️⃣ Применить локально
dotnet ef database update -p EmailFixer.Infrastructure -s EmailFixer.Api

# 4️⃣ Закоммитить
git add .
git commit -m "Add migration: ..."
```

---

## 🐛 Решение проблем

### ❌ Порт уже используется
```powershell
# Найти процесс
netstat -ano | findstr :5165

# Завершить (замените PID)
taskkill /PID 12345 /F
```

### ❌ Docker ошибка
```powershell
docker-compose down -v
docker system prune -a
docker-compose up --build
```

### ❌ БД проблемы
```powershell
# Пересоздать
rm emailfixer.db
dotnet ef database update -p EmailFixer.Infrastructure -s EmailFixer.Api
```

### ❌ Миграции не работают
```powershell
dotnet ef database update -p EmailFixer.Infrastructure -s EmailFixer.Api -v
```

👉 **Полный список решений:** [docs/LOCAL_SETUP.md#решение-проблем](docs/LOCAL_SETUP.md#решение-проблем)

---

## 🎯 Что дальше?

### Для новичков
1. ✅ Запустить проект (выберите Docker или локально)
2. ✅ Открыть http://localhost:5165/swagger
3. ✅ Попробовать POST /api/email/validate
4. ✅ Посмотреть в Client как это работает
5. ✅ Прочитать [CLAUDE.md](CLAUDE.md)

### Для разработчиков
1. 📖 Изучить [docs/LOCAL_SETUP.md](docs/LOCAL_SETUP.md)
2. 🔨 Понять структуру кода в каждом проекте
3. 💾 Научиться работать с миграциями
4. ✅ Запустить тесты
5. 🚀 Начать разработку!

### Для деплоя
1. 👁️ Проверить [CLAUDE.md - Deployment](CLAUDE.md#deployment)
2. 🔐 Настроить secrets на GCP
3. 🐳 Проверить Docker образы
4. 🚀 Развернуть на Cloud Run

---

## 📞 Помощь

### Документация
- 📖 **LOCAL_SETUP.md** - Полное руководство
- ⚡ **quick-commands.md** - Шпаргалка
- 📋 **CLAUDE.md** - Архитектура проекта

### Команды помощи
```powershell
# Версия .NET
dotnet --version

# Информация о Docker
docker --version
docker-compose --version

# Help на любую команду
dotnet help
dotnet ef help
```

### Запрашивайте help в IDE
- Visual Studio: F1
- VS Code: Ctrl+K, Ctrl+I

---

## 📊 Статистика проекта

- **8 проектов** (.NET решений)
- **2 БД** поддерживаются (SQLite & PostgreSQL)
- **11+ API endpoints** задокументировано
- **2 фронтенда** компилятора (Blazor WASM + ngninx)
- **100% контейнеризировано** (Docker)
- **CI/CD готово** (GitHub Actions)

---

## 🎉 Готово!

Вы все настроили! Теперь:

1. 🚀 Запустите скрипт:
   ```powershell
   .\scripts\run-docker.ps1
   # или
   .\scripts\run-local.ps1 -MigrateDb
   ```

2. 🌐 Откройте браузер:
   ```
   http://localhost:5000  (локально)
   http://localhost        (Docker)
   http://localhost:5165/swagger  (API)
   ```

3. 🎓 Изучите документацию:
   ```
   docs/LOCAL_SETUP.md - Все детали
   scripts/quick-commands.md - Шпаргалка
   ```

4. 💻 Начните разработку:
   ```powershell
   dotnet watch run
   ```

---

**Version:** 1.0.0
**Last Updated:** 2025-11-12
**Status:** ✅ Production Ready

🤖 Generated with Claude Code

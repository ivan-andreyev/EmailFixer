# План: Paddle.com Payment Integration

**Дата создания:** 2025-11-09
**Статус:** [ ] В работе
**Приоритет:** P0 (Critical)

## Цель
Реализовать полную интеграцию с Paddle.com для обработки платежей за email credits, заменив существующую Stripe интеграцию.

## Контекст
- Проект Email Fixer на .NET 8
- Уже реализованы: Core Layer (Email Validator), Database Layer (EF Core + PostgreSQL)
- Существует Stripe integration, которую нужно заменить на Paddle
- CreditTransaction entity уже создана, но использует StripePaymentIntentId
- CreditTransactionRepository уже реализован с методами для Stripe

## Scope

### В scope:
- Создание IPaddlePaymentService интерфейса
- Реализация PaddlePaymentService с HttpClient
- Обновление CreditTransaction entity для Paddle
- Обновление CreditTransactionRepository для работы с Paddle
- Валидация вебхук сигнатур Paddle (HMAC-SHA256)
- Обработка transaction.completed и transaction.updated webhooks
- Начисление кредитов после успешного платежа
- XML документация для всех публичных API
- Тестирование компиляции проекта

### Вне scope:
- Миграция данных от Stripe к Paddle (будет отдельная задача)
- UI/Frontend интеграция
- Полное удаление Stripe сервисов (сохраним для backward compatibility)
- Unit тесты (будет отдельная задача)

## Задачи

### [ ] 01. Обновить CreditTransaction entity для Paddle
**Оценка:** 15 минут
**Зависимости:** Нет

**Что сделать:**
- Добавить поле `PaddleTransactionId` (string, nullable)
- Сохранить `StripePaymentIntentId` для backward compatibility
- Обновить XML комментарии

**Критерии приёмки:**
- [ ] Поле PaddleTransactionId добавлено
- [ ] StripePaymentIntentId остался для совместимости
- [ ] XML документация обновлена
- [ ] Проект компилируется

**Файлы:**
- `EmailFixer.Infrastructure/Data/Entities/CreditTransaction.cs`

---

### [ ] 02. Обновить ICreditTransactionRepository для Paddle
**Оценка:** 20 минут
**Зависимости:** 01

**Что сделать:**
- Добавить метод `GetByPaddleTransactionIdAsync(string transactionId)`
- Сохранить метод `GetByStripePaymentIntentIdAsync` для backward compatibility
- Обновить XML комментарии

**Критерии приёмки:**
- [ ] Метод GetByPaddleTransactionIdAsync добавлен в интерфейс
- [ ] XML документация добавлена
- [ ] GetByStripePaymentIntentIdAsync сохранён
- [ ] Проект компилируется

**Файлы:**
- `EmailFixer.Infrastructure/Data/Repositories/ICreditTransactionRepository.cs`

---

### [ ] 03. Реализовать GetByPaddleTransactionIdAsync в CreditTransactionRepository
**Оценка:** 15 минут
**Зависимости:** 02

**Что сделать:**
- Реализовать метод `GetByPaddleTransactionIdAsync`
- Использовать паттерн как в GetByStripePaymentIntentIdAsync
- Добавить валидацию параметра
- Использовать async/await

**Критерии приёмки:**
- [ ] Метод реализован
- [ ] Валидация параметра добавлена (ArgumentException)
- [ ] Используется FirstOrDefaultAsync
- [ ] XML документация добавлена
- [ ] Проект компилируется

**Файлы:**
- `EmailFixer.Infrastructure/Data/Repositories/CreditTransactionRepository.cs`

---

### [ ] 04. Создать IPaddlePaymentService интерфейс
**Оценка:** 30 минут
**Зависимости:** 03

**Что сделать:**
- Создать файл `Services/Payment/IPaddlePaymentService.cs`
- Определить метод `CreateCheckoutUrlAsync(Guid userId, int emailsCount)`
- Определить метод `HandleTransactionWebhookAsync(string webhookPayload)`
- Определить метод `ValidateWebhookSignatureAsync(string body, string signature)`
- Создать record types для результатов (PaddleCheckoutResult, PaddleWebhookResult)
- Полная XML документация

**Критерии приёмки:**
- [ ] Интерфейс IPaddlePaymentService создан
- [ ] 3 метода определены с правильными сигнатурами
- [ ] Record types для результатов созданы
- [ ] Все методы async с CancellationToken
- [ ] XML документация для всех методов
- [ ] Проект компилируется

**Файлы:**
- `EmailFixer.Infrastructure/Services/Payment/IPaddlePaymentService.cs` (новый)

---

### [ ] 05. Создать PaddleConfiguration класс
**Оценка:** 20 минут
**Зависимости:** 04

**Что сделать:**
- Создать `Services/Payment/PaddleConfiguration.cs`
- Определить свойства: ApiKey, SellerId, WebhookSecret, ApiBaseUrl
- Добавить валидацию в конструкторе
- XML документация

**Критерии приёмки:**
- [ ] Класс PaddleConfiguration создан
- [ ] Все 4 свойства определены
- [ ] Валидация в конструкторе (ArgumentException для пустых значений)
- [ ] XML документация добавлена
- [ ] Проект компилируется

**Файлы:**
- `EmailFixer.Infrastructure/Services/Payment/PaddleConfiguration.cs` (новый)

---

### [ ] 06. Добавить NuGet пакет для HTTP клиента
**Оценка:** 10 минут
**Зависимости:** 05

**Что сделать:**
- Проверить наличие Microsoft.Extensions.Http в проекте
- Если нет - добавить PackageReference
- Убедиться что System.Text.Json доступен (входит в .NET 8)

**Критерии приёмки:**
- [ ] Microsoft.Extensions.Http доступен
- [ ] System.Text.Json доступен
- [ ] Проект компилируется
- [ ] Нет конфликтов пакетов

**Файлы:**
- `EmailFixer.Infrastructure/EmailFixer.Infrastructure.csproj`

---

### [ ] 07. Создать PaddlePaymentService - Базовая структура
**Оценка:** 30 минут
**Зависимости:** 06

**Что сделать:**
- Создать `Services/Payment/PaddlePaymentService.cs`
- Реализовать конструктор с IHttpClientFactory, PaddleConfiguration, ILogger, ICreditTransactionRepository, UserRepository
- Создать приватные методы-заглушки для 3 публичных методов
- Добавить поле для HttpClient
- XML документация класса

**Критерии приёмки:**
- [ ] Класс PaddlePaymentService создан
- [ ] Конструктор с DI зависимостями реализован
- [ ] 3 метода интерфейса реализованы (пока throw NotImplementedException)
- [ ] Поле _httpClient создано
- [ ] XML документация класса добавлена
- [ ] Проект компилируется

**Файлы:**
- `EmailFixer.Infrastructure/Services/Payment/PaddlePaymentService.cs` (новый)

---

### [ ] 08. Реализовать ValidateWebhookSignatureAsync
**Оценка:** 40 минут
**Зависимости:** 07

**Что сделать:**
- Реализовать HMAC-SHA256 валидацию
- Использовать WebhookSecret из конфигурации
- Сравнить вычисленную подпись с переданной
- Добавить логирование
- Обработать edge cases (null/empty signature)

**Критерии приёмки:**
- [ ] HMAC-SHA256 вычисление реализовано
- [ ] Сравнение подписи безопасное (constant-time)
- [ ] Валидация параметров (ArgumentException)
- [ ] Логирование добавлено (Information при успехе, Warning при неудаче)
- [ ] XML документация добавлена
- [ ] Проект компилируется

**Файлы:**
- `EmailFixer.Infrastructure/Services/Payment/PaddlePaymentService.cs`

---

### [ ] 09. Реализовать CreateCheckoutUrlAsync
**Оценка:** 60 минут
**Зависимости:** 08

**Что сделать:**
- Реализовать создание checkout через Paddle API POST /transactions
- Рассчитать цену: emailsCount / 100 * 1.00 USD
- Создать JSON payload для Paddle API
- Отправить HTTP POST запрос
- Парсить ответ и извлечь checkout URL
- Создать pending CreditTransaction в БД
- Обработать ошибки от API
- Логирование всех операций

**Критерии приёмки:**
- [ ] Расчёт цены корректен ($1 за 100 credits)
- [ ] JSON payload сформирован правильно
- [ ] HTTP запрос отправлен с Authorization header
- [ ] Ответ распарсен, checkout URL извлечён
- [ ] Pending транзакция создана в БД
- [ ] HttpRequestException обработаны
- [ ] Логирование добавлено
- [ ] XML документация добавлена
- [ ] Проект компилируется

**Файлы:**
- `EmailFixer.Infrastructure/Services/Payment/PaddlePaymentService.cs`

---

### [ ] 10. Создать DTO классы для Paddle API
**Оценка:** 30 минут
**Зависимости:** 09

**Что сделать:**
- Создать `Services/Payment/Paddle/PaddleTransactionDto.cs`
- Создать `Services/Payment/Paddle/PaddleWebhookDto.cs`
- Определить все необходимые свойства согласно Paddle API
- Добавить JsonPropertyName атрибуты для snake_case mapping
- XML документация

**Критерии приёмки:**
- [ ] PaddleTransactionDto создан с полями: id, status, amount, currency, customer_id, custom_data
- [ ] PaddleWebhookDto создан с полями: event_type, data
- [ ] JsonPropertyName атрибуты добавлены
- [ ] XML документация добавлена
- [ ] Проект компилируется

**Файлы:**
- `EmailFixer.Infrastructure/Services/Payment/Paddle/PaddleTransactionDto.cs` (новый)
- `EmailFixer.Infrastructure/Services/Payment/Paddle/PaddleWebhookDto.cs` (новый)

---

### [ ] 11. Реализовать HandleTransactionWebhookAsync
**Оценка:** 90 минут
**Зависимости:** 10

**Что сделать:**
- Распарсить webhook payload в PaddleWebhookDto
- Проверить event_type (transaction.completed, transaction.updated)
- Извлечь transaction data
- Найти соответствующую CreditTransaction по PaddleTransactionId
- Обновить статус транзакции на Completed
- Начислить кредиты пользователю (User.CreditsBalance += CreditsChange)
- Установить CompletedAt = DateTime.UtcNow
- Сохранить изменения в БД
- Обработать idempotency (если уже processed)
- Логирование всех операций
- Обработка ошибок

**Критерии приёмки:**
- [ ] Webhook payload корректно десериализуется
- [ ] Поддерживаются event types: transaction.completed, transaction.updated
- [ ] Транзакция находится по PaddleTransactionId
- [ ] Статус обновляется на Completed
- [ ] Кредиты начисляются пользователю
- [ ] CompletedAt устанавливается
- [ ] Idempotency реализована (повторные вебхуки не дублируют кредиты)
- [ ] Логирование добавлено
- [ ] Обработка ошибок (JsonException, DbUpdateException)
- [ ] XML документация добавлена
- [ ] Проект компилируется

**Файлы:**
- `EmailFixer.Infrastructure/Services/Payment/PaddlePaymentService.cs`

---

### [ ] 12. Добавить интеграционную документацию
**Оценка:** 30 минут
**Зависимости:** 11

**Что сделать:**
- Создать `docs/PADDLE_INTEGRATION.md`
- Описать как работает интеграция
- Привести примеры конфигурации
- Описать webhook setup
- Описать ценообразование
- Добавить troubleshooting секцию

**Критерии приёмки:**
- [ ] Документ создан
- [ ] Описана архитектура интеграции
- [ ] Примеры конфигурации добавлены
- [ ] Webhook setup инструкция добавлена
- [ ] Секция troubleshooting добавлена
- [ ] Markdown форматирование корректно

**Файлы:**
- `docs/PADDLE_INTEGRATION.md` (новый)

---

### [ ] 13. Финальная сборка и проверка
**Оценка:** 20 минут
**Зависимости:** 12

**Что сделать:**
- Выполнить dotnet build для EmailFixer.Infrastructure
- Выполнить dotnet build для всего solution
- Проверить отсутствие warnings
- Проверить что все файлы скомпилированы
- Проверить что нет breaking changes в публичных API

**Критерии приёмки:**
- [ ] dotnet build EmailFixer.Infrastructure.csproj - SUCCESS
- [ ] dotnet build EmailFixer.sln - SUCCESS
- [ ] Нет compilation warnings
- [ ] Нет breaking changes в существующих интерфейсах
- [ ] Все новые типы доступны

**Файлы:**
- Все файлы проекта

---

## Входные зависимости
- .NET 8 SDK установлен
- EmailFixer.Infrastructure проект существует
- EmailFixer.Core проект существует
- CreditTransaction entity реализована
- CreditTransactionRepository реализован
- PostgreSQL entities настроены

## Выходные результаты
- IPaddlePaymentService интерфейс
- PaddlePaymentService реализация
- Обновлённая CreditTransaction entity с Paddle полями
- Обновлённый CreditTransactionRepository с Paddle методами
- DTO классы для Paddle API
- PaddleConfiguration класс
- Документация интеграции
- Успешная компиляция всего проекта

## Риски
- **Изменения в Paddle API**: Paddle может изменить формат API
  - Митигация: Использовать версионированные endpoints
- **Breaking changes для существующих пользователей**: Stripe всё ещё может использоваться
  - Митигация: Сохранить Stripe методы для backward compatibility
- **Webhook signature validation issues**: Неправильная реализация HMAC
  - Митигация: Использовать standard library для HMAC-SHA256, тестировать с реальными webhooks

## Метрики успеха
- [ ] Проект компилируется без ошибок
- [ ] Проект компилируется без warnings
- [ ] Все публичные API имеют XML документацию
- [ ] CreditTransaction поддерживает как Paddle так и Stripe (backward compatibility)
- [ ] Repository методы реализованы для Paddle
- [ ] PaddlePaymentService полностью реализован (3 метода)
- [ ] Документация создана

## Примечания
- Сохраняем Stripe integration для backward compatibility
- Не удаляем StripePaymentIntentId из entity
- Paddle использует transaction ID вместо payment intent ID
- Ценообразование: $1.00 = 100 email credits
- Webhook signature: HMAC-SHA256 с WebhookSecret

# WorkTrack.Common.Messaging.Kafka

Реализация `WorkTrack.Common.Messaging` для Apache Kafka. Предоставляет готовую реализацию `IMessagePublisher` для публикации сообщений в Kafka.

**Версия**: 1.0.1  
**Статус**: ✅ Опубликован в GitHub Packages  
**CI/CD**: ✅ Автоматическая публикация при push в `main`

## Установка

Пакет доступен через GitHub Packages. Добавьте в `Directory.Packages.props`:

```xml
<PackageVersion Include="WorkTrack.Common.Messaging.Kafka" Version="1.0.1" />
```

И в `.csproj`:

```xml
<PackageReference Include="WorkTrack.Common.Messaging.Kafka" />
```

## Настройка NuGet

Для доступа к GitHub Packages настройте `nuget.config`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="github" value="https://nuget.pkg.github.com/WorkTrack-Team/index.json" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
  <packageSourceCredentials>
    <github>
      <add key="Username" value="BelousovMike" />
      <add key="ClearTextPassword" value="YOUR_PERSONAL_ACCESS_TOKEN" />
    </github>
  </packageSourceCredentials>
  <packageSourceMapping>
    <packageSource key="github">
      <package pattern="WorkTrack.Common.*" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
```

**Важно**: 
- Для локальной разработки создайте Personal Access Token (PAT) с правами `read:packages` и `write:packages`
- Используйте package source mapping для оптимизации поиска пакетов: `WorkTrack.Common.*` → GitHub Packages, остальные → nuget.org
- Файл `nuget.config` с токеном должен быть в `.gitignore`

### Настройка через CLI

```bash
# Обновить или добавить источник GitHub Packages
dotnet nuget update source github \
  --username BelousovMike \
  --password YOUR_PERSONAL_ACCESS_TOKEN \
  --store-password-in-clear-text || \
dotnet nuget add source https://nuget.pkg.github.com/WorkTrack-Team/index.json \
  --name github \
  --username BelousovMike \
  --password YOUR_PERSONAL_ACCESS_TOKEN \
  --store-password-in-clear-text

# Добавить nuget.org
dotnet nuget add source https://api.nuget.org/v3/index.json --name nuget.org
```

## Использование

### Базовая регистрация

```csharp
using WorkTrack.Common.Messaging;
using WorkTrack.Common.Messaging.Kafka;
using WorkTrack.Common.Messaging.Kafka.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Регистрируем базовые сервисы messaging
builder.Services.AddCommonMessaging();

// Регистрируем Kafka publisher через конфигурацию
builder.Services.AddKafkaMessagePublisher(builder.Configuration, "Kafka");

// Или через делегат
builder.Services.AddKafkaMessagePublisher(options =>
{
    options.BootstrapServers = "localhost:9092";
    options.ClientId = "my-app";
    options.EnableIdempotence = true;
    options.Security.SecurityProtocol = "SASL_SSL";
    options.Security.SaslMechanism = "SCRAM-SHA-256";
    options.Security.SaslUsername = "username";
    options.Security.SaslPassword = "password";
});
```

### Конфигурация через appsettings.json

```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "ClientId": "worktrack-app",
    "EnableIdempotence": true,
    "AcksTimeout": "00:00:30",
    "GroupId": "worktrack-consumer-group",
    "AutoOffsetReset": "earliest",
    "SessionTimeout": "00:00:30",
    "DefaultHeaders": {
      "source": "worktrack",
      "version": "1.0"
    },
    "Security": {
      "SecurityProtocol": "SASL_SSL",
      "SaslMechanism": "SCRAM-SHA-256",
      "SaslUsername": "username",
      "SaslPassword": "password"
    }
  }
}
```

### Публикация сообщений

```csharp
public class MyService
{
    private readonly IMessagePublisher _publisher;

    public MyService(IMessagePublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task PublishUserCreatedEvent(Guid userId, string email)
    {
        var event = new UserCreatedEvent
        {
            UserId = userId,
            Email = email,
            CreatedAt = DateTime.UtcNow
        };

        var key = MessageKeyBuilder.Default.ForId(userId);
        var headers = MessageHeaders.ForId("user-id", userId);

        await _publisher.PublishAsync(
            topic: "user-events",
            key: key,
            payload: event,
            headers: headers);
    }
}
```

### Работа с заголовками

```csharp
// Использование MessageHeaders
var headers = MessageHeaders.ForId("correlation-id", correlationId);
await _publisher.PublishAsync("events", key, payload, headers);

// Или с несколькими идентификаторами
var headers2 = MessageHeaders.ForIds(
    new MessageHeaderDescriptor("user-id", userId),
    new MessageHeaderDescriptor("tenant-id", tenantId));

// Добавление дополнительных заголовков
var headers3 = MessageHeaders.ForId(
    "event-id",
    eventId,
    ("custom-header", "value"),
    ("version", "1.0"));
```

### Обработка ошибок

```csharp
try
{
    await _publisher.PublishAsync("topic", key, payload, headers);
}
catch (KafkaPublishException ex)
{
    // Обработка ошибок публикации
    logger.LogError(ex, 
        "Failed to publish to topic {Topic} with key {Key}", 
        ex.Topic, 
        ex.Key);
    
    // Доступ к внутреннему исключению
    if (ex.InnerException is KafkaException kafkaEx)
    {
        // Дополнительная обработка Kafka-специфичных ошибок
    }
}
```

## Архитектура

Пакет реализует паттерн Template Method через наследование от `MessagePublisherBase`:

- **Сериализация**: Делегируется в `IMessageSerializer` (JSON по умолчанию)
- **Заголовки**: Автоматическое слияние `defaultHeaders` из конфигурации и пользовательских заголовков
- **Логирование**: Структурированное логирование через Serilog с метриками производительности
- **Ошибки**: Обёртывание исключений в `KafkaPublishException` с контекстом

## Зависимости

- `WorkTrack.Common.Messaging` — базовые абстракции и утилиты
- `Confluent.Kafka` — клиент Apache Kafka
- `Serilog` — структурированное логирование
- `Microsoft.Extensions.Options` — конфигурация через опции
- `Ardalis.GuardClauses` — guard clauses для валидации

## Публикация пакета

Пакет автоматически публикуется в GitHub Packages при push в ветку `main` через GitHub Actions workflow.

### CI/CD Pipeline

Workflow `.github/workflows/ci.yml` выполняет:
1. Сборку решения
2. Запуск unit-тестов
3. Проверку форматирования кода (StyleCop)
4. Автоматическую публикацию пакета в GitHub Packages при успешной сборке

### Ручная публикация (если необходимо)

```bash
# Сборка пакета
dotnet pack src/WorkTrack.Common.Messaging.Kafka/WorkTrack.Common.Messaging.Kafka.csproj \
  --configuration Release \
  --output ./nupkg

# Публикация в GitHub Packages
dotnet nuget push nupkg/WorkTrack.Common.Messaging.Kafka.*.nupkg \
  --source github \
  --api-key YOUR_PERSONAL_ACCESS_TOKEN \
  --skip-duplicate
```

### Проверка публикации

Пакет доступен по адресу:
```
https://nuget.pkg.github.com/WorkTrack-Team/download/worktrack.common.messaging.kafka/index.json
```

Проверка через API:
```bash
curl -u "BelousovMike:YOUR_PAT" \
  "https://nuget.pkg.github.com/WorkTrack-Team/download/worktrack.common.messaging.kafka/index.json"
```

## Зависимые пакеты

Этот пакет требует:
- `WorkTrack.Common.Messaging` (1.1.0) — базовые абстракции, также опубликован в GitHub Packages

## Лицензия

MIT

## Репозиторий

https://github.com/WorkTrack-Team/WorkTrack.Common.Messaging.Kafka


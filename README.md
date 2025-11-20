# WorkTrack.Common.Messaging.Kafka

Реализация `WorkTrack.Common.Messaging` для Apache Kafka. Предоставляет готовую реализацию `IMessagePublisher` для публикации сообщений в Kafka.

## Установка

Пакет доступен через GitHub Packages. Добавьте в `Directory.Packages.props`:

```xml
<PackageVersion Include="WorkTrack.Common.Messaging.Kafka" Version="1.0.0" />
```

И в `.csproj`:

```xml
<PackageReference Include="WorkTrack.Common.Messaging.Kafka" />
```

## Настройка NuGet

Для доступа к GitHub Packages настройте `nuget.config`:

```xml
<configuration>
  <packageSources>
    <clear />
    <add key="github" value="https://nuget.pkg.github.com/WorkTrack-Team/index.json" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
  <packageSourceCredentials>
    <github>
      <add key="Username" value="YOUR_GITHUB_USERNAME" />
      <add key="ClearTextPassword" value="YOUR_PERSONAL_ACCESS_TOKEN" />
    </github>
  </packageSourceCredentials>
</configuration>
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

## Лицензия

Proprietary — WorkTrack Team


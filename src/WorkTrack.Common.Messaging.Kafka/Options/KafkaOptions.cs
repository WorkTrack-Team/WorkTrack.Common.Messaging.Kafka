using Confluent.Kafka;

namespace WorkTrack.Common.Messaging.Kafka.Options;

/// <summary>
/// Конфигурация Kafka.
/// </summary>
public sealed class KafkaOptions
{
    /// <summary>
    /// Bootstrap-сервера Kafka.
    /// </summary>
    public string BootstrapServers { get; set; } = string.Empty;

    /// <summary>
    /// Клиентский идентификатор продюсера.
    /// </summary>
    public string ClientId { get; set; } = "worktrack-kafka-producer";

    /// <summary>
    /// Использовать ли идемпотентность продюсера.
    /// </summary>
    public bool EnableIdempotence { get; set; } = true;

    /// <summary>
    /// Таймаут подтверждения записи.
    /// </summary>
    public TimeSpan AcksTimeout { get; set; } = TimeSpan.FromSeconds(seconds: 30);

    /// <summary>
    /// Требуемое количество подтверждений от брокеров перед завершением запроса (по умолчанию All = -1).
    /// </summary>
    public Acks Acks { get; set; } = Acks.All;

    /// <summary>
    /// Количество повторных попыток отправки сообщения при ошибке.
    /// </summary>
    public int MessageSendMaxRetries { get; set; } = 3;

    /// <summary>
    /// Задержка в миллисекундах перед отправкой батча сообщений (0 = отправлять сразу).
    /// </summary>
    public int LingerMs { get; set; } = 5;

    /// <summary>
    /// Group ID для consumer (по умолчанию используется ClientId).
    /// </summary>
    public string GroupId { get; set; } = string.Empty;

    /// <summary>
    /// Стратегия автоматического сброса смещения при отсутствии сохраненного смещения (по умолчанию "earliest").
    /// </summary>
    public string AutoOffsetReset { get; set; } = "earliest";

    /// <summary>
    /// Таймаут сессии consumer.
    /// </summary>
    public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromSeconds(seconds: 30);

    /// <summary>
    /// Дополнительные заголовки для всех сообщений.
    /// </summary>
    public IReadOnlyDictionary<string, string> DefaultHeaders { get; set; } =
        new Dictionary<string, string>(comparer: StringComparer.Ordinal);

    /// <summary>
    /// Параметры безопасности (SASL/SCRAM и т.д.).
    /// </summary>
    public KafkaSecurityOptions Security { get; set; } = new();
}

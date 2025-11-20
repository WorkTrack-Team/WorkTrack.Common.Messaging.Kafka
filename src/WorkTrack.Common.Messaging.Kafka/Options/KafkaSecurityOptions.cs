namespace WorkTrack.Common.Messaging.Kafka.Options;

/// <summary>
/// Настройки безопасности Kafka.
/// </summary>
public sealed class KafkaSecurityOptions
{
    /// <summary>
    /// Используемый протокол безопасности.
    /// </summary>
    public string SecurityProtocol { get; set; } = "PLAINTEXT";

    /// <summary>
    /// Тип механизма SASL.
    /// </summary>
    public string SaslMechanism { get; set; } = string.Empty;

    /// <summary>
    /// Имя пользователя SASL.
    /// </summary>
    public string SaslUsername { get; set; } = string.Empty;

    /// <summary>
    /// Пароль SASL.
    /// </summary>
    public string SaslPassword { get; set; } = string.Empty;
}

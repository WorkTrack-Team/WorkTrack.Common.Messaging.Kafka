using WorkTrack.Common.Messaging.Publishers;

namespace WorkTrack.Common.Messaging.Kafka;

/// <summary>
/// Исключение публикации в Kafka.
/// </summary>
public sealed class KafkaPublishException : MessagePublishException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaPublishException"/> class.
    /// </summary>
    public KafkaPublishException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaPublishException"/> class.
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    public KafkaPublishException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaPublishException"/> class.
    /// </summary>
    /// <param name="message">Сообщение об ошибке.</param>
    /// <param name="innerException">Исходное исключение.</param>
    public KafkaPublishException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaPublishException"/> class.
    /// </summary>
    /// <param name="topic">Имя топика.</param>
    /// <param name="key">Ключ сообщения.</param>
    /// <param name="innerException">Исходное исключение.</param>
    public KafkaPublishException(string topic, string key, Exception innerException)
        : base(topic, key, innerException)
    {
    }
}

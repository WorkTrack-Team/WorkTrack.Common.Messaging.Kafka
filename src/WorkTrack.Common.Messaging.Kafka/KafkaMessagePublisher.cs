using System.Text;
using Ardalis.GuardClauses;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Serilog;
using WorkTrack.Common.Messaging.Kafka.Internal;
using WorkTrack.Common.Messaging.Kafka.Options;
using WorkTrack.Common.Messaging.Publishers;
using WorkTrack.Common.Messaging.Serialization;

namespace WorkTrack.Common.Messaging.Kafka;

/// <summary>
/// Реализация IMessagePublisher для Apache Kafka.
/// </summary>
public sealed class KafkaMessagePublisher : MessagePublisherBase, IDisposable
{
    private readonly IKafkaProducerFactory _producerFactory;
    private readonly ILogger _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaMessagePublisher"/> class.
    /// </summary>
    /// <param name="producerFactory">Фабрика Kafka-продюсеров.</param>
    /// <param name="serializer">Сериализатор сообщений.</param>
    /// <param name="logger">Логгер (Serilog).</param>
    /// <param name="options">Настройки Kafka.</param>
    public KafkaMessagePublisher(
        IKafkaProducerFactory producerFactory,
        IMessageSerializer serializer,
        ILogger logger,
        IOptions<KafkaOptions> options)
        : base(
            serializer: serializer,
            logger: logger,
            defaultHeaders: Guard.Against.Null(options).Value.DefaultHeaders)
    {
        _producerFactory = Guard.Against.Null(producerFactory);
        _logger = Guard.Against.Null(logger);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _producerFactory.Dispose();
        _disposed = true;
    }

    /// <inheritdoc />
    protected override async Task PublishCoreAsync(
        string topic,
        string key,
        byte[] serializedPayload,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(KafkaMessagePublisher));

        var producer = _producerFactory.GetProducer();
        var message = CreateMessage(
            key: key,
            payloadBytes: serializedPayload,
            headers: headers);

        var deliveryResult = await producer.ProduceAsync(
            topic: topic,
            message: message,
            cancellationToken: cancellationToken).ConfigureAwait(continueOnCapturedContext: false);

        LogDeliveryResult(deliveryResult: deliveryResult, key: key);
    }

    /// <inheritdoc />
    protected override MessagePublishException CreatePublishException(
        string topic,
        string key,
        Exception innerException) =>
        new KafkaPublishException(topic: topic, key: key, innerException: innerException);

    private static Headers BuildHeaders(IReadOnlyDictionary<string, string> mergedHeaders)
    {
        Guard.Against.Null(mergedHeaders);
        Headers result = [];
        foreach (var header in mergedHeaders)
        {
            Guard.Against.NullOrWhiteSpace(input: header.Key);
            Guard.Against.Null(input: header.Value);
            result.Add(key: header.Key, val: Encoding.UTF8.GetBytes(s: header.Value));
        }

        return result;
    }

    private static Message<string, byte[]> CreateMessage(
        string key,
        byte[] payloadBytes,
        IReadOnlyDictionary<string, string> headers)
    {
        var kafkaHeaders = BuildHeaders(mergedHeaders: headers);

        return new Message<string, byte[]>
        {
            Key = key,
            Value = payloadBytes,
            Headers = kafkaHeaders,
        };
    }

    /// <summary>
    /// Логирует результат доставки сообщения в Kafka.
    /// </summary>
    /// <param name="deliveryResult">Результат доставки сообщения.</param>
    /// <param name="key">Ключ сообщения.</param>
    private void LogDeliveryResult(DeliveryResult<string, byte[]> deliveryResult, string key)
    {
        if (deliveryResult.Status != PersistenceStatus.Persisted)
        {
            _logger.Warning(
                "Message delivery status is not Persisted: topic={Topic}, partition={Partition}, offset={Offset}, status={Status}, key={Key}",
                deliveryResult.Topic,
                deliveryResult.Partition,
                deliveryResult.Offset,
                deliveryResult.Status,
                key);
        }
        else
        {
            _logger.Debug(
                "Message delivered to Kafka: topic={Topic}, partition={Partition}, offset={Offset}, status={Status}, key={Key}",
                deliveryResult.Topic,
                deliveryResult.Partition,
                deliveryResult.Offset,
                deliveryResult.Status,
                key);
        }
    }
}

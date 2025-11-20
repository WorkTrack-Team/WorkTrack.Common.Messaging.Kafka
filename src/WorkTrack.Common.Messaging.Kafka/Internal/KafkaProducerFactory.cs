using Ardalis.GuardClauses;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using WorkTrack.Common.Messaging.Kafka.Options;

namespace WorkTrack.Common.Messaging.Kafka.Internal;

/// <summary>
/// Фабрика Kafka-продюсеров.
/// </summary>
public sealed class KafkaProducerFactory : IKafkaProducerFactory
{
    private readonly IProducer<string, byte[]> _producer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaProducerFactory"/> class.
    /// </summary>
    /// <param name="options">Настройки Kafka.</param>
    public KafkaProducerFactory(IOptions<KafkaOptions> options)
    {
        Guard.Against.Null(options);
        var kafkaOptions = options.Value;
        Guard.Against.NullOrWhiteSpace(kafkaOptions.BootstrapServers);
        var config = BuildConfig(options: kafkaOptions);
        _producer = new ProducerBuilder<string, byte[]>(config: config).Build();
    }

    /// <summary>
    /// Возвращает продюсер Kafka.
    /// </summary>
    /// <returns>Экземпляр продюсера Kafka.</returns>
    public IProducer<string, byte[]> GetProducer()
    {
        ObjectDisposedException.ThrowIf(condition: _disposed, instance: nameof(KafkaProducerFactory));
        return _producer;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _producer.Dispose();
        _disposed = true;
    }

    private static ProducerConfig BuildConfig(KafkaOptions options)
    {
        var config = CreateBaseConfig(options: options);
        ApplySecurityProtocol(config: config, security: options.Security);
        ApplySaslOptions(config: config, security: options.Security);
        return config;
    }

    private static ProducerConfig CreateBaseConfig(KafkaOptions options) =>
        new()
        {
            BootstrapServers = options.BootstrapServers,
            ClientId = options.ClientId,
            EnableIdempotence = options.EnableIdempotence,
            Acks = options.Acks,
            MessageSendMaxRetries = options.MessageSendMaxRetries,
            LingerMs = options.LingerMs,
            SocketTimeoutMs = (int)options.AcksTimeout.TotalMilliseconds,
        };

    private static void ApplySecurityProtocol(ProducerConfig config, KafkaSecurityOptions security)
    {
        if (string.IsNullOrWhiteSpace(value: security.SecurityProtocol)
            || string.Equals(a: security.SecurityProtocol, b: "PLAINTEXT", comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!Enum.TryParse<SecurityProtocol>(value: security.SecurityProtocol, ignoreCase: true, out var protocol))
        {
            throw new ArgumentException(
                $"Invalid SecurityProtocol: '{security.SecurityProtocol}'. " +
                $"Valid values: {string.Join(", ", Enum.GetNames<SecurityProtocol>())}",
                nameof(security));
        }

        config.SecurityProtocol = protocol;
    }

    private static void ApplySaslOptions(ProducerConfig config, KafkaSecurityOptions security)
    {
        if (string.IsNullOrWhiteSpace(value: security.SaslMechanism))
        {
            return;
        }

        if (!Enum.TryParse<SaslMechanism>(value: security.SaslMechanism, ignoreCase: true, out var mechanism))
        {
            throw new ArgumentException(
                $"Invalid SaslMechanism: '{security.SaslMechanism}'. " +
                $"Valid values: {string.Join(", ", Enum.GetNames<SaslMechanism>())}",
                nameof(security));
        }

        config.SaslMechanism = mechanism;
        config.SaslUsername = security.SaslUsername;
        config.SaslPassword = security.SaslPassword;
    }
}

using Ardalis.GuardClauses;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using WorkTrack.Common.Messaging.Kafka.Options;

namespace WorkTrack.Common.Messaging.Kafka.Internal;

/// <summary>
/// Фабрика Kafka-консьюмеров.
/// </summary>
public sealed class KafkaConsumerFactory : IDisposable
{
    private readonly ConsumerConfig _config;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaConsumerFactory"/> class.
    /// </summary>
    /// <param name="options">Настройки Kafka.</param>
    public KafkaConsumerFactory(IOptions<KafkaOptions> options)
    {
        Guard.Against.Null(options);
        var kafkaOptions = options.Value;
        Guard.Against.NullOrWhiteSpace(kafkaOptions.BootstrapServers);
        _config = BuildConfig(options: kafkaOptions);
    }

    /// <summary>
    /// Создаёт новый консьюмер Kafka.
    /// </summary>
    /// <returns>Экземпляр консьюмера Kafka.</returns>
    public IConsumer<string, byte[]> CreateConsumer()
    {
        ObjectDisposedException.ThrowIf(condition: _disposed, instance: nameof(KafkaConsumerFactory));
        return new ConsumerBuilder<string, byte[]>(config: _config).Build();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposed = true;
    }

    private static ConsumerConfig BuildConfig(KafkaOptions options)
    {
        var config = CreateBaseConfig(options: options);
        ApplySecurityProtocol(config: config, security: options.Security);
        ApplySaslOptions(config: config, security: options.Security);
        return config;
    }

    private static ConsumerConfig CreateBaseConfig(KafkaOptions options)
    {
        var groupId = string.IsNullOrWhiteSpace(value: options.GroupId) ? options.ClientId : options.GroupId;
        return new ConsumerConfig
        {
            BootstrapServers = options.BootstrapServers,
            GroupId = groupId,
            ClientId = options.ClientId,
            AutoOffsetReset = ParseAutoOffsetReset(options.AutoOffsetReset),
            SessionTimeoutMs = (int)options.SessionTimeout.TotalMilliseconds,
            EnableAutoCommit = true,
            EnablePartitionEof = false,
        };
    }

    private static AutoOffsetReset ParseAutoOffsetReset(string autoOffsetReset)
    {
        if (string.IsNullOrWhiteSpace(autoOffsetReset))
        {
            return AutoOffsetReset.Earliest;
        }

        if (!Enum.TryParse<AutoOffsetReset>(autoOffsetReset, ignoreCase: true, out var result))
        {
            throw new ArgumentException(
                $"Invalid AutoOffsetReset: '{autoOffsetReset}'. " +
                $"Valid values: {string.Join(", ", Enum.GetNames<AutoOffsetReset>())}",
                nameof(autoOffsetReset));
        }

        return result;
    }

    private static void ApplySecurityProtocol(ConsumerConfig config, KafkaSecurityOptions security)
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

    private static void ApplySaslOptions(ConsumerConfig config, KafkaSecurityOptions security)
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

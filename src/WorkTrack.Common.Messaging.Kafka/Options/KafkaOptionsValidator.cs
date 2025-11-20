using Microsoft.Extensions.Options;

namespace WorkTrack.Common.Messaging.Kafka.Options;

/// <summary>
/// Валидатор конфигурации Kafka.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by DI container through IValidateOptions interface.")]
internal sealed class KafkaOptionsValidator : IValidateOptions<KafkaOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, KafkaOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BootstrapServers))
        {
            return ValidateOptionsResult.Fail("BootstrapServers is required.");
        }

        if (!string.IsNullOrWhiteSpace(options.AutoOffsetReset))
        {
            if (!System.Enum.TryParse<Confluent.Kafka.AutoOffsetReset>(
                options.AutoOffsetReset,
                ignoreCase: true,
                out _))
            {
                return ValidateOptionsResult.Fail(
                    $"Invalid AutoOffsetReset: '{options.AutoOffsetReset}'. " +
                    $"Valid values: {string.Join(", ", System.Enum.GetNames<Confluent.Kafka.AutoOffsetReset>())}");
            }
        }

        if (options.Security != null)
        {
            if (!string.IsNullOrWhiteSpace(options.Security.SecurityProtocol)
                && !string.Equals(options.Security.SecurityProtocol, "PLAINTEXT", System.StringComparison.OrdinalIgnoreCase))
            {
                if (!System.Enum.TryParse<Confluent.Kafka.SecurityProtocol>(
                    options.Security.SecurityProtocol,
                    ignoreCase: true,
                    out _))
                {
                    return ValidateOptionsResult.Fail(
                        $"Invalid SecurityProtocol: '{options.Security.SecurityProtocol}'. " +
                        $"Valid values: {string.Join(", ", System.Enum.GetNames<Confluent.Kafka.SecurityProtocol>())}");
                }
            }

            if (!string.IsNullOrWhiteSpace(options.Security.SaslMechanism))
            {
                if (!System.Enum.TryParse<Confluent.Kafka.SaslMechanism>(
                    options.Security.SaslMechanism,
                    ignoreCase: true,
                    out _))
                {
                    return ValidateOptionsResult.Fail(
                        $"Invalid SaslMechanism: '{options.Security.SaslMechanism}'. " +
                        $"Valid values: {string.Join(", ", System.Enum.GetNames<Confluent.Kafka.SaslMechanism>())}");
                }
            }
        }

        return ValidateOptionsResult.Success;
    }
}

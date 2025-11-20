using Ardalis.GuardClauses;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using WorkTrack.Common.Messaging.DependencyInjection;
using WorkTrack.Common.Messaging.Kafka.Internal;
using WorkTrack.Common.Messaging.Kafka.Options;
using WorkTrack.Common.Messaging.Publishers;
using WorkTrack.Common.Messaging.Serialization;

namespace WorkTrack.Common.Messaging.Kafka.DependencyInjection;

/// <summary>
/// Расширения для регистрации Kafka messaging в DI контейнере.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Добавляет Kafka message publisher в DI контейнер.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="configure">Делегат для настройки опций Kafka.</param>
    /// <returns>Коллекция сервисов для цепочки вызовов.</returns>
    public static IServiceCollection AddKafkaMessagePublisher(
        this IServiceCollection services,
        Action<KafkaOptions> configure)
    {
        Guard.Against.Null(services);
        Guard.Against.Null(configure);

        services.AddCommonMessaging();
        services.Configure(configureOptions: configure);
        RegisterKafkaMessagePublisher(services);

        return services;
    }

    /// <summary>
    /// Добавляет Kafka message publisher в DI контейнер с использованием IConfiguration.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="configuration">Конфигурация приложения.</param>
    /// <param name="configurationSectionName">Имя секции конфигурации (по умолчанию "Kafka").</param>
    /// <returns>Коллекция сервисов для цепочки вызовов.</returns>
    public static IServiceCollection AddKafkaMessagePublisher(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSectionName = "Kafka")
    {
        Guard.Against.Null(services);
        Guard.Against.Null(configuration);
        Guard.Against.NullOrWhiteSpace(configurationSectionName);

        services.AddCommonMessaging();
        services.Configure<KafkaOptions>(options => configuration.GetSection(configurationSectionName).Bind(options));
        RegisterKafkaMessagePublisher(services);

        return services;
    }

    private static void RegisterKafkaMessagePublisher(IServiceCollection services)
    {
        services.AddSingleton<IKafkaProducerFactory, KafkaProducerFactory>();
        services.AddSingleton<IValidateOptions<KafkaOptions>, KafkaOptionsValidator>();

        // Регистрируем KafkaMessagePublisher с зависимостями
        services.AddSingleton<IMessagePublisher>(serviceProvider =>
        {
            var producerFactory = serviceProvider.GetRequiredService<IKafkaProducerFactory>();
            var serializer = serviceProvider.GetRequiredService<IMessageSerializer>();
            var logger = serviceProvider.GetRequiredService<ILogger>();
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<KafkaOptions>>();
            return new KafkaMessagePublisher(producerFactory, serializer, logger, options);
        });
    }
}

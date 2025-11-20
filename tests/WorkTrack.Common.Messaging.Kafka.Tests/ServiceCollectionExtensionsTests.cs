using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Serilog;
using WorkTrack.Common.Messaging.Kafka;
using WorkTrack.Common.Messaging.Kafka.DependencyInjection;
using WorkTrack.Common.Messaging.Kafka.Internal;
using WorkTrack.Common.Messaging.Kafka.Options;
using WorkTrack.Common.Messaging.Publishers;
using WorkTrack.Common.Messaging.Serialization;
using Xunit;

namespace WorkTrack.Common.Messaging.Kafka.Tests;

/// <summary>
/// Тесты для <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    /// <summary>
    /// Проверяет, что AddKafkaMessagePublisher регистрирует IMessagePublisher.
    /// </summary>
    [Fact]
    public void AddKafkaMessagePublisher_WithConfigure_RegistersIMessagePublisher()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IMessageSerializer, TestMessageSerializer>();
        services.AddSingleton(Substitute.For<ILogger>());

        // Act
        services.AddKafkaMessagePublisher(options =>
        {
            options.BootstrapServers = "localhost:9092";
            options.ClientId = "test-client";
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var publisher = serviceProvider.GetService<IMessagePublisher>();
        publisher.Should().NotBeNull();
        publisher.Should().BeOfType<KafkaMessagePublisher>();
    }

    /// <summary>
    /// Проверяет, что AddKafkaMessagePublisher регистрирует IKafkaProducerFactory.
    /// </summary>
    [Fact]
    public void AddKafkaMessagePublisher_WithConfigure_RegistersIKafkaProducerFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IMessageSerializer, TestMessageSerializer>();
        services.AddSingleton(Substitute.For<ILogger>());

        // Act
        services.AddKafkaMessagePublisher(options =>
        {
            options.BootstrapServers = "localhost:9092";
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetService<IKafkaProducerFactory>();
        factory.Should().NotBeNull();
        factory.Should().BeOfType<KafkaProducerFactory>();
    }

    /// <summary>
    /// Проверяет, что AddKafkaMessagePublisher с IConfiguration регистрирует IMessagePublisher.
    /// </summary>
    [Fact]
    public void AddKafkaMessagePublisher_WithConfiguration_RegistersIMessagePublisher()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["Kafka:BootstrapServers"] = "localhost:9092",
                    ["Kafka:ClientId"] = "test-client",
                }

                as IEnumerable<KeyValuePair<string, string?>>)
            .Build();
        services.AddSingleton<IMessageSerializer, TestMessageSerializer>();
        services.AddSingleton(Substitute.For<ILogger>());

        // Act
        services.AddKafkaMessagePublisher(configuration, "Kafka");

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var publisher = serviceProvider.GetService<IMessagePublisher>();
        publisher.Should().NotBeNull();
        publisher.Should().BeOfType<KafkaMessagePublisher>();
    }

    /// <summary>
    /// Проверяет, что AddKafkaMessagePublisher настраивает KafkaOptions.
    /// </summary>
    [Fact]
    public void AddKafkaMessagePublisher_WithConfigure_ConfiguresKafkaOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IMessageSerializer, TestMessageSerializer>();
        services.AddSingleton(Substitute.For<ILogger>());
        var expectedBootstrapServers = "test-server:9092";
        var expectedClientId = "test-client-id";

        // Act
        services.AddKafkaMessagePublisher(options =>
        {
            options.BootstrapServers = expectedBootstrapServers;
            options.ClientId = expectedClientId;
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<KafkaOptions>>();
        options.Value.BootstrapServers.Should().Be(expectedBootstrapServers);
        options.Value.ClientId.Should().Be(expectedClientId);
    }

    /// <summary>
    /// Проверяет, что AddKafkaMessagePublisher с null services бросает исключение.
    /// </summary>
    [Fact]
    public void AddKafkaMessagePublisher_WithNullServices_ThrowsArgumentNullException()
    {
        // Act
        var act = () => ServiceCollectionExtensions.AddKafkaMessagePublisher(
            null!,
            _ => { });

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Проверяет, что AddKafkaMessagePublisher с null configure бросает исключение.
    /// </summary>
    [Fact]
    public void AddKafkaMessagePublisher_WithNullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddKafkaMessagePublisher(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Проверяет, что AddKafkaMessagePublisher с null configuration бросает исключение.
    /// </summary>
    [Fact]
    public void AddKafkaMessagePublisher_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddKafkaMessagePublisher(null!, "Kafka");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Проверяет, что AddKafkaMessagePublisher с пустым configurationSectionName бросает исключение.
    /// </summary>
    [Fact]
    public void AddKafkaMessagePublisher_WithEmptyConfigurationSectionName_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var act = () => services.AddKafkaMessagePublisher(configuration, string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Тестовый сериализатор для проверки DI регистрации.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Used for testing DI registration.")]
    private sealed class TestMessageSerializer : IMessageSerializer
    {
        /// <inheritdoc />
        public string Serialize<T>(T value)
            where T : notnull => "{}";

        /// <inheritdoc />
        public T Deserialize<T>(string value) => default!;
    }
}

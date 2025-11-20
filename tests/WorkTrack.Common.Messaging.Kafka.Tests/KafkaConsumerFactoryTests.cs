using FluentAssertions;
using Microsoft.Extensions.Options;
using WorkTrack.Common.Messaging.Kafka.Internal;
using WorkTrack.Common.Messaging.Kafka.Options;
using Xunit;

namespace WorkTrack.Common.Messaging.Kafka.Tests;

/// <summary>
/// Тесты для <see cref="KafkaConsumerFactory"/>.
/// </summary>
public sealed class KafkaConsumerFactoryTests : IDisposable
{
    private readonly KafkaConsumerFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaConsumerFactoryTests"/> class.
    /// </summary>
    public KafkaConsumerFactoryTests()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new KafkaOptions
        {
            BootstrapServers = "localhost:9092",
            GroupId = "test-group",
            AutoOffsetReset = "earliest",
        });
        _factory = new KafkaConsumerFactory(options);
    }

    /// <summary>
    /// Проверяет, что CreateConsumer создаёт консьюмер.
    /// </summary>
    [Fact]
    public void CreateConsumer_CreatesConsumer()
    {
        // Act
        var consumer = _factory.CreateConsumer();

        // Assert
        consumer.Should().NotBeNull();
    }

    /// <summary>
    /// Проверяет, что CreateConsumer создаёт новый экземпляр при каждом вызове.
    /// </summary>
    [Fact]
    public void CreateConsumer_CreatesNewInstance()
    {
        // Act
        var consumer1 = _factory.CreateConsumer();
        var consumer2 = _factory.CreateConsumer();

        // Assert
        consumer1.Should().NotBeSameAs(consumer2);
    }

    /// <summary>
    /// Проверяет, что Dispose освобождает ресурсы.
    /// </summary>
    [Fact]
    public void Dispose_DisposesFactory()
    {
        // Act
        _factory.Dispose();

        // Assert - проверяем, что после Dispose нельзя создать консьюмер
        var act = () => _factory.CreateConsumer();
        act.Should().Throw<ObjectDisposedException>();
    }

    /// <summary>
    /// Проверяет, что Dispose можно вызывать несколько раз безопасно.
    /// </summary>
    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Act
        _factory.Dispose();
        _factory.Dispose();

        // Assert - не должно быть исключений
        var act = () => _factory.Dispose();
        act.Should().NotThrow();
    }

    /// <summary>
    /// Проверяет, что конструктор бросает исключение при null options.
    /// </summary>
    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new KafkaConsumerFactory(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Проверяет, что конструктор бросает исключение при пустом BootstrapServers.
    /// </summary>
    [Fact]
    public void Constructor_WithEmptyBootstrapServers_ThrowsArgumentException()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(new KafkaOptions { BootstrapServers = string.Empty });

        // Act
        var act = () => new KafkaConsumerFactory(options);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _factory?.Dispose();
    }
}

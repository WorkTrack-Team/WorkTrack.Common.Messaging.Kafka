using FluentAssertions;
using Microsoft.Extensions.Options;
using WorkTrack.Common.Messaging.Kafka.Internal;
using WorkTrack.Common.Messaging.Kafka.Options;
using Xunit;

namespace WorkTrack.Common.Messaging.Kafka.Tests;

/// <summary>
/// Тесты для <see cref="KafkaProducerFactory"/>.
/// </summary>
public sealed class KafkaProducerFactoryTests : IDisposable
{
    private readonly KafkaProducerFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaProducerFactoryTests"/> class.
    /// </summary>
    public KafkaProducerFactoryTests()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new KafkaOptions { BootstrapServers = "localhost:9092" });
        _factory = new KafkaProducerFactory(options);
    }

    /// <summary>
    /// Проверяет, что GetProducer возвращает продюсер.
    /// </summary>
    [Fact]
    public void GetProducer_ReturnsProducer()
    {
        // Act
        var producer = _factory.GetProducer();

        // Assert
        producer.Should().NotBeNull();
    }

    /// <summary>
    /// Проверяет, что GetProducer возвращает тот же экземпляр при повторных вызовах.
    /// </summary>
    [Fact]
    public void GetProducer_ReturnsSameInstance()
    {
        // Act
        var producer1 = _factory.GetProducer();
        var producer2 = _factory.GetProducer();

        // Assert
        producer1.Should().BeSameAs(producer2);
    }

    /// <summary>
    /// Проверяет, что Dispose освобождает ресурсы.
    /// </summary>
    [Fact]
    public void Dispose_DisposesProducer()
    {
        // Arrange
        var producer = _factory.GetProducer();

        // Act
        _factory.Dispose();

        // Assert - проверяем, что после Dispose нельзя получить продюсер
        var act = () => _factory.GetProducer();
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
        var act = () => new KafkaProducerFactory(null!);

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
        var act = () => new KafkaProducerFactory(options);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _factory?.Dispose();
    }
}

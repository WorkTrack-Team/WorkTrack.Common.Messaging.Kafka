using FluentAssertions;
using WorkTrack.Common.Messaging.Kafka;
using Xunit;

namespace WorkTrack.Common.Messaging.Kafka.Tests;

/// <summary>
/// Тесты для <see cref="KafkaPublishException"/>.
/// </summary>
public sealed class KafkaPublishExceptionTests
{
    /// <summary>
    /// Проверяет, что конструктор с topic, key и innerException устанавливает свойства правильно.
    /// </summary>
    [Fact]
    public void Constructor_WithTopicKeyAndInnerException_SetsPropertiesCorrectly()
    {
        // Arrange
        var topic = "test-topic";
        var key = "test-key";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new KafkaPublishException(topic, key, innerException);

        // Assert
        exception.Topic.Should().Be(expected: topic);
        exception.Key.Should().Be(expected: key);
        exception.InnerException.Should().BeSameAs(innerException);
        exception.Message.Should().Contain(topic);
        exception.Message.Should().Contain(key);
    }

    /// <summary>
    /// Проверяет, что Message содержит информацию о topic и key.
    /// </summary>
    [Fact]
    public void Message_ContainsTopicAndKey()
    {
        // Arrange
        var topic = "my-topic";
        var key = "my-key";
        var innerException = new InvalidOperationException("Error");

        // Act
        var exception = new KafkaPublishException(topic, key, innerException);

        // Assert
        exception.Message.Should().Contain($"topic '{topic}'");
        exception.Message.Should().Contain($"key '{key}'");
    }

    /// <summary>
    /// Проверяет, что InnerException сохраняется.
    /// </summary>
    [Fact]
    public void InnerException_IsPreserved()
    {
        // Arrange
        var innerException = new InvalidOperationException("Test error");

        // Act
        var exception = new KafkaPublishException("topic", "key", innerException);

        // Assert
        exception.InnerException.Should().BeSameAs(innerException);
        exception.InnerException.Should().BeOfType<InvalidOperationException>();
    }

    /// <summary>
    /// Проверяет, что конструктор без параметров устанавливает пустые строки.
    /// </summary>
    [Fact]
    public void Constructor_WithoutParameters_SetsEmptyStrings()
    {
        // Act
        var exception = new KafkaPublishException();

        // Assert
        exception.Topic.Should().BeEmpty();
        exception.Key.Should().BeEmpty();
    }

    /// <summary>
    /// Проверяет, что конструктор с message устанавливает пустые строки для Topic и Key.
    /// </summary>
    [Fact]
    public void Constructor_WithMessage_SetsEmptyStringsForTopicAndKey()
    {
        // Arrange
        var message = "Custom error message";

        // Act
        var exception = new KafkaPublishException(message);

        // Assert
        exception.Message.Should().Be(expected: message);
        exception.Topic.Should().BeEmpty();
        exception.Key.Should().BeEmpty();
    }

    /// <summary>
    /// Проверяет, что конструктор с message и innerException устанавливает пустые строки для Topic и Key.
    /// </summary>
    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsEmptyStringsForTopicAndKey()
    {
        // Arrange
        var message = "Custom error message";
        var innerException = new InvalidOperationException("Inner");

        // Act
        var exception = new KafkaPublishException(message, innerException);

        // Assert
        exception.Message.Should().Be(expected: message);
        exception.InnerException.Should().BeSameAs(innerException);
        exception.Topic.Should().BeEmpty();
        exception.Key.Should().BeEmpty();
    }
}

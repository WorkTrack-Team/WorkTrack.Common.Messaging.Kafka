using System.Collections;
using System.Text;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Serilog;
using WorkTrack.Common.Messaging.Kafka;
using WorkTrack.Common.Messaging.Kafka.Internal;
using WorkTrack.Common.Messaging.Kafka.Options;
using WorkTrack.Common.Messaging.Serialization;
using Xunit;

namespace WorkTrack.Common.Messaging.Kafka.Tests;

/// <summary>
/// Тесты для <see cref="KafkaMessagePublisher"/>.
/// </summary>
public sealed class KafkaMessagePublisherTests : IDisposable
{
    private readonly IProducer<string, byte[]> _producer;
    private readonly IKafkaProducerFactory _producerFactory;
    private readonly IMessageSerializer _serializer;
    private readonly ILogger _logger;
    private readonly IOptions<KafkaOptions> _options;
    private readonly KafkaMessagePublisher _publisher;

    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaMessagePublisherTests"/> class.
    /// </summary>
    public KafkaMessagePublisherTests()
    {
        _producer = Substitute.For<IProducer<string, byte[]>>();
        _producerFactory = Substitute.For<IKafkaProducerFactory>();
        _producerFactory.GetProducer().Returns(_producer);
        _serializer = Substitute.For<IMessageSerializer>();
        _logger = Substitute.For<ILogger>();
        _options = Microsoft.Extensions.Options.Options.Create(new KafkaOptions { BootstrapServers = "localhost:9092" });
        _publisher = new KafkaMessagePublisher(_producerFactory, _serializer, _logger, _options);
    }

    /// <summary>
    /// Проверяет, что PublishAsync вызывает ProduceAsync с правильными параметрами.
    /// </summary>
    [Fact]
    public async Task PublishAsync_CallsProduceAsync_WithCorrectParameters()
    {
        // Arrange
        var payload = new { Test = "value" };
        var topic = "test-topic";
        var key = "test-key";
        _serializer.Serialize(payload).Returns("{\"test\":\"value\"}");
        var deliveryResult = new DeliveryResult<string, byte[]>
        {
            Topic = topic,
            Partition = 0,
            Offset = 123,
            Status = PersistenceStatus.Persisted,
        };
        _producer.ProduceAsync(Arg.Any<string>(), Arg.Any<Message<string, byte[]>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(deliveryResult));

        // Act
        await _publisher.PublishAsync(topic, key, payload, null);

        // Assert
        await _producer.Received(1).ProduceAsync(
            Arg.Is<string>(t => t == topic),
            Arg.Is<Message<string, byte[]>>(m =>
                m.Key == key &&
                m.Value != null &&
                Encoding.UTF8.GetString(m.Value) == "{\"test\":\"value\"}"),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Проверяет, что PublishAsync передаёт заголовки в сообщение Kafka.
    /// </summary>
    [Fact]
    public async Task PublishAsync_PassesHeaders_ToKafkaMessage()
    {
        // Arrange
        var payload = new { Test = "value" };
        var topic = "test-topic";
        var key = "test-key";
        var headers = new Dictionary<string, string>(StringComparer.Ordinal) { ["header-key"] = "header-value" };
        _serializer.Serialize(payload).Returns("{}");
        var deliveryResult = new DeliveryResult<string, byte[]>
        {
            Topic = topic,
            Partition = 0,
            Offset = 123,
            Status = PersistenceStatus.Persisted,
        };
        _producer.ProduceAsync(Arg.Any<string>(), Arg.Any<Message<string, byte[]>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(deliveryResult));

        // Act
        await _publisher.PublishAsync(topic, key, payload, headers);

        // Assert
        await _producer.Received(1).ProduceAsync(
            Arg.Any<string>(),
            Arg.Is<Message<string, byte[]>>(m =>
                m.Headers != null &&
                m.Headers.Count == 1 &&
                Encoding.UTF8.GetString(m.Headers[0].GetValueBytes()) == "header-value"),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Проверяет, что PublishAsync бросает KafkaPublishException при ошибке.
    /// </summary>
    [Fact]
    public async Task PublishAsync_OnError_ThrowsKafkaPublishException()
    {
        // Arrange
        var payload = new { Test = "value" };
        var topic = "test-topic";
        var key = "test-key";
        _serializer.Serialize(payload).Returns("{}");
        var innerException = new KafkaException(new Error(ErrorCode.Local_Fail, "Test error"));
        _producer.ProduceAsync(Arg.Any<string>(), Arg.Any<Message<string, byte[]>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<DeliveryResult<string, byte[]>>(innerException));

        // Act
        var act = async () => await _publisher.PublishAsync(topic, key, payload, null);

        // Assert
        var exception = await act.Should().ThrowAsync<KafkaPublishException>();
        exception.Which.Topic.Should().Be(topic);
        exception.Which.Key.Should().Be(key);
        exception.Which.InnerException.Should().Be(innerException);
    }

    /// <summary>
    /// Проверяет, что Dispose освобождает ресурсы.
    /// </summary>
    [Fact]
    public void Dispose_DisposesProducerFactory()
    {
        // Act
        _publisher.Dispose();

        // Assert
        _producerFactory.Received(1).Dispose();
    }

    /// <summary>
    /// Проверяет, что Dispose можно вызывать несколько раз безопасно.
    /// </summary>
    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Act
        _publisher.Dispose();
        _publisher.Dispose();

        // Assert
        _producerFactory.Received(1).Dispose();
    }

    /// <summary>
    /// Проверяет, что PublishAsync бросает KafkaPublishException с ObjectDisposedException после Dispose.
    /// </summary>
    [Fact]
    public async Task PublishAsync_AfterDispose_ThrowsKafkaPublishException()
    {
        // Arrange
        _publisher.Dispose();
        var payload = new { Test = "value" };
        _serializer.Serialize(payload).Returns("{}");

        // Act
        var act = async () => await _publisher.PublishAsync("topic", "key", payload, null);

        // Assert
        var exception = await act.Should().ThrowAsync<KafkaPublishException>();
        exception.Which.InnerException.Should().BeOfType<ObjectDisposedException>();
    }

    /// <summary>
    /// Проверяет, что PublishAsync логирует результат доставки с Persisted статусом.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WithPersistedStatus_LogsDebug()
    {
        // Arrange
        var payload = new { Test = "value" };
        var topic = "test-topic";
        var key = "test-key";
        _serializer.Serialize(payload).Returns("{}");
        var deliveryResult = new DeliveryResult<string, byte[]>
        {
            Topic = topic,
            Partition = 1,
            Offset = 456,
            Status = PersistenceStatus.Persisted,
        };
        _producer.ProduceAsync(Arg.Any<string>(), Arg.Any<Message<string, byte[]>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(deliveryResult));

        // Act
        await _publisher.PublishAsync(topic, key, payload, null);

        // Assert
        _logger.Received(1).Debug(
            Arg.Is<string>(t => t == "Message delivered to Kafka: topic={Topic}, partition={Partition}, offset={Offset}, status={Status}, key={Key}"),
            Arg.Any<object[]>());
    }

    /// <summary>
    /// Проверяет, что PublishAsync логирует предупреждение при статусе отличном от Persisted.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WithNonPersistedStatus_LogsWarning()
    {
        // Arrange
        var payload = new { Test = "value" };
        var topic = "test-topic";
        var key = "test-key";
        _serializer.Serialize(payload).Returns("{}");
        var deliveryResult = new DeliveryResult<string, byte[]>
        {
            Topic = topic,
            Partition = 0,
            Offset = 0,
            Status = PersistenceStatus.NotPersisted,
        };
        _producer.ProduceAsync(Arg.Any<string>(), Arg.Any<Message<string, byte[]>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(deliveryResult));

        // Act
        await _publisher.PublishAsync(topic, key, payload, null);

        // Assert
        _logger.Received(1).Warning(
            Arg.Is<string>(t => t == "Message delivery status is not Persisted: topic={Topic}, partition={Partition}, offset={Offset}, status={Status}, key={Key}"),
            Arg.Any<object[]>());
    }

    /// <summary>
    /// Проверяет, что PublishAsync выбрасывает исключение при некорректных заголовках.
    /// </summary>
    [Theory]
    [InlineData(null, "value")]
    [InlineData("bad-key", null)]
    [InlineData("", "value")]
    public async Task PublishAsync_WithInvalidHeaders_ThrowsKafkaPublishException(string? headerKey, string? headerValue)
    {
        // Arrange
        var payload = new { Test = "value" };
        var topic = "test-topic";
        var key = "test-key";
        _serializer.Serialize(payload).Returns("{}");
        var headers = new TestHeaders(
            ("valid-key", "ok"),
            (headerKey!, headerValue!));

        // Act
        Func<Task> act = async () => await _publisher.PublishAsync(
            topic,
            key,
            payload,
            headers);

        // Assert
        await act.Should().ThrowAsync<KafkaPublishException>();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _publisher?.Dispose();
        _producerFactory?.Dispose();
        _producer?.Dispose();
    }

    private sealed class TestHeaders : IReadOnlyDictionary<string, string>
    {
        private readonly IReadOnlyList<(string? Key, string? Value)> _pairs;

        public TestHeaders(params (string? Key, string? Value)[] pairs) => _pairs = pairs;

        public IEnumerable<string> Keys => throw new NotSupportedException();

        public IEnumerable<string> Values => throw new NotSupportedException();

        public int Count => _pairs.Count;

        public string this[string key] => throw new NotSupportedException();

        public bool ContainsKey(string key) => throw new NotSupportedException();

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            foreach (var (key, value) in _pairs)
            {
                yield return new KeyValuePair<string, string>(key!, value!);
            }
        }

        public bool TryGetValue(string key, out string value) => throw new NotSupportedException();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

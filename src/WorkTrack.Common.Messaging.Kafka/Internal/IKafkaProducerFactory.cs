using Confluent.Kafka;

namespace WorkTrack.Common.Messaging.Kafka.Internal;

/// <summary>
/// Фабрика Kafka-продюсеров.
/// </summary>
public interface IKafkaProducerFactory : IDisposable
{
    /// <summary>
    /// Возвращает продюсер Kafka.
    /// </summary>
    /// <returns>Экземпляр продюсера Kafka.</returns>
    IProducer<string, byte[]> GetProducer();
}

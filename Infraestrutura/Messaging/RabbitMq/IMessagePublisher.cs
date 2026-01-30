
namespace Infraestrutura.Messaging.RabbitMq
{
    public interface IMessagePublisher
    {
        Task PublishAsync<T>(
            string exchange,
            string routingKey,
            T message,
            IDictionary<string, object>? headers = null,
            CancellationToken ct = default);
    }
}

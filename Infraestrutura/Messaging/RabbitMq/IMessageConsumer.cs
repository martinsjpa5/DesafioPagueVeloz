
namespace Infraestrutura.Messaging.RabbitMq
{
    public interface IMessageConsumer
    {
        Task StartAsync(CancellationToken ct);
    }
}

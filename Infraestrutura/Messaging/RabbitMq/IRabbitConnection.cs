using RabbitMQ.Client;


namespace Infraestrutura.Messaging.RabbitMq
{
    public interface IRabbitConnection : IDisposable
    {
        IConnection GetConnection();
        IModel CreateChannel();
    }
}

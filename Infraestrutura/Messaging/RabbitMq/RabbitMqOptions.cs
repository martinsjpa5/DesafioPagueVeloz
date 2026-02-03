using System.Text.Json;


namespace Infraestrutura.Messaging.RabbitMq
{
    public sealed class RabbitMqOptions
    {
        public string HostName { get; init; } = "localhost";
        public int Port { get; init; } = 5672;
        public string UserName { get; init; } = "guest";
        public string Password { get; init; } = "guest";
        public string VirtualHost { get; init; } = "/";
        public bool UseSsl { get; init; } = false;

        public ushort PrefetchCount { get; init; } = 20;

        public bool PublisherConfirms { get; init; } = true;
        public TimeSpan PublishConfirmTimeout { get; init; } = TimeSpan.FromSeconds(5);

        public int ShardCount { get; init; } = 1;

        public JsonSerializerOptions JsonOptions { get; init; } = new(JsonSerializerDefaults.Web);
    }
}

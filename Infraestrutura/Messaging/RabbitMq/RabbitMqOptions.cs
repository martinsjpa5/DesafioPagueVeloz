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

        // Consumer QoS
        public ushort PrefetchCount { get; init; } = 20;

        // Publisher reliability
        public bool PublisherConfirms { get; init; } = true;
        public TimeSpan PublishConfirmTimeout { get; init; } = TimeSpan.FromSeconds(5);

        // Sharding
        public int ShardCount { get; init; } = 16;

        // JSON
        public JsonSerializerOptions JsonOptions { get; init; } = new(JsonSerializerDefaults.Web);
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Infraestrutura.Messaging.RabbitMq
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services, IConfiguration config)
        {
            var opt = config.GetSection("RabbitMq").Get<RabbitMqOptions>() ?? new RabbitMqOptions();

            services.AddSingleton(opt);
            services.AddSingleton<IRabbitConnection, RabbitConnection>();
            services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
            services.AddHealthChecks()
                .AddRabbitMQ(
                    rabbitConnectionString: opt.ToAmqpUri(),
                    name: "rabbitmq",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: new[] { "messaging", "rabbitmq" });

            return services;
        }
        private static string ToAmqpUri(this RabbitMqOptions opt)
        {
            var scheme = opt.UseSsl ? "amqps" : "amqp";
            var user = Uri.EscapeDataString(opt.UserName ?? "guest");
            var pass = Uri.EscapeDataString(opt.Password ?? "guest");

            var vhost = opt.VirtualHost;
            if (string.IsNullOrWhiteSpace(vhost))
                vhost = "/";

            if (!vhost.StartsWith("/"))
                vhost = "/" + vhost;

            var vhostEncoded = vhost == "/" ? "%2F" : Uri.EscapeDataString(vhost.TrimStart('/'));

            return $"{scheme}://{user}:{pass}@{opt.HostName}:{opt.Port}/{vhostEncoded}";
        }
    }
}

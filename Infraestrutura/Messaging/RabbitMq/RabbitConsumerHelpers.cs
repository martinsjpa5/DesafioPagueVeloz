using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Infraestrutura.Messaging.RabbitMq
{
    public static class RabbitConsumerHelpers
    {
        public static RabbitEnvelope<T> DeserializeEnvelope<T>(byte[] body, JsonSerializerOptions jsonOptions)
        {
            var json = Encoding.UTF8.GetString(body);
            return JsonSerializer.Deserialize<RabbitEnvelope<T>>(json, jsonOptions)
                   ?? throw new InvalidOperationException("Could not deserialize message envelope.");
        }

        public static int GetAttempts(IBasicProperties props)
        {
            if (props?.Headers is null) return 0;
            if (!props.Headers.TryGetValue(RabbitMqHeaders.Attempts, out var val) || val is null) return 0;

            return val switch
            {
                int i => i,
                long l => (int)l,
                byte[] b when int.TryParse(Encoding.UTF8.GetString(b), out var i) => i,
                _ => 0
            };
        }

        public static void Republish(
            IModel ch,
            string exchange,
            string routingKey,
            BasicDeliverEventArgs ea,
            int attempts,
            RabbitMqOptions opt)
        {
            var props = ch.CreateBasicProperties();
            props.Persistent = true;
            props.ContentType = ea.BasicProperties?.ContentType ?? "application/json";
            props.MessageId = ea.BasicProperties?.MessageId ?? Guid.NewGuid().ToString("N");
            props.CorrelationId = ea.BasicProperties?.CorrelationId ?? props.MessageId;

            props.Headers = ea.BasicProperties?.Headers is null
                ? new Dictionary<string, object>()
                : new Dictionary<string, object>(ea.BasicProperties.Headers);

            props.Headers[RabbitMqHeaders.Attempts] = attempts;

            ch.BasicPublish(exchange, routingKey, mandatory: true, basicProperties: props, body: ea.Body);

            if (opt.PublisherConfirms)
            {
                ch.ConfirmSelect();
                if (!ch.WaitForConfirms(opt.PublishConfirmTimeout))
                    throw new InvalidOperationException("Republish not confirmed within timeout.");
            }
        }
    }
}

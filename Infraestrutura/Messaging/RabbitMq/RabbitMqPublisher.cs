using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Infraestrutura.Messaging.RabbitMq
{
    public sealed class RabbitMqPublisher : IMessagePublisher
    {
        private readonly IRabbitConnection _conn;
        private readonly RabbitMqOptions _opt;

        public RabbitMqPublisher(IRabbitConnection conn, RabbitMqOptions opt)
        {
            _conn = conn;
            _opt = opt;
        }

        public Task PublishAsync<T>(
            string exchange,
            string routingKey,
            T message,
            IDictionary<string, object>? headers = null,
            CancellationToken ct = default)
        {
            using var ch = _conn.CreateChannel();

            if (_opt.PublisherConfirms)
                ch.ConfirmSelect();

            var messageId = Guid.NewGuid().ToString("N");
            var correlationId =
                headers?.TryGetValue("correlationId", out var c) == true
                    ? c?.ToString()
                    : null;

            correlationId ??= Guid.NewGuid().ToString("N");

            var envelope = new RabbitEnvelope<T>(
                Data: message,
                MessageId: messageId,
                CorrelationId: correlationId,
                CreatedAtUtc: DateTimeOffset.UtcNow,
                Headers: headers
            );

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(envelope, _opt.JsonOptions));

            var props = ch.CreateBasicProperties();
            props.Persistent = true;
            props.ContentType = "application/json";
            props.MessageId = envelope.MessageId;
            props.CorrelationId = envelope.CorrelationId;
            props.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            props.Headers ??= new Dictionary<string, object>();
            props.Headers[RabbitMqHeaders.MessageType] = typeof(T).FullName ?? typeof(T).Name;

            if (headers is not null)
            {
                foreach (var kv in headers)
                    props.Headers[kv.Key] = kv.Value;
            }

            ch.BasicPublish(
                exchange: exchange,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: props,
                body: body
            );

            if (_opt.PublisherConfirms)
            {
                if (!ch.WaitForConfirms(_opt.PublishConfirmTimeout))
                    throw new InvalidOperationException("RabbitMQ publish not confirmed within timeout.");
            }

            return Task.CompletedTask;
        }
    }
}

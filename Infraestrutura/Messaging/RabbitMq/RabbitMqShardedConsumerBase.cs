using RabbitMQ.Client.Events;

namespace Infraestrutura.Messaging.RabbitMq
{
    public abstract class RabbitMqShardedConsumerBase<T> : IMessageConsumer
    {
        private readonly IRabbitConnection _conn;
        private readonly RabbitMqOptions _opt;

        protected abstract string ExchangeBase { get; }
        protected abstract string RoutingKeyBase { get; }
        protected abstract string QueueBase { get; }

        protected virtual int MaxAttempts => 5;
        protected virtual int RetryTtlMs => 15_000;

        protected RabbitMqShardedConsumerBase(IRabbitConnection conn, RabbitMqOptions opt)
        {
            _conn = conn;
            _opt = opt;
        }

        protected abstract Task HandleAsync(RabbitEnvelope<T> message, CancellationToken ct);

        public Task StartAsync(CancellationToken ct)
        {
            var ch = _conn.CreateChannel();

            RabbitMqShardedTopology.EnsureShardedTopology(
                ch, _opt, ExchangeBase, RoutingKeyBase, QueueBase, RetryTtlMs);

            ch.BasicQos(prefetchSize: 0, prefetchCount: _opt.PrefetchCount, global: false);

            var consumer = new AsyncEventingBasicConsumer(ch);

            consumer.Received += async (_, ea) =>
            {
                try
                {
                    var envelope = RabbitConsumerHelpers.DeserializeEnvelope<T>(ea.Body.ToArray(), _opt.JsonOptions);
                    await HandleAsync(envelope, ct);
                    ch.BasicAck(ea.DeliveryTag, multiple: false);
                }
                catch
                {
                    var attempts = RabbitConsumerHelpers.GetAttempts(ea.BasicProperties) + 1;

                    if (attempts >= MaxAttempts)
                        RabbitConsumerHelpers.Republish(ch, $"{ExchangeBase}.dlx", ea.RoutingKey, ea, attempts, _opt);
                    else
                        RabbitConsumerHelpers.Republish(ch, $"{ExchangeBase}.retry", ea.RoutingKey, ea, attempts, _opt);

                    ch.BasicAck(ea.DeliveryTag, multiple: false);
                }
            };

            for (var shard = 0; shard < _opt.ShardCount; shard++)
            {
                var queue = $"{QueueBase}.shard-{shard}.queue";
                ch.BasicConsume(
              queue: queue,
             autoAck: false,
              consumerTag: "",
              noLocal: false,
             exclusive: false,
             arguments: null,
            consumer: consumer
            );
            }

            return Task.CompletedTask;
        }
    }
}

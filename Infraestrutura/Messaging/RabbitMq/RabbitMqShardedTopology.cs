using RabbitMQ.Client;

namespace Infraestrutura.Messaging.RabbitMq
{
    public static class RabbitMqShardedTopology
    {
        public static void EnsureShardedTopology(
            IModel ch,
            RabbitMqOptions opt,
            string exchangeBase,
            string routingKeyBase,
            string queueBase,
            int retryTtlMs = 15_000)
        {
            ch.ExchangeDeclare(exchange: exchangeBase, type: ExchangeType.Direct, durable: true, autoDelete: false);

            var retryExchange = $"{exchangeBase}.retry";
            var dlxExchange = $"{exchangeBase}.dlx";

            ch.ExchangeDeclare(retryExchange, ExchangeType.Direct, durable: true, autoDelete: false);
            ch.ExchangeDeclare(dlxExchange, ExchangeType.Direct, durable: true, autoDelete: false);

            for (var shard = 0; shard < opt.ShardCount; shard++)
            {
                var routingKey = $"{routingKeyBase}.shard-{shard}";
                var queue = $"{queueBase}.shard-{shard}.queue";

                var retryQueue = $"{queue}.retry";
                var dlq = $"{queue}.dlq";

                // Main shard queue
                var qArgs = new Dictionary<string, object>
                {
                    ["x-single-active-consumer"] = true
                };

                ch.QueueDeclare(queue: queue, durable: true, exclusive: false, autoDelete: false, arguments: qArgs);
                ch.QueueBind(queue: queue, exchange: exchangeBase, routingKey: routingKey);

                // Retry queue (TTL -> dead-letter back to main exchange/routingKey)
                var retryArgs = new Dictionary<string, object>
                {
                    ["x-message-ttl"] = retryTtlMs,
                    ["x-dead-letter-exchange"] = exchangeBase,
                    ["x-dead-letter-routing-key"] = routingKey,
                    ["x-single-active-consumer"] = true
                };

                ch.QueueDeclare(queue: retryQueue, durable: true, exclusive: false, autoDelete: false, arguments: retryArgs);
                ch.QueueBind(queue: retryQueue, exchange: retryExchange, routingKey: routingKey);

                // DLQ
                var dlqArgs = new Dictionary<string, object>
                {
                    ["x-single-active-consumer"] = true
                };

                ch.QueueDeclare(queue: dlq, durable: true, exclusive: false, autoDelete: false, arguments: dlqArgs);
                ch.QueueBind(queue: dlq, exchange: dlxExchange, routingKey: routingKey);
            }
        }
    }
}
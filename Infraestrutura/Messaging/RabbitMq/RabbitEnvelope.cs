
namespace Infraestrutura.Messaging.RabbitMq
{
    public sealed record RabbitEnvelope<T>(
    T Data,
    string MessageId,
    string CorrelationId,
    DateTimeOffset CreatedAtUtc,
    IDictionary<string, object>? Headers = null
);
}

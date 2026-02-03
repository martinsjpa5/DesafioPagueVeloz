
using Infraestrutura.Messaging.RabbitMq;

namespace WorkerTransacao;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IEnumerable<IMessageConsumer> _consumers;

    public Worker(ILogger<Worker> logger, IEnumerable<IMessageConsumer> consumers)
    {
        _logger = logger;
        _consumers = consumers;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker started at: {time}", DateTimeOffset.Now);

        foreach (var c in _consumers)
            c.StartAsync(stoppingToken);

        return Task.Delay(Timeout.Infinite, stoppingToken);
    }
}

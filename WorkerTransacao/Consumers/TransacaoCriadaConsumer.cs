using Domain.Events;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services;
using Infraestrutura.EntidadeBaseFramework.Repositories;
using Infraestrutura.Messaging.RabbitMq;

namespace WorkerTransacao.Consumers
{
    public sealed class TransacaoCriadaConsumer : RabbitMqShardedConsumerBase<TransacaoCriadaEvent>
    {
        private readonly ILogger<TransacaoCriadaConsumer> _logger;
        private readonly IServiceScopeFactory _scopeFactory;



        public TransacaoCriadaConsumer(IRabbitConnection conn, RabbitMqOptions opt, ILogger<TransacaoCriadaConsumer> logger, IServiceScopeFactory scopeFactory)
            : base(conn, opt)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override string ExchangeBase => "transacoes.exchange";
        protected override string RoutingKeyBase => "transacoes";
        protected override string QueueBase => "transacoes";

        protected override async Task HandleAsync(RabbitEnvelope<TransacaoCriadaEvent> message, CancellationToken ct)
        {
            _logger.LogInformation(
                "Consumiu TransacaoCriada: {TransacaoId} Corr {Corr}",
                message.Data.TransacaoId, message.CorrelationId);

            using var scope = _scopeFactory.CreateScope();
            IEfBaseRepository efBaseRepository = scope.ServiceProvider.GetRequiredService<IEfBaseRepository>();
            IProcessadorTransacaoDomainService processadorTransacao = scope.ServiceProvider.GetRequiredService<IProcessadorTransacaoDomainService>();
            ITransacaoRepository transacaoRepository = scope.ServiceProvider.GetRequiredService<ITransacaoRepository>();


            var transacao = await transacaoRepository.ObterTransacaoPendenteAsync(message.Data.TransacaoId);

            if (transacao == null) return;

            processadorTransacao.Processar(transacao);


            await efBaseRepository.SalvarAlteracoesAsync();

            return;
        }
    }
}

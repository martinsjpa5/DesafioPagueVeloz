using Domain.Entities;
using Domain.Events;
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
            var efBaseRepository = scope.ServiceProvider.GetRequiredService<IEfBaseRepository>();


            var transacao = await efBaseRepository.ObterPorCondicaoAsync<Transacao>(trans => trans.Id == message.Data.TransacaoId && trans.Status == Domain.Enums.StatusTransacaoEnum.PENDENTE);

            if (transacao == null) return;

            efBaseRepository.RastrearEntidadeBase(transacao);
            transacao.Status = Domain.Enums.StatusTransacaoEnum.SUCESSO;

            await efBaseRepository.SalvarAlteracoesAsync();

            return;
        }
    }
}

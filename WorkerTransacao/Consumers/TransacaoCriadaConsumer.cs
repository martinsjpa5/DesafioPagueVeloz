using Domain.Base;
using Domain.Events;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services;
using Domain.Models;
using Infraestrutura.EntidadeBaseFramework.Repositories;
using Infraestrutura.Messaging.RabbitMq;

namespace WorkerTransacao.Consumers
{
    public sealed class TransacaoCriadaConsumer : RabbitMqShardedConsumerBase<TransacaoCriadaEvent>
    {
        private readonly ILogger<TransacaoCriadaConsumer> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ICommonCachingRepository _commonCachingRepository; 



        public TransacaoCriadaConsumer(IRabbitConnection conn, RabbitMqOptions opt, ILogger<TransacaoCriadaConsumer> logger, IServiceScopeFactory scopeFactory, ICommonCachingRepository commonCachingRepository)
            : base(conn, opt)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _commonCachingRepository = commonCachingRepository;
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

            DomainPattern result = processadorTransacao.Processar(transacao);


            await efBaseRepository.SalvarAlteracoesAsync();

            if (result.Sucesso)
            {
                ContaModel contaCacheOrigem = new() { ClienteId = transacao.ContaOrigem.ClienteId, ContaId = transacao.ContaOrigemId };
                await _commonCachingRepository.RemoveAsync(contaCacheOrigem);

                if (transacao.ContaDestinoId != null)
                {
                    ContaModel contaCacheDestino = new() { ClienteId = transacao.ContaDestino.ClienteId, ContaId = transacao.ContaDestinoId.Value };
                    await _commonCachingRepository.RemoveAsync(contaCacheDestino);
                }
            }

            return;
        }
    }
}

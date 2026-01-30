
using Application.Dtos.Base;
using Application.Dtos.Requests;
using Application.Dtos.Responses;
using Application.Interfaces.Context;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Events;
using Infraestrutura.EntidadeBaseFramework.Repositories;
using Infraestrutura.Messaging.RabbitMq;
using Microsoft.AspNetCore.Http;

namespace Application.Services
{
    public class TransacaoService : ITransacaoService
    {
        private readonly IEfBaseRepository _efBaseRepository;
        private readonly IUserContext _userContext;
        private readonly IMessagePublisher _publisher;
        private readonly RabbitMqOptions _opt;

        public TransacaoService(IEfBaseRepository efBaseRepository, IUserContext userContext, IMessagePublisher publisher, RabbitMqOptions opt)
        {
            _efBaseRepository = efBaseRepository;
            _userContext = userContext;
            _publisher = publisher;
            _opt = opt;
        }

        public async Task<ResultPatternGeneric<CriarTransacaoResponse>> CriarTransacaoAsync(CriarTransacaoRequest request, string correlationId)
        {
            int clienteId = _userContext.ClienteId;

            Conta? conta = await _efBaseRepository.ObterPorCondicaoAsync<Conta>(conta => conta.ClienteId == clienteId && conta.Id == request.ContaId);

            if (conta == null)
            {
                return ResultPatternGeneric<CriarTransacaoResponse>.ErroBuilder("Conta não encontrada!");
            }

            Transacao transacao = new() { ContaId = request.ContaId, Moeda = request.Moeda, Quantia = request.Quantia, Tipo = (TipoOperacaoEnum)request.Operacao, Status = StatusTransacaoEnum.PENDENTE  };

            await _efBaseRepository.AdicionarEntidadeBaseAsync(transacao);

            await _efBaseRepository.SalvarAlteracoesAsync();

            var response = new CriarTransacaoResponse()
            {
                Data = transacao.DataCriacao,
                Id = transacao.Id,
                MensagemErro = transacao.MensagemErro,
                SaldoDisponivel = conta.SaldoDisponivel,
                SaldoReservado = conta.SaldoReservado,
                SaldoTotal = conta.SaldoReservado + conta.SaldoDisponivel
            };


            var evt = new TransacaoCriadaEvent() { TransacaoId = transacao.Id };

            var shard = ShardRouter.CalculateShard(clienteId.ToString(), _opt.ShardCount);

            var exchange = "transacoes.exchange";
            var routingKey = $"transacoes.shard-{shard}";

            await _publisher.PublishAsync(exchange, routingKey, evt, headers: new Dictionary<string, object>
            {
                ["correlationId"] = correlationId
            });

            return ResultPatternGeneric<CriarTransacaoResponse>.SucessoBuilder(response);
        }

        public Task<ResultPattern> ExecutarTransacaoAsync()
        {
            throw new NotImplementedException();
        }
    }
}

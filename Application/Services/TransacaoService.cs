
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
using Microsoft.EntityFrameworkCore;

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

            Conta? contaOrigem = await _efBaseRepository.ObterPorCondicaoAsync<Conta>(conta => conta.ClienteId == clienteId && conta.Id == request.ContaOrigemId);

            if (contaOrigem == null)
            {
                return ResultPatternGeneric<CriarTransacaoResponse>.ErroBuilder("Conta Origem não encontrada!");
            }

            if (request.Operacao == (int)TipoOperacaoEnum.Transferencia)
            {
                if (request.ContaDestinoId == null || request.ContaDestinoId == 0)
                {
                    return ResultPatternGeneric<CriarTransacaoResponse>.ErroBuilder("Conta Destino é Obrigatorio para Transferencia!");
                }

                Conta? contaDestino = await _efBaseRepository.ObterPorCondicaoAsync<Conta>(x => x.Id == request.ContaDestinoId);
                if (contaDestino == null)
                {
                    return ResultPatternGeneric<CriarTransacaoResponse>.ErroBuilder("Conta Origem não encontrada!");
                }
            }
            else if (request.Operacao == (int)TipoOperacaoEnum.Estorno)
            {
                if(request.TransacaoEstornadaId == null || request.TransacaoEstornadaId == 0)
                {
                    return ResultPatternGeneric<CriarTransacaoResponse>.ErroBuilder("O Id da Transacao que você quer estornar é obrigatório");
                }
                var transacaoEstornada = await _efBaseRepository.ObterPorCondicaoAsync<Transacao>(x => x.TransacaoEstornadaId == request.TransacaoEstornadaId && x.Status == StatusTransacaoEnum.SUCESSO && x.Tipo == TipoOperacaoEnum.Estorno);
                if (transacaoEstornada != null)
                {
                    return ResultPatternGeneric<CriarTransacaoResponse>.ErroBuilder("A Transação que você está tentando estornar já foi estornada");
                }
            }

            Transacao transacao = new()
            {
                ContaOrigemId = request.ContaOrigemId,
                Moeda = request.Moeda,
                Quantia = request.Quantia,
                Tipo = (TipoOperacaoEnum)request.Operacao,
                Status = StatusTransacaoEnum.PENDENTE,
                ContaDestinoId = request.ContaDestinoId,
                TransacaoEstornadaId = request.TransacaoEstornadaId,
            };

            await _efBaseRepository.AdicionarEntidadeBaseAsync(transacao);

            await _efBaseRepository.SalvarAlteracoesAsync();

            CriarTransacaoResponse response = new()
            {
                Data = transacao.DataCriacao,
                Id = transacao.Id,
                MensagemErro = transacao.MensagemErro,
                SaldoDisponivel = contaOrigem.SaldoDisponivel,
                SaldoReservado = contaOrigem.SaldoReservado,
                SaldoTotal = contaOrigem.SaldoReservado + contaOrigem.SaldoDisponivel
            };


            TransacaoCriadaEvent evt = new() { TransacaoId = transacao.Id };

            int shard = ShardRouter.CalculateShard(clienteId.ToString(), _opt.ShardCount);

            string exchange = "transacoes.exchange";
            string routingKey = $"transacoes.shard-{shard}";

            await _publisher.PublishAsync(exchange, routingKey, evt, headers: new Dictionary<string, object>
            {
                ["correlationId"] = correlationId
            });

            return ResultPatternGeneric<CriarTransacaoResponse>.SucessoBuilder(response);
        }

        public async Task<ResultPatternGeneric<IEnumerable<ObterTransacaoResponse>>> ObterTransacoesPassiveisDeEstornoUsuarioLogadoAsync(int contaId)
        {
            var transacoes = await _efBaseRepository.ObterTodosPorCondicaoAsync<Transacao>(x => x.ContaOrigemId == contaId && x.ContaOrigem.ClienteId == _userContext.ClienteId
            && x.Status == StatusTransacaoEnum.SUCESSO && x.TransacaoEstornadaId == null,
                trans => trans.Include(x => x.ContaDestino).ThenInclude(y => y.Cliente));


            var transacoesEstornadas = await _efBaseRepository.ObterTodosPorCondicaoAsync<Transacao>(x => x.ContaOrigemId == contaId && x.ContaOrigem.ClienteId == _userContext.ClienteId
            && x.Status == StatusTransacaoEnum.SUCESSO && x.TransacaoEstornadaId != null);

            var idsJaEstornados = transacoesEstornadas
                                .Select(t => t.TransacaoEstornadaId!.Value)
                                .ToHashSet();

            var transacoesComPossibilidadeDeEstorno = transacoes
                .Where(t => t.TransacaoEstornadaId == null)
                .Where(t => !idsJaEstornados.Contains(t.Id))
                .ToList();


            var response = transacoesComPossibilidadeDeEstorno.Select(x => new ObterTransacaoResponse
            {
                TransacaoEstornadaId = x.TransacaoEstornadaId,
                ContaDestinoId = x.ContaDestinoId,
                Moeda = x.Moeda,
                Id = x.Id,
                NomeClienteContaDestino = x.ContaDestino?.Cliente.Nome,
                Quantia = x.Quantia,
                Status = x.Status.ToString(),
                Tipo = x.Tipo.ToString(),
            });

            return ResultPatternGeneric<IEnumerable<ObterTransacaoResponse>>.SucessoBuilder(response);
        }

        public async Task<ResultPatternGeneric<IEnumerable<ObterTransacaoResponse>>> ObterTransacoesUsuarioLogadoAsync(int contaId)
        {
            var transacoes = await _efBaseRepository.ObterTodosPorCondicaoAsync<Transacao>(x => (x.ContaOrigemId == contaId && x.ContaOrigem.ClienteId == _userContext.ClienteId) || (x.ContaDestinoId == contaId && x.ContaDestino.ClienteId == _userContext.ClienteId),
                trans => trans.Include(x => x.ContaDestino).ThenInclude(y => y.Cliente));

            var response = transacoes.Select(x => new ObterTransacaoResponse
            {
                TransacaoEstornadaId = x.TransacaoEstornadaId,
                ContaDestinoId = x.ContaDestinoId,
                Moeda = x.Moeda,
                Id = x.Id,
                NomeClienteContaDestino = x.ContaDestino?.Cliente.Nome,
                Quantia = x.Quantia,
                Status = x.Status.ToString(),
                Tipo = x.Tipo.ToString(),
            });

            return ResultPatternGeneric<IEnumerable<ObterTransacaoResponse>>.SucessoBuilder(response);
        }
    }
}

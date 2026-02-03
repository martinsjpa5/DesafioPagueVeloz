
using Application.Dtos.Base;
using Application.Dtos.Requests;
using Application.Dtos.Responses;
using Application.Interfaces.Context;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Events;
using Domain.Interfaces.Repositories;
using Infraestrutura.EntidadeBaseFramework.Repositories;
using Infraestrutura.Messaging.RabbitMq;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public class TransacaoService : ITransacaoService
    {
        private readonly IEfBaseRepository _efBaseRepository;
        private readonly IUserContext _userContext;
        private readonly IMessagePublisher _publisher;
        private readonly RabbitMqOptions _opt;
        private readonly ITransacaoRepository _transacaoRepository;

        public TransacaoService(IEfBaseRepository efBaseRepository, IUserContext userContext, IMessagePublisher publisher, RabbitMqOptions opt, ITransacaoRepository transacaoRepository)
        {
            _efBaseRepository = efBaseRepository;
            _userContext = userContext;
            _publisher = publisher;
            _opt = opt;
            _transacaoRepository = transacaoRepository;
        }

        public async Task<ResultPatternGeneric<CriarTransacaoResponse>> CriarTransacaoAsync(
    CriarTransacaoRequest request,
    string correlationId)
        {
            var clienteId = _userContext.ClienteId;

            var operacao = (TipoOperacaoEnum)request.Operacao;

            if (request.ContaOrigemId <= 0)
                return ResultPatternGeneric<CriarTransacaoResponse>.ErroBuilder("Conta Origem é obrigatória.");

            if (request.Quantia <= 0 && request.Operacao != (int)TipoOperacaoEnum.Estorno)
                return ResultPatternGeneric<CriarTransacaoResponse>.ErroBuilder("Quantia deve ser maior que zero.");

            var contaOrigem = await ObterContaOrigemDoClienteAsync(clienteId, request.ContaOrigemId);
            if (contaOrigem is null)
                return ResultPatternGeneric<CriarTransacaoResponse>.ErroBuilder("Conta Origem não encontrada!");

            Transacao? transacaoOriginalParaEstorno = null;

            switch (operacao)
            {
                case TipoOperacaoEnum.Transferencia:
                    {
                        var erro = await ValidarTransferenciaAsync(request);
                        if (erro is not null)
                            return ResultPatternGeneric<CriarTransacaoResponse>.ErroBuilder(erro);

                        break;
                    }

                case TipoOperacaoEnum.Estorno:
                    {
                        var resultado = await ValidarECarregarDadosDeEstornoAsync(clienteId, request);
                        if (!resultado.Sucesso)
                            return ResultPatternGeneric<CriarTransacaoResponse>.ErroBuilder(resultado.MensagemErro!);

                        transacaoOriginalParaEstorno = resultado.TransacaoOriginal!;
                        contaOrigem = resultado.ContaOrigemAutorizada!;
                        break;
                    }

                default:
                    break;
            }

            var transacao = CriarEntidadeTransacao(request, operacao, transacaoOriginalParaEstorno);

            await _efBaseRepository.AdicionarEntidadeBaseAsync(transacao);
            await _efBaseRepository.SalvarAlteracoesAsync();

            var response = MapearResponse(transacao, contaOrigem);

            await PublicarEventoTransacaoCriadaAsync(clienteId, transacao.Id, correlationId);

            return ResultPatternGeneric<CriarTransacaoResponse>.SucessoBuilder(response);
        }

        private async Task<Conta?> ObterContaOrigemDoClienteAsync(int clienteId, int contaOrigemId)
        {
            return await _efBaseRepository.ObterPorCondicaoAsync<Conta>(
                conta => conta.ClienteId == clienteId && conta.Id == contaOrigemId);
        }

        private async Task<string?> ValidarTransferenciaAsync(CriarTransacaoRequest request)
        {
            if (request.ContaDestinoId is null || request.ContaDestinoId <= 0)
                return "Conta Destino é obrigatória para Transferência!";

            var contaDestino = await _efBaseRepository.ObterPorCondicaoAsync<Conta>(x => x.Id == request.ContaDestinoId);
            if (contaDestino is null)
                return "Conta Destino não encontrada!";

            if (request.ContaDestinoId == request.ContaOrigemId)
                return "Conta Destino não pode ser igual à Conta Origem.";

            return null;
        }

        private sealed record ValidacaoEstornoResult(
            bool Sucesso,
            string? MensagemErro,
            Transacao? TransacaoOriginal,
            Conta? ContaOrigemAutorizada);

        private async Task<ValidacaoEstornoResult> ValidarECarregarDadosDeEstornoAsync(int clienteId, CriarTransacaoRequest request)
        {
            if (request.TransacaoEstornadaId is null || request.TransacaoEstornadaId <= 0)
                return new(false, "O Id da Transação que você quer estornar é obrigatório.", null, null);

            var transacaoOriginal = await _efBaseRepository.ObterPorCondicaoAsync<Transacao>(x =>
                x.Id == request.TransacaoEstornadaId
                && x.Status == StatusTransacaoEnum.SUCESSO
                && x.Tipo != TipoOperacaoEnum.Estorno);

            if (transacaoOriginal is null)
                return new(false, "Transação para estorno não encontrada ou não está com SUCESSO.", null, null);

            var contaOrigemAutorizada = await _efBaseRepository.ObterPorCondicaoAsync<Conta>(c =>
                c.Id == transacaoOriginal.ContaOrigemId && c.ClienteId == clienteId);

            if (contaOrigemAutorizada is null)
                return new(false, "Você não tem permissão para estornar essa transação.", null, null);

            var estornoJaExiste = await _efBaseRepository.ObterPorCondicaoAsync<Transacao>(x =>
                x.TransacaoEstornadaId == transacaoOriginal.Id
                && x.Status == StatusTransacaoEnum.SUCESSO
                && x.Tipo == TipoOperacaoEnum.Estorno);

            if (estornoJaExiste is not null)
                return new(false, "A Transação que você está tentando estornar já foi estornada.", null, null);

            return new(true, null, transacaoOriginal, contaOrigemAutorizada);
        }

        private static Transacao CriarEntidadeTransacao(
            CriarTransacaoRequest request,
            TipoOperacaoEnum operacao,
            Transacao? transacaoOriginalParaEstorno)
        {
            var transacao = new Transacao
            {
                Status = StatusTransacaoEnum.PENDENTE
            };

            if (operacao == TipoOperacaoEnum.Estorno)
            {
                transacao.ContaOrigemId = transacaoOriginalParaEstorno!.ContaOrigemId;
                transacao.ContaDestinoId = transacaoOriginalParaEstorno.ContaDestinoId;
                transacao.Moeda = transacaoOriginalParaEstorno.Moeda;
                transacao.Quantia = transacaoOriginalParaEstorno.Quantia;
                transacao.Tipo = TipoOperacaoEnum.Estorno;
                transacao.TransacaoEstornadaId = transacaoOriginalParaEstorno.Id;

                return transacao;
            }

            transacao.ContaOrigemId = request.ContaOrigemId;
            transacao.ContaDestinoId = request.ContaDestinoId;
            transacao.Moeda = request.Moeda;
            transacao.Quantia = request.Quantia;
            transacao.Tipo = operacao;
            transacao.TransacaoEstornadaId = request.TransacaoEstornadaId;

            return transacao;
        }

        private static CriarTransacaoResponse MapearResponse(Transacao transacao, Conta contaOrigem)
        {
            var saldoTotal = contaOrigem.SaldoReservado + contaOrigem.SaldoDisponivel;

            return new CriarTransacaoResponse
            {
                Data = transacao.DataCriacao,
                Id = transacao.Id,
                MensagemErro = transacao.MensagemErro,
                SaldoDisponivel = contaOrigem.SaldoDisponivel,
                SaldoReservado = contaOrigem.SaldoReservado,
                SaldoTotal = saldoTotal
            };
        }

        private async Task PublicarEventoTransacaoCriadaAsync(int clienteId, int transacaoId, string correlationId)
        {
            var evt = new TransacaoCriadaEvent { TransacaoId = transacaoId };

            var shard = ShardRouter.CalculateShard(clienteId.ToString(), _opt.ShardCount);
            var exchange = "transacoes.exchange";
            var routingKey = $"transacoes.shard-{shard}";

            await _publisher.PublishAsync(exchange, routingKey, evt, headers: new Dictionary<string, object>
            {
                ["correlationId"] = correlationId
            });


        }


        public async Task<ResultPatternGeneric<IEnumerable<ObterTransacaoResponse>>> ObterTransacoesPassiveisDeEstornoUsuarioLogadoAsync(int contaId)
        {
            int clienteId = _userContext.ClienteId;

            var transacoes = await _transacaoRepository
                .ObterTransacoesPassiveisDeEstornoAsync(contaId, clienteId);

            var response = transacoes.Select(x => new ObterTransacaoResponse
            {
                TransacaoEstornadaId = x.TransacaoEstornadaId,
                ContaDestinoId = x.ContaDestinoId,
                Moeda = x.Moeda,
                Id = x.Id,
                NomeClienteContaDestino = x.ContaDestino?.Cliente?.Nome,
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
                MensagemErro = x.MensagemErro,

            });

            return ResultPatternGeneric<IEnumerable<ObterTransacaoResponse>>.SucessoBuilder(response);
        }
    }
}

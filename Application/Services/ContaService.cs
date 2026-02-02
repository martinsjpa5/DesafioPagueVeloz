
using Application.Dtos.Base;
using Application.Dtos.Requests;
using Application.Dtos.Responses;
using Application.Interfaces.Context;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces.Repositories;
using Domain.Models;
using Infraestrutura.EntidadeBaseFramework.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public class ContaService : IContaService
    {
        private readonly IEfBaseRepository _efBaseRepository;
        private readonly IUserContext _userContext;
        private readonly ICommonCachingRepository _commonCachingRepository;

        public ContaService(IEfBaseRepository efBaseRepository, IUserContext userContext, ICommonCachingRepository commonCachingRepository)
        {
            _efBaseRepository = efBaseRepository;
            _userContext = userContext;
            _commonCachingRepository = commonCachingRepository;
        }

        public async Task<ResultPatternGeneric<IEnumerable<ObterContaParaTransferenciaResponse>>> ObterContasParaTransferenciaAsync(int contaId)
        {
            ICollection<Conta> contas = await _efBaseRepository.ObterTodosPorCondicaoAsync<Conta>(conta => conta.Id != contaId && conta.Status == StatusContaEnum.Ativa, x => x.Include(y => y.Cliente));

            IEnumerable<ObterContaParaTransferenciaResponse> contasResponse = contas.Select(conta => new ObterContaParaTransferenciaResponse
            {
                Id = conta.Id,
                NomeCliente = conta.Cliente.Nome
            });

            return ResultPatternGeneric<IEnumerable<ObterContaParaTransferenciaResponse>>.SucessoBuilder(contasResponse);
        }


        public async Task<ResultPatternGeneric<ObterClienteResponse>> ObterPorUsuarioLogadoAsync()
        {
            ICollection<Conta> contas = await _efBaseRepository.ObterTodosPorCondicaoAsync<Conta>(conta => conta.ClienteId == _userContext.ClienteId, conta => conta.Include(x => x.Cliente));

            ObterClienteResponse clienteResponse = new() { Nome = contas.FirstOrDefault()?.Cliente.Nome, Contas = contas.Select(x => x.Id).ToList() };

            return ResultPatternGeneric<ObterClienteResponse>.SucessoBuilder(clienteResponse);
        }

        public async Task<ResultPatternGeneric<ObterContaResponse>> ObterPorIdUsuarioLogadoAsync(int contaId)
        {
            ContaModel? contaCache = new() { ClienteId = _userContext.ClienteId, ContaId = contaId };

            contaCache = await _commonCachingRepository.GetAsync<ContaModel>(contaCache.ObterKey());

            Conta? conta = null;

            if (contaCache == null)
            {
                conta = await _efBaseRepository.ObterPorCondicaoAsync<Conta>(conta => conta.ClienteId == _userContext.ClienteId && conta.Id == contaId);

                if (conta == null)
                {
                    return ResultPatternGeneric<ObterContaResponse>.ErroBuilder("Conta não Encontrada!");
                }

                contaCache = new()
                {
                    ClienteId = _userContext.ClienteId,
                    ContaId = contaId,
                    LimiteDeCredito = conta.LimiteDeCredito,
                    SaldoDisponivel = conta.SaldoDisponivel,
                    SaldoReservado = conta.SaldoReservado,
                    Status = conta.Status.ToString()
                };

                await _commonCachingRepository.SetAsync(contaCache, TimeSpan.FromDays(1));
            }

            ObterContaResponse response = new()
            {
                Id = contaId,
                LimiteDeCredito = contaCache.LimiteDeCredito,
                SaldoDisponivel = contaCache.SaldoDisponivel,
                SaldoReservado = contaCache.SaldoReservado,
                Status = contaCache.Status
            };

            return ResultPatternGeneric<ObterContaResponse>.SucessoBuilder(response);

        }

        public async Task<ResultPattern> RegistrarPorUsuarioLogadoAsync(RegistrarContaRequest request)
        {
            Conta conta = new()
            {
                ClienteId = _userContext.ClienteId,
                LimiteDeCredito = request.LimiteDeCredito,
                SaldoDisponivel = request.SaldoInicial,
                Status = StatusContaEnum.Ativa

            };
            await _efBaseRepository.AdicionarEntidadeBaseAsync(conta);
            await _efBaseRepository.SalvarAlteracoesAsync();

            return ResultPattern.SucessoBuilder();

        }
    }
}

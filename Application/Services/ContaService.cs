
using Application.Dtos.Base;
using Application.Dtos.Requests;
using Application.Dtos.Responses;
using Application.Interfaces.Context;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Infraestrutura.EntidadeBaseFramework.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public class ContaService : IContaService
    {
        private readonly IEfBaseRepository _efBaseRepository;
        private readonly IUserContext _userContext;

        public ContaService(IEfBaseRepository efBaseRepository, IUserContext userContext)
        {
            _efBaseRepository = efBaseRepository;
            _userContext = userContext;
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
            ICollection<Conta> contas = await _efBaseRepository.ObterTodosPorCondicaoAsync<Conta>(conta => conta.ClienteId == _userContext.ClienteId);
            Cliente? cliente = await _efBaseRepository.ObterPorCondicaoAsync<Cliente>(cliente => cliente.Id == _userContext.ClienteId);

            IEnumerable<ObterContaResponse> contaResponse = contas.Select(conta => new ObterContaResponse
            {
                Id = conta.Id,
                LimiteDeCredito = conta.LimiteDeCredito,
                SaldoDisponivel = conta.SaldoDisponivel,
                SaldoReservado = conta.SaldoReservado,
                Status = conta.Status.ToString()
            });

            ObterClienteResponse clienteResponse = new() { Nome = cliente.Nome, Contas = contaResponse };

            return ResultPatternGeneric<ObterClienteResponse>.SucessoBuilder(clienteResponse);
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

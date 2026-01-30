
using Application.Dtos.Base;
using Application.Dtos.Requests;
using Application.Dtos.Responses;
using Application.Interfaces.Context;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Infraestrutura.EntidadeBaseFramework.Repositories;

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

        public async Task<ResultPatternGeneric<IEnumerable<ObterContaResponse>>> ObterPorUsuarioLogadoAsync()
        {
            ICollection<Conta> contas = await _efBaseRepository.ObterTodosPorCondicaoAsync<Conta>(conta => conta.ClienteId == _userContext.ClienteId);

            IEnumerable<ObterContaResponse> response = contas.Select(conta => new ObterContaResponse
            {
                Id = conta.Id,
                LimiteDeCredito = conta.LimiteDeCredito,
                SaldoDisponivel = conta.SaldoDisponivel,
                SaldoReservado = conta.SaldoReservado,
                Status = conta.Status.ToString()
            });

            return ResultPatternGeneric<IEnumerable<ObterContaResponse>>.SucessoBuilder(response);
        }

        public async Task<ResultPattern> RegistrarPorUsuarioLogadoAsync(RegistrarContaRequest request)
        {
            var conta = new Conta()
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

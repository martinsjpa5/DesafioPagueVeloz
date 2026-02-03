
using Application.Dtos.Base;
using Application.Dtos.Requests;
using Application.Dtos.Responses;

namespace Application.Interfaces.Services
{
    public interface ITransacaoService
    {
        public Task<ResultPatternGeneric<CriarTransacaoResponse>> CriarTransacaoAsync(CriarTransacaoRequest request, string correlationId);
        Task<ResultPatternGeneric<IEnumerable<ObterTransacaoResponse>>> ObterTransacoesPassiveisDeEstornoUsuarioLogadoAsync(int contaId);
        Task<ResultPatternGeneric<IEnumerable<ObterTransacaoResponse>>> ObterTransacoesUsuarioLogadoAsync(int contaId);
    }
}

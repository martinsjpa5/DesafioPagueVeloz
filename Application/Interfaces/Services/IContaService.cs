using Application.Dtos.Base;
using Application.Dtos.Requests;
using Application.Dtos.Responses;

namespace Application.Interfaces.Services
{
    public interface IContaService
    {
        Task<ResultPattern> RegistrarPorUsuarioLogadoAsync(RegistrarContaRequest request);
        Task<ResultPatternGeneric<ObterClienteResponse>> ObterPorUsuarioLogadoAsync();
        Task<ResultPatternGeneric<IEnumerable<ObterContaParaTransferenciaResponse>>> ObterContasParaTransferenciaAsync(int contaId);
        Task<ResultPatternGeneric<ObterContaResponse>> ObterPorIdUsuarioLogadoAsync(int contaId);
    }
}


using Application.Dtos.Base;
using Application.Dtos.Requests;
using Application.Dtos.Responses;

namespace Application.Interfaces.Services
{
    public interface ITransacaoService
    {
        public Task<ResultPatternGeneric<CriarTransacaoResponse>> CriarTransacaoAsync(CriarTransacaoRequest request, string correlationId);
        public Task<ResultPattern> ExecutarTransacaoAsync();
    }
}

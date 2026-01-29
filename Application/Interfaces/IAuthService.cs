
using Application.Dtos.Base;
using Application.Dtos.Requests;

namespace Application.Interfaces
{
    public interface IAuthService
    {
        public Task<ResultPattern> RegistrarAsync(RegistroRequest request);
        public Task<ResultPatternGeneric<string>> LogarAsync(LogarRequest request);
    }
}

using Application.Dtos.Base;
using Application.Dtos.Requests;

namespace Application.Interfaces.Services
{
    public interface IAuthService
    {
        public Task<ResultPattern> RegistrarAsync(RegistrarUsuarioRequest request);
        public Task<ResultPatternGeneric<string>> LogarAsync(LogarRequest request);
    }
}

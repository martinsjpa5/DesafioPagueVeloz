using Application.Dtos.Base;
using Application.Dtos.Requests;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Route("[controller]")]
    public class AuthController : CustomController
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("Registrar")]
        public async Task<ActionResult<ResultPattern>> RegistrarAsync(RegistrarRequest request)
        {
            ResultPattern response = await _authService.RegistrarAsync(request);

            return CustomResponse(response);
        }

        [HttpPost("Logar")]
        public async Task<ActionResult<ResultPattern>> LogarAsync(LogarRequest request)
        {
            ResultPatternGeneric<string> response = await _authService.LogarAsync(request);

            return CustomResponse(response);
        }


    }
}

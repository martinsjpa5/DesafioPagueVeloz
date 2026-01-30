using Application.Dtos.Base;
using Application.Dtos.Requests;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{

    [Authorize]
    [Route("[controller]")]
    public class ContaController : CustomController
    {
        private readonly IContaService _contaService;

        public ContaController(IContaService contaService)
        {
            _contaService = contaService;
        }

        [HttpPost("Registrar")]
        public async Task<ActionResult<ResultPattern>> RegistrarPorUsuarioLogadoAsync(RegistrarContaRequest request)
        {
            var result = await _contaService.RegistrarPorUsuarioLogadoAsync(request);

            return CustomResponse(result);
        }


        [HttpGet]
        public async Task<ActionResult<ResultPattern>> ObterPorUsuarioLogadoAsync()
        {
            var result = await _contaService.ObterPorUsuarioLogadoAsync();

            return CustomResponse(result);
        }



    }
}

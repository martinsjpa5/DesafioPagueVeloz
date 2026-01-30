using Application.Dtos.Base;
using Application.Dtos.Requests;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class TransacaoController : CustomController
    {
        private readonly ITransacaoService _transacaoService;

        public TransacaoController(ITransacaoService transacaoService)
        {
            _transacaoService = transacaoService;
        }


        [HttpPost]
        public async Task<ActionResult<ResultPatternGeneric<CriarTransacaoRequest>>> CriarTransacao(CriarTransacaoRequest request)
        {
            var result = await _transacaoService.CriarTransacaoAsync(request, HttpContext.TraceIdentifier);

            return CustomResponse(result);
        }
    }
}

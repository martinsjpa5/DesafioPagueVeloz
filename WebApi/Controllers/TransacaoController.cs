using Application.Dtos.Base;
using Application.Dtos.Requests;
using Application.Dtos.Responses;
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

        [HttpGet("passiveisDeEstorno/conta/{contaId}")]
        public async Task<ActionResult<ResultPatternGeneric<IEnumerable<ObterTransacaoResponse>>>> ObterTransacoesPassiveisDeEstornoUsuarioLogadoAsync(int contaId)
        {
            var result = await _transacaoService.ObterTransacoesPassiveisDeEstornoUsuarioLogadoAsync(contaId);

            return CustomResponse(result);
        }

        [HttpGet("conta/{contaId}")]
        public async Task<ActionResult<ResultPatternGeneric<IEnumerable<ObterTransacaoResponse>>>> ObterTransacoesUsuarioLogadoAsync(int contaId)
        {
            var result = await _transacaoService.ObterTransacoesUsuarioLogadoAsync(contaId);

            return CustomResponse(result);
        }
    }
}

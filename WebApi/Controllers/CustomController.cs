using Application.Dtos.Base;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    public class CustomController: ControllerBase
    {
        protected ActionResult CustomResponse(ResultPattern response)
        {
            if (response.Erros.Count > 0)
                return BadRequest(response);
            else
                return Ok(response);
        }
        protected ActionResult CustomResponse<T>(ResultPatternGeneric<T> response)
        {
            if (response.Erros.Count > 0)
                return BadRequest(response);
            else
                return Ok(response);
        }
    }
}

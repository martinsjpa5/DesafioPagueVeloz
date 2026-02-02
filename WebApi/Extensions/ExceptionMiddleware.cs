using Application.Dtos.Base;
using System.Net;
using System.Text;

namespace WebApi.Extensions
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                HandleExceptionAsync(httpContext, ex);
            }
        }

        private void HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var mensagemErro = "Ops... Algo deu errado, Contate o Suporte do Sistema";

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            context.Response.WriteAsJsonAsync(ResultPattern.ErroBuilder(mensagemErro));

            this.EmitLogErrors(context, exception);
        }

        private void EmitLogErrors(HttpContext context, Exception exception)
        {
            StringBuilder logMessage = new();
            logMessage.AppendLine("StackTrace: " + exception.StackTrace);
            logMessage.AppendLine("QueryString: " + context.Request.QueryString);
            logMessage.AppendLine("Path: " + context.Request.Path);
            logMessage.AppendLine("Mensagem: " + exception.Message);

            _logger.LogError(logMessage.ToString());

        }
    }
}

using Application.Dtos.Base;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Extensions
{
    public static class ApiBehaviorExtensions
    {
        public static IServiceCollection AddApiBehavior(this IServiceCollection services)
        {
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = false;
                options.InvalidModelStateResponseFactory = context =>
                {
                    var erros = context.ModelState.Values.SelectMany(e => e.Errors);
                    var errosResult = erros.Select(x => x.ErrorMessage).ToList();

                    return new UnprocessableEntityObjectResult(ResultPattern.ErroBuilder(errosResult));
                };
            });

            return services;
        }
    }
}

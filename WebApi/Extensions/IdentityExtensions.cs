using Infraestrutura.EntityFramework;
using Infraestrutura.EntityFramework.Context;
using Infraestrutura.EntityFramework.Models;
using Microsoft.AspNetCore.Identity;

namespace WebApi.Extensions
{

    public static class IdentityExtensions
    {
        public static IServiceCollection AddIdentityConfiguration(this IServiceCollection services)
        {
            services
                .AddIdentityCore<ApplicationUser>(options =>
                {
                    options.Password.RequiredLength = 8;
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<AppDbContext>()
                .AddSignInManager()
                .AddDefaultTokenProviders()
                .AddErrorDescriber<IdentityMensagensEmPortugues>();

            return services;
        }
    }
}

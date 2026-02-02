using Domain.Interfaces.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infraestrutura.Caching
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration config)
        {
            services.AddSingleton<ICommonCachingRepository, CommonCachingRepository>();
            var redisConnection = config.GetSection("RedisConnection").Get<RedisConnectionSettings>() ?? new RedisConnectionSettings();


            services.AddStackExchangeRedisCache(o =>
            {
                o.InstanceName = redisConnection.InstanceName;
                o.Configuration = redisConnection.Configuration;
            });

            return services;
        }
    }
}

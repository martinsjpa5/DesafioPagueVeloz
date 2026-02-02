using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services;
using Domain.Services;
using Infraestrutura.Caching;
using Infraestrutura.EntidadeBaseFramework.Repositories;
using Infraestrutura.EntityFramework.Context;
using Infraestrutura.EntityFramework.Repositories;
using Infraestrutura.Messaging.RabbitMq;
using Microsoft.EntityFrameworkCore;
using WorkerTransacao;
using WorkerTransacao.Consumers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddRabbitMqMessaging(builder.Configuration);


builder.Services.AddSingleton<IMessageConsumer, TransacaoCriadaConsumer>();


builder.Services.AddScoped<IEfBaseRepository, EfBaseRepository>();
builder.Services.AddScoped<IProcessadorTransacaoDomainService, ProcessadorTransacaoDomainService>();
builder.Services.AddScoped<ITransacaoRepository, TransacaoRepository>();

builder.Services.AddSingleton<ICommonCachingRepository, CommonCachingRepository>();
var redisConnection = builder.Configuration.GetSection("RedisConnection").Get<RedisConnectionSettings>() ?? new RedisConnectionSettings();



builder.Services.AddStackExchangeRedisCache(o =>
{
    o.InstanceName = redisConnection.InstanceName;
    o.Configuration = redisConnection.Configuration;
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

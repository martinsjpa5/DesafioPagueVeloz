using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services;
using Domain.Services;
using Infraestrutura.Caching;
using Infraestrutura.EntityFramework;
using Infraestrutura.EntityFramework.Repositories;
using Infraestrutura.Messaging.RabbitMq;
using WorkerTransacao;
using WorkerTransacao.Consumers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddRabbitMqMessaging(builder.Configuration);
builder.Services.AddEntityFrameworkSql(builder.Configuration);
builder.Services.AddRedis(builder.Configuration);


builder.Services.AddSingleton<IMessageConsumer, TransacaoCriadaConsumer>();

builder.Services.AddScoped<IProcessadorTransacaoDomainService, ProcessadorTransacaoDomainService>();
builder.Services.AddScoped<ITransacaoRepository, TransacaoRepository>();


builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

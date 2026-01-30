using Infraestrutura.EntidadeBaseFramework.Repositories;
using Infraestrutura.EntityFramework.Context;
using Infraestrutura.Messaging.RabbitMq;
using Microsoft.EntityFrameworkCore;
using WorkerTransacao;
using WorkerTransacao.Consumers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddRabbitMqMessaging(builder.Configuration);


builder.Services.AddSingleton<IMessageConsumer, TransacaoCriadaConsumer>();

builder.Services.AddScoped<IEfBaseRepository, EfBaseRepository>();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

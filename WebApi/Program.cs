using Application.Interfaces.Context;
using Application.Interfaces.Services;
using Application.Services;
using Domain.Interfaces.Repositories;
using Infraestrutura.Caching;
using Infraestrutura.EntityFramework;
using Infraestrutura.EntityFramework.Context;
using Infraestrutura.EntityFramework.Repositories;
using Infraestrutura.Messaging.RabbitMq;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using WebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSwaggerDocumentation();

builder.Services.AddApiBehavior();

builder.Services.AddIdentityConfiguration();

builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserContext, UserContext>();
builder.Services.AddScoped<IContaService, ContaService>();
builder.Services.AddScoped<ITransacaoService, TransacaoService>();
builder.Services.AddScoped<ITransacaoRepository, TransacaoRepository>();

builder.Services.AddSingleton<ICommonCachingRepository, CommonCachingRepository>();

builder.Services.AddRabbitMqMessaging(builder.Configuration);
builder.Services.AddEntityFrameworkSql(builder.Configuration);
builder.Services.AddRedis(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseSwaggerDocumentation();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}


app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("database") || check.Tags.Contains("messaging")
});

app.MapControllers();

app.Run();

using Application.Interfaces.Context;
using Application.Interfaces.Services;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces.Repositories;
using Domain.Models;
using Infraestrutura.EntidadeBaseFramework.Repositories;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.Application.Services
{
    public class ContaServiceTest : BaseTest
    {
        private readonly IContaService _contaService;

        public ContaServiceTest()
        {
            _contaService = _autoMocker.CreateInstance<ContaService>();
        }

        [Fact]
        public async Task ObterContasParaTransferenciaAsync_RetornaLista_DeveRetornarSucesso()
        {
            _autoMocker.GetMock<IUserContext>().Setup(x => x.ClienteId).Returns(1);
            _autoMocker.GetMock<IEfBaseRepository>().Setup(x => x.ObterTodosPorCondicaoAsync<Conta>(It.IsAny<Expression<Func<Conta, bool>>>(),
        It.IsAny<Func<IQueryable<Conta>, IIncludableQueryable<Conta, object>>[]>())).ReturnsAsync([]);


            var result = await _contaService.ObterContasParaTransferenciaAsync(1);

            Assert.True(result.Sucesso);
        }

        [Fact]
        public async Task ObterPorUsuarioLogadoAsync_RetornaLista_DeveRetornarSucesso()
        {
            _autoMocker.GetMock<IUserContext>().Setup(x => x.ClienteId).Returns(1);
            _autoMocker.GetMock<IEfBaseRepository>().Setup(x => x.ObterTodosPorCondicaoAsync<Conta>(It.IsAny<Expression<Func<Conta, bool>>>(),
        It.IsAny<Func<IQueryable<Conta>, IIncludableQueryable<Conta, object>>[]>())).ReturnsAsync([]);


            var result = await _contaService.ObterPorUsuarioLogadoAsync();

            Assert.True(result.Sucesso);
        }

        [Fact]
        public async Task RegistrarPorUsuarioLogadoAsync_RetornaLista_DeveRetornarSucesso()
        {
            _autoMocker.GetMock<IUserContext>().Setup(x => x.ClienteId).Returns(1);


            var result = await _contaService.RegistrarPorUsuarioLogadoAsync(new());

            Assert.True(result.Sucesso);
        }

        [Fact]
        public async Task ObterPorIdUsuarioLogadoAsync_QuandoCacheHit_DeveRetornarSucesso_SemConsultarBanco_SemSetarCache()
        {
            // Arrange
            const int clienteId = 1;
            const int contaId = 10;

            _autoMocker.GetMock<IUserContext>()
                .Setup(x => x.ClienteId)
                .Returns(clienteId);

            var contaCache = new ContaModel
            {
                ClienteId = clienteId,
                ContaId = contaId,
                LimiteDeCredito = 1000m,
                SaldoDisponivel = 250m,
                SaldoReservado = 50m,
                Status = "Ativa"
            };

            _autoMocker.GetMock<ICommonCachingRepository>()
                .Setup(x => x.GetAsync<ContaModel>(It.IsAny<string>()))
                .ReturnsAsync(contaCache);

            // Act
            var result = await _contaService.ObterPorIdUsuarioLogadoAsync(contaId);

            // Assert
            Assert.True(result.Sucesso);
            Assert.NotNull(result.Data);
            Assert.Equal(contaId, result.Data!.Id);
            Assert.Equal(contaCache.LimiteDeCredito, result.Data.LimiteDeCredito);
            Assert.Equal(contaCache.SaldoDisponivel, result.Data.SaldoDisponivel);
            Assert.Equal(contaCache.SaldoReservado, result.Data.SaldoReservado);
            Assert.Equal(contaCache.Status, result.Data.Status);

            _autoMocker.GetMock<IEfBaseRepository>()
                .Verify(x => x.ObterPorCondicaoAsync<Conta>(It.IsAny<System.Linq.Expressions.Expression<System.Func<Conta, bool>>>()),
                    Times.Never);

            _autoMocker.GetMock<ICommonCachingRepository>()
                .Verify(x => x.SetAsync(It.IsAny<ContaModel>(), It.IsAny<TimeSpan>()),
                    Times.Never);
        }

        [Fact]
        public async Task ObterPorIdUsuarioLogadoAsync_QuandoCacheMiss_E_ContaNaoEncontrada_DeveRetornarErro()
        {
            // Arrange
            const int clienteId = 1;
            const int contaId = 999;

            _autoMocker.GetMock<IUserContext>()
                .Setup(x => x.ClienteId)
                .Returns(clienteId);

            _autoMocker.GetMock<ICommonCachingRepository>()
                .Setup(x => x.GetAsync<ContaModel>(It.IsAny<string>()))
                .ReturnsAsync((ContaModel?)null);

            _autoMocker.GetMock<IEfBaseRepository>()
                .Setup(x => x.ObterPorCondicaoAsync<Conta>(It.IsAny<System.Linq.Expressions.Expression<System.Func<Conta, bool>>>()))
                .ReturnsAsync((Conta?)null);

            // Act
            var result = await _contaService.ObterPorIdUsuarioLogadoAsync(contaId);

            // Assert
            Assert.False(result.Sucesso);
            Assert.NotNull(result.Erros);
            Assert.Single(result.Erros);
            Assert.Equal("Conta não Encontrada!", result.Erros[0]);
            Assert.Equal(default, result.Data);

            _autoMocker.GetMock<ICommonCachingRepository>()
                .Verify(x => x.SetAsync(It.IsAny<ContaModel>(), It.IsAny<TimeSpan>()),
                    Times.Never);

            _autoMocker.GetMock<IEfBaseRepository>()
                .Verify(x => x.ObterPorCondicaoAsync<Conta>(It.IsAny<System.Linq.Expressions.Expression<System.Func<Conta, bool>>>()),
                    Times.Once);
        }

        [Fact]
        public async Task ObterPorIdUsuarioLogadoAsync_QuandoCacheMiss_E_ContaEncontrada_DeveSetarCache_E_RetornarSucesso()
        {
            // Arrange
            const int clienteId = 1;
            const int contaId = 10;

            _autoMocker.GetMock<IUserContext>()
                .Setup(x => x.ClienteId)
                .Returns(clienteId);

            _autoMocker.GetMock<ICommonCachingRepository>()
                .Setup(x => x.GetAsync<ContaModel>(It.IsAny<string>()))
                .ReturnsAsync((ContaModel?)null);

            var conta = new Conta
            {
                Id = contaId,
                ClienteId = clienteId,
                LimiteDeCredito = 2000m,
                SaldoDisponivel = 800m,
                SaldoReservado = 100m,
                Status = StatusContaEnum.Ativa 
            };

            _autoMocker.GetMock<IEfBaseRepository>()
                .Setup(x => x.ObterPorCondicaoAsync<Conta>(It.IsAny<Expression<System.Func<Conta, bool>>>()))
                .ReturnsAsync(conta);

            ContaModel? contaSetadaNoCache = null;
            TimeSpan? ttlSetado = null;

            _autoMocker.GetMock<ICommonCachingRepository>()
                .Setup(x => x.SetAsync(It.IsAny<ContaModel>(), It.IsAny<TimeSpan>()))
                .Callback<ContaModel, TimeSpan>((model, ttl) =>
                {
                    contaSetadaNoCache = model;
                    ttlSetado = ttl;
                })
                .Returns(Task.FromResult(true));

            // Act
            var result = await _contaService.ObterPorIdUsuarioLogadoAsync(contaId);

            // Assert
            Assert.True(result.Sucesso);
            Assert.NotNull(result.Data);
            Assert.Equal(contaId, result.Data!.Id);
            Assert.Equal(conta.LimiteDeCredito, result.Data.LimiteDeCredito);
            Assert.Equal(conta.SaldoDisponivel, result.Data.SaldoDisponivel);
            Assert.Equal(conta.SaldoReservado, result.Data.SaldoReservado);
            Assert.Equal(conta.Status.ToString(), result.Data.Status);

            _autoMocker.GetMock<IEfBaseRepository>()
                .Verify(x => x.ObterPorCondicaoAsync<Conta>(It.IsAny<System.Linq.Expressions.Expression<System.Func<Conta, bool>>>()),
                    Times.Once);

            _autoMocker.GetMock<ICommonCachingRepository>()
                .Verify(x => x.SetAsync(It.IsAny<ContaModel>(), It.IsAny<TimeSpan>()),
                    Times.Once);

            Assert.NotNull(contaSetadaNoCache);
            Assert.Equal(clienteId, contaSetadaNoCache!.ClienteId);
            Assert.Equal(contaId, contaSetadaNoCache.ContaId);
            Assert.Equal(conta.LimiteDeCredito, contaSetadaNoCache.LimiteDeCredito);
            Assert.Equal(conta.SaldoDisponivel, contaSetadaNoCache.SaldoDisponivel);
            Assert.Equal(conta.SaldoReservado, contaSetadaNoCache.SaldoReservado);
            Assert.Equal(conta.Status.ToString(), contaSetadaNoCache.Status);

            Assert.NotNull(ttlSetado);
            Assert.Equal(TimeSpan.FromDays(1), ttlSetado!.Value);
        }
    }
}
    
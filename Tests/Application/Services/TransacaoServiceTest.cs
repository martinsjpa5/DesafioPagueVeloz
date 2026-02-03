
using Application.Dtos.Requests;
using Application.Interfaces.Context;
using Application.Interfaces.Services;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Events;
using Domain.Interfaces.Repositories;
using Infraestrutura.EntidadeBaseFramework.Repositories;
using Infraestrutura.Messaging.RabbitMq;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.Application.Services
{
    public class TransacaoServiceTest : BaseTest
    {
        private readonly ITransacaoService _transacaoService;

        public TransacaoServiceTest()
        {
            _autoMocker.Use(new RabbitMqOptions { ShardCount = 8 });

            _transacaoService = _autoMocker.CreateInstance<TransacaoService>();
        }

        [Fact]
        public async Task ObterTransacoesPassiveisDeEstornoUsuarioLogadoAsync_Lista_DeveRetornarSucesso()
        {
            _autoMocker.GetMock<IUserContext>().Setup(x => x.ClienteId).Returns(1);
            _autoMocker.GetMock<ITransacaoRepository>().Setup(x => x.ObterTransacoesPassiveisDeEstornoAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync([]);


            var result = await _transacaoService.ObterTransacoesPassiveisDeEstornoUsuarioLogadoAsync(1);

            Assert.True(result.Sucesso);
        }

        [Fact]
        public async Task ObterTransacoesUsuarioLogadoAsync_Lista_DeveRetornarSucesso()
        {
            _autoMocker.GetMock<IUserContext>().Setup(x => x.ClienteId).Returns(1);
            _autoMocker.GetMock<IEfBaseRepository>().Setup(x => x.ObterTodosPorCondicaoAsync<Transacao>(It.IsAny<Expression<Func<Transacao, bool>>>(),
        It.IsAny<Func<IQueryable<Transacao>, IIncludableQueryable<Transacao, object>>[]>())).ReturnsAsync([]);


            var result = await _transacaoService.ObterTransacoesUsuarioLogadoAsync(1);

            Assert.True(result.Sucesso);
        }

        [Fact]
        public async Task CriarTransacaoAsync_QuandoContaOrigemIdInvalido_DeveRetornarErro_E_NaoPersistir_NemPublicar()
        {
            _autoMocker.GetMock<IUserContext>().Setup(x => x.ClienteId).Returns(1);

            var request = new CriarTransacaoRequest
            {
                Operacao = (int)TipoOperacaoEnum.Transferencia,
                ContaOrigemId = 0,
                ContaDestinoId = 2,
                Quantia = 10,
                Moeda = "BRL"
            };

            var result = await _transacaoService.CriarTransacaoAsync(request, "corr-1");

            Assert.False(result.Sucesso);
            Assert.Contains("Conta Origem é obrigatória.", result.Erros);

            _autoMocker.GetMock<IEfBaseRepository>()
                .Verify(x => x.AdicionarEntidadeBaseAsync(It.IsAny<Transacao>()), Times.Never);


        }

        [Fact]
        public async Task CriarTransacaoAsync_QuandoQuantiaInvalida_E_NaoEstorno_DeveRetornarErro()
        {
            _autoMocker.GetMock<IUserContext>().Setup(x => x.ClienteId).Returns(1);

            var request = new CriarTransacaoRequest
            {
                Operacao = (int)TipoOperacaoEnum.Captura,
                ContaOrigemId = 1,
                Quantia = 0,
                Moeda = "BRL"
            };

            var result = await _transacaoService.CriarTransacaoAsync(request, "corr-1");

            Assert.False(result.Sucesso);
            Assert.Contains("Quantia deve ser maior que zero.", result.Erros);

            _autoMocker.GetMock<IEfBaseRepository>()
                .Verify(x => x.AdicionarEntidadeBaseAsync(It.IsAny<Transacao>()), Times.Never);
        }

        [Fact]
        public async Task CriarTransacaoAsync_QuandoContaOrigemNaoEncontrada_DeveRetornarErro()
        {
            _autoMocker.GetMock<IUserContext>().Setup(x => x.ClienteId).Returns(1);

            _autoMocker.GetMock<IEfBaseRepository>()
                .Setup(x => x.ObterPorCondicaoAsync<Conta>(It.IsAny<System.Linq.Expressions.Expression<System.Func<Conta, bool>>>()))
                .ReturnsAsync((Conta?)null);

            var request = new CriarTransacaoRequest
            {
                Operacao = (int)TipoOperacaoEnum.Transferencia,
                ContaOrigemId = 10,
                ContaDestinoId = 11,
                Quantia = 50,
                Moeda = "BRL"
            };

            var result = await _transacaoService.CriarTransacaoAsync(request, "corr-1");

            Assert.False(result.Sucesso);
            Assert.Contains("Conta Origem não encontrada!", result.Erros);

            _autoMocker.GetMock<IEfBaseRepository>()
                .Verify(x => x.AdicionarEntidadeBaseAsync(It.IsAny<Transacao>()), Times.Never);
        }

        [Fact]
        public async Task CriarTransacaoAsync_Transferencia_QuandoContaDestinoIdInvalida_DeveRetornarErro()
        {
            _autoMocker.GetMock<IUserContext>().Setup(x => x.ClienteId).Returns(1);

            _autoMocker.GetMock<IEfBaseRepository>()
                .Setup(x => x.ObterPorCondicaoAsync<Conta>(It.IsAny<System.Linq.Expressions.Expression<System.Func<Conta, bool>>>()))
                .ReturnsAsync(new Conta
                {
                    Id = 10,
                    ClienteId = 1,
                    SaldoDisponivel = 100,
                    SaldoReservado = 0,
                    LimiteDeCredito = 0,
                    Status = StatusContaEnum.Ativa
                });

            var request = new CriarTransacaoRequest
            {
                Operacao = (int)TipoOperacaoEnum.Transferencia,
                ContaOrigemId = 10,
                ContaDestinoId = 0,
                Quantia = 50,
                Moeda = "BRL"
            };

            var result = await _transacaoService.CriarTransacaoAsync(request, "corr-1");

            Assert.False(result.Sucesso);
            Assert.Contains("Conta Destino é obrigatória para Transferência!", result.Erros);

            _autoMocker.GetMock<IEfBaseRepository>()
                .Verify(x => x.AdicionarEntidadeBaseAsync(It.IsAny<Transacao>()), Times.Never);
        }

        [Fact]
        public async Task CriarTransacaoAsync_Transferencia_QuandoContaDestinoNaoEncontrada_DeveRetornarErro()
        {
            _autoMocker.GetMock<IUserContext>().Setup(x => x.ClienteId).Returns(1);

            _autoMocker.GetMock<IEfBaseRepository>()
                .SetupSequence(x => x.ObterPorCondicaoAsync<Conta>(
                    It.IsAny<System.Linq.Expressions.Expression<System.Func<Conta, bool>>>()))
                .ReturnsAsync(new Conta
                {
                    Id = 10,
                    ClienteId = 1,
                    SaldoDisponivel = 100,
                    SaldoReservado = 0,
                    LimiteDeCredito = 0,
                    Status = StatusContaEnum.Ativa 
                })
                .ReturnsAsync((Conta?)null);

            var request = new CriarTransacaoRequest
            {
                Operacao = (int)TipoOperacaoEnum.Transferencia,
                ContaOrigemId = 10,
                ContaDestinoId = 99,
                Quantia = 50,
                Moeda = "BRL"
            };

            var result = await _transacaoService.CriarTransacaoAsync(request, "corr-1");

            Assert.False(result.Sucesso);
            Assert.Contains("Conta Destino não encontrada!", result.Erros);

            _autoMocker.GetMock<IEfBaseRepository>()
                .Verify(x => x.AdicionarEntidadeBaseAsync(It.IsAny<Transacao>()), Times.Never);
        }

        [Fact]
        public async Task CriarTransacaoAsync_Transferencia_QuandoContaDestinoIgualOrigem_DeveRetornarErro()
        {
            _autoMocker.GetMock<IUserContext>().Setup(x => x.ClienteId).Returns(1);

            _autoMocker.GetMock<IEfBaseRepository>()
                .SetupSequence(x => x.ObterPorCondicaoAsync<Conta>(
                    It.IsAny<System.Linq.Expressions.Expression<System.Func<Conta, bool>>>()))
                .ReturnsAsync(new Conta { Id = 10, ClienteId = 1, SaldoDisponivel = 100, Status = StatusContaEnum.Ativa })
                .ReturnsAsync(new Conta { Id = 10, ClienteId = 999, SaldoDisponivel = 0, Status = StatusContaEnum.Ativa });

            var request = new CriarTransacaoRequest
            {
                Operacao = (int)TipoOperacaoEnum.Transferencia,
                ContaOrigemId = 10,
                ContaDestinoId = 10,
                Quantia = 50,
                Moeda = "BRL"
            };

            var result = await _transacaoService.CriarTransacaoAsync(request, "corr-1");

            Assert.False(result.Sucesso);
            Assert.Contains("Conta Destino não pode ser igual à Conta Origem.", result.Erros);

            _autoMocker.GetMock<IEfBaseRepository>()
                .Verify(x => x.AdicionarEntidadeBaseAsync(It.IsAny<Transacao>()), Times.Never);
        }

        [Fact]
        public async Task CriarTransacaoAsync_Transferencia_QuandoOk_DevePersistir_MapearResponse_E_PublicarEvento()
        {
            const int clienteId = 1;
            const int contaOrigemId = 10;
            const int contaDestinoId = 11;
            const string correlationId = "corr-xyz";

            _autoMocker.GetMock<IUserContext>().Setup(x => x.ClienteId).Returns(clienteId);

            var contaOrigem = new Conta
            {
                Id = contaOrigemId,
                ClienteId = clienteId,
                SaldoDisponivel = 1000m,
                SaldoReservado = 200m,
                LimiteDeCredito = 0m,
                Status = StatusContaEnum.Ativa
            };

            var contaDestino = new Conta
            {
                Id = contaDestinoId,
                ClienteId = 999, 
                SaldoDisponivel = 0m,
                SaldoReservado = 0m,
                LimiteDeCredito = 0m,
                Status = StatusContaEnum.Ativa
            };

            _autoMocker.GetMock<IEfBaseRepository>()
                .SetupSequence(x => x.ObterPorCondicaoAsync<Conta>(
                    It.IsAny<System.Linq.Expressions.Expression<System.Func<Conta, bool>>>()))
                .ReturnsAsync(contaOrigem)   
                .ReturnsAsync(contaDestino); 

            Transacao? transacaoPersistida = null;

            _autoMocker.GetMock<IEfBaseRepository>()
                .Setup(x => x.AdicionarEntidadeBaseAsync(It.IsAny<Transacao>()))
                .Callback<object>(obj =>
                {
                    transacaoPersistida = (Transacao)obj;

                    transacaoPersistida.Id = 123;
                })
                .Returns(Task.FromResult(true));

            _autoMocker.GetMock<IEfBaseRepository>()
                .Setup(x => x.SalvarAlteracoesAsync())
                .Returns(Task.FromResult(1));

            Dictionary<string, object>? headersPublicados = null;
            string? exchangePublicado = null;
            string? routingKeyPublicado = null;
            object? eventoPublicado = null;

            _autoMocker.GetMock<IMessagePublisher>()
    .Setup(x => x.PublishAsync<object>(
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<object>(),
        It.IsAny<IDictionary<string, object>?>(),
        It.IsAny<CancellationToken>()
    ))
    .Callback<string, string, object, IDictionary<string, object>?, CancellationToken>((ex, rk, evt, headers, ct) =>
    {
        exchangePublicado = ex;
        routingKeyPublicado = rk;
        eventoPublicado = evt;
        headersPublicados = (Dictionary<string, object>?)(headers ?? new Dictionary<string,object>());
    })
    .Returns(Task.CompletedTask);

            var request = new CriarTransacaoRequest
            {
                Operacao = (int)TipoOperacaoEnum.Transferencia,
                ContaOrigemId = contaOrigemId,
                ContaDestinoId = contaDestinoId,
                Quantia = 50m,
                Moeda = "BRL"
            };

            var result = await _transacaoService.CriarTransacaoAsync(request, correlationId);

            Assert.True(result.Sucesso);
            Assert.NotNull(result.Data);

            Assert.NotNull(transacaoPersistida);
            Assert.Equal(contaOrigemId, transacaoPersistida!.ContaOrigemId);
            Assert.Equal(contaDestinoId, transacaoPersistida.ContaDestinoId);
            Assert.Equal("BRL", transacaoPersistida.Moeda);
            Assert.Equal(50m, transacaoPersistida.Quantia);
            Assert.Equal(TipoOperacaoEnum.Transferencia, transacaoPersistida.Tipo);
            Assert.Equal(StatusTransacaoEnum.PENDENTE, transacaoPersistida.Status);

            _autoMocker.GetMock<IEfBaseRepository>()
                .Verify(x => x.AdicionarEntidadeBaseAsync(It.IsAny<Transacao>()), Times.Once);

            _autoMocker.GetMock<IEfBaseRepository>()
                .Verify(x => x.SalvarAlteracoesAsync(), Times.Once);
            Assert.Equal(123, result.Data!.Id);
            Assert.Equal(contaOrigem.SaldoDisponivel, result.Data.SaldoDisponivel);
            Assert.Equal(contaOrigem.SaldoReservado, result.Data.SaldoReservado);
            Assert.Equal(contaOrigem.SaldoDisponivel + contaOrigem.SaldoReservado, result.Data.SaldoTotal);

            _autoMocker.GetMock<IMessagePublisher>()
    .Verify(x => x.PublishAsync<object>(
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<object>(),
        It.IsAny<IDictionary<string, object>?>(),
        It.IsAny<CancellationToken>()),
        Times.Once);


            Assert.Equal("transacoes.exchange", exchangePublicado);
            Assert.NotNull(routingKeyPublicado);
            Assert.StartsWith("transacoes.shard-", routingKeyPublicado);

            Assert.NotNull(headersPublicados);
            Assert.True(headersPublicados!.ContainsKey("correlationId"));
            Assert.Equal(correlationId, headersPublicados["correlationId"]);

            Assert.IsType<TransacaoCriadaEvent>(eventoPublicado);
            Assert.Equal(123, ((TransacaoCriadaEvent)eventoPublicado!).TransacaoId);
        }

        [Fact]
        public async Task CriarTransacaoAsync_Estorno_QuandoTransacaoEstornadaIdInvalido_DeveRetornarErro()
        {
            _autoMocker.GetMock<IUserContext>().Setup(x => x.ClienteId).Returns(1);

            _autoMocker.GetMock<IEfBaseRepository>()
                .Setup(x => x.ObterPorCondicaoAsync<Conta>(It.IsAny<System.Linq.Expressions.Expression<System.Func<Conta, bool>>>()))
                .ReturnsAsync(new Conta { Id = 10, ClienteId = 1, SaldoDisponivel = 10, Status = StatusContaEnum.Ativa });

            var request = new CriarTransacaoRequest
            {
                Operacao = (int)TipoOperacaoEnum.Estorno,
                ContaOrigemId = 10,
                Quantia = 0,
                TransacaoEstornadaId = 0
            };

            var result = await _transacaoService.CriarTransacaoAsync(request, "corr-1");

            Assert.False(result.Sucesso);
            Assert.Contains("O Id da Transação que você quer estornar é obrigatório.", result.Erros);

            _autoMocker.GetMock<IEfBaseRepository>()
                .Verify(x => x.AdicionarEntidadeBaseAsync(It.IsAny<Transacao>()), Times.Never);
        }

        [Fact]
        public async Task CriarTransacaoAsync_Estorno_QuandoOk_DeveCriarTransacaoComDadosDaOriginal_E_Publicar()
        {
            const int clienteId = 1;
            const int contaOrigemId = 10;
            const int transacaoOriginalId = 77;

            _autoMocker.GetMock<IUserContext>().Setup(x => x.ClienteId).Returns(clienteId);


            _autoMocker.GetMock<IEfBaseRepository>()
                .SetupSequence(x => x.ObterPorCondicaoAsync<Conta>(
                    It.IsAny<System.Linq.Expressions.Expression<System.Func<Conta, bool>>>()))
                .ReturnsAsync(new Conta { Id = contaOrigemId, ClienteId = clienteId, SaldoDisponivel = 100, SaldoReservado = 0, Status = StatusContaEnum.Ativa }) // conta origem inicial
                .ReturnsAsync(new Conta { Id = contaOrigemId, ClienteId = clienteId, SaldoDisponivel = 100, SaldoReservado = 0, Status = StatusContaEnum.Ativa }); // conta origem autorizada (mesma)

            _autoMocker.GetMock<IEfBaseRepository>()
                .SetupSequence(x => x.ObterPorCondicaoAsync<Transacao>(
                    It.IsAny<System.Linq.Expressions.Expression<System.Func<Transacao, bool>>>()))
                .ReturnsAsync(new Transacao
                {
                    Id = transacaoOriginalId,
                    ContaOrigemId = contaOrigemId,
                    ContaDestinoId = 11,
                    Moeda = "BRL",
                    Quantia = 25m,
                    Status = StatusTransacaoEnum.SUCESSO,
                    Tipo = TipoOperacaoEnum.Transferencia
                })
                .ReturnsAsync((Transacao?)null); 

            Transacao? transacaoCriada = null;

            _autoMocker.GetMock<IEfBaseRepository>()
                .Setup(x => x.AdicionarEntidadeBaseAsync(It.IsAny<Transacao>()))
                .Callback<object>(obj =>
                {
                    transacaoCriada = (Transacao)obj;
                    transacaoCriada.Id = 555;
                })
                .Returns(Task.FromResult(true));

            _autoMocker.GetMock<IEfBaseRepository>()
                .Setup(x => x.SalvarAlteracoesAsync())
                .Returns(Task.FromResult(1));

            _autoMocker.GetMock<IMessagePublisher>()
     .Setup(x => x.PublishAsync<object>(
         It.IsAny<string>(),
         It.IsAny<string>(),
         It.IsAny<object>(),
         It.IsAny<Dictionary<string, object>?>(),
         It.IsAny<CancellationToken>()
     ))
     .Returns(Task.CompletedTask);

            var request = new CriarTransacaoRequest
            {
                Operacao = (int)TipoOperacaoEnum.Estorno,
                ContaOrigemId = contaOrigemId,
                Quantia = 0, 
                TransacaoEstornadaId = transacaoOriginalId
            };

            var result = await _transacaoService.CriarTransacaoAsync(request, "corr-estorno");

            Assert.True(result.Sucesso);
            Assert.NotNull(result.Data);

            Assert.NotNull(transacaoCriada);
            Assert.Equal(TipoOperacaoEnum.Estorno, transacaoCriada!.Tipo);
            Assert.Equal(transacaoOriginalId, transacaoCriada.TransacaoEstornadaId);
            Assert.Equal(contaOrigemId, transacaoCriada.ContaOrigemId);
            Assert.Equal(11, transacaoCriada.ContaDestinoId);
            Assert.Equal("BRL", transacaoCriada.Moeda);
            Assert.Equal(25m, transacaoCriada.Quantia);
            Assert.Equal(StatusTransacaoEnum.PENDENTE, transacaoCriada.Status);

            _autoMocker.GetMock<IMessagePublisher>()
    .Verify(x => x.PublishAsync(
            "transacoes.exchange",
            It.Is<string>(rk => rk.StartsWith("transacoes.shard-")),
            It.Is<TransacaoCriadaEvent>(e => e.TransacaoId == 555),
            It.Is<Dictionary<string, object>?>(h =>
                h != null &&
                h.ContainsKey("correlationId") &&
                (string)h["correlationId"] == "corr-estorno"),
            It.IsAny<CancellationToken>()
        ),
        Times.Once);
        }



    }
}

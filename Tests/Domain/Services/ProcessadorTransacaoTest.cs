using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces.Services;
using Domain.Services;
using Xunit;

namespace Tests.Domain.Services
{
    public class ProcessadorTransacaoTest : BaseTest
    {
        private readonly IProcessadorTransacaoDomainService _processadorTransacaoDomainService;

        public ProcessadorTransacaoTest()
        {
            _processadorTransacaoDomainService = _autoMocker.CreateInstance<ProcessadorTransacaoDomainService>();
        }

        private static Conta NovaConta(
            decimal saldoDisponivel = 0m,
            decimal saldoReservado = 0m,
            decimal limiteDeCredito = 0m)
            => new Conta
            {
                SaldoDisponivel = saldoDisponivel,
                SaldoReservado = saldoReservado,
                LimiteDeCredito = limiteDeCredito
            };

        private static Transacao NovaTransacao(
            TipoOperacaoEnum tipo,
            decimal quantia = 10m,
            string moeda = "BRL",
            Conta? origem = null,
            Conta? destino = null)
            => new Transacao
            {
                Tipo = tipo,
                Quantia = quantia,
                Moeda = moeda,
                ContaOrigem = origem,
                ContaDestino = destino
            };

        private static Transacao NovaSolicitacaoEstorno(Transacao? original, string moeda = "BRL")
            => new Transacao
            {
                Tipo = TipoOperacaoEnum.Estorno,
                Quantia = 1m,
                Moeda = moeda,
                TransacaoEstornada = original
            };


        [Fact]
        public void Processar_DeveRetornarErro_QuandoQuantiaForMenorOuIgualZero()
        {
            var t = NovaTransacao(TipoOperacaoEnum.Credito, quantia: 0m, moeda: "BRL", origem: NovaConta(100m));

            var result = _processadorTransacaoDomainService.Processar(t);

            Assert.False(result.Sucesso);
            Assert.Contains("Quantia deve ser maior que zero.", result.Erros);
            Assert.Equal(StatusTransacaoEnum.FALHA, t.Status);
            Assert.Contains("Quantia deve ser maior que zero.", t.MensagemErro);
        }

        [Fact]
        public void Processar_DeveRetornarErro_QuandoMoedaForNulaOuVazia()
        {
            var t = NovaTransacao(TipoOperacaoEnum.Credito, quantia: 10m, moeda: "", origem: NovaConta(100m));

            var result = _processadorTransacaoDomainService.Processar(t);

            Assert.False(result.Sucesso);
            Assert.Contains("Moeda é obrigatória.", result.Erros);
            Assert.Equal(StatusTransacaoEnum.FALHA, t.Status);
            Assert.Contains("Moeda é obrigatória.", t.MensagemErro);
        }

        [Fact]
        public void Processar_DeveRetornarMultiplosErros_QuandoQuantiaInvalidaEMoedaInvalida()
        {
            var t = NovaTransacao(TipoOperacaoEnum.Credito, quantia: 0m, moeda: "   ", origem: NovaConta(100m));

            var result = _processadorTransacaoDomainService.Processar(t);

            Assert.False(result.Sucesso);
            Assert.Contains("Quantia deve ser maior que zero.", result.Erros);
            Assert.Contains("Moeda é obrigatória.", result.Erros);
            Assert.Equal(StatusTransacaoEnum.FALHA, t.Status);
            Assert.Contains("Quantia deve ser maior que zero.", t.MensagemErro);
            Assert.Contains("Moeda é obrigatória.", t.MensagemErro);
        }

        [Fact]
        public void Processar_DeveRetornarErro_QuandoTipoOperacaoInvalido()
        {
            var t = NovaTransacao((TipoOperacaoEnum)999, quantia: 10m, moeda: "BRL");

            var result = _processadorTransacaoDomainService.Processar(t);

            Assert.False(result.Sucesso);
            Assert.Contains("Tipo de operação inválido", result.Erros);
            Assert.Equal(StatusTransacaoEnum.FALHA, t.Status);
            Assert.Contains("Tipo de operação inválido", t.MensagemErro);
        }


        [Fact]
        public void Processar_Credito_DeveFalhar_QuandoContaOrigemForNula()
        {
            var t = NovaTransacao(TipoOperacaoEnum.Credito, quantia: 50m, moeda: "BRL", origem: null);

            var result = _processadorTransacaoDomainService.Processar(t);

            Assert.False(result.Sucesso);
            Assert.Contains("Conta origem é obrigatória para crédito", result.Erros);
            Assert.Equal(StatusTransacaoEnum.FALHA, t.Status);
        }

        [Fact]
        public void Processar_Credito_DeveSomarSaldoEMarcarSucesso()
        {
            var conta = NovaConta(saldoDisponivel: 100m);
            var t = NovaTransacao(TipoOperacaoEnum.Credito, quantia: 50m, moeda: "BRL", origem: conta);

            var result = _processadorTransacaoDomainService.Processar(t);

            Assert.True(result.Sucesso);
            Assert.Equal(150m, conta.SaldoDisponivel);
            Assert.Equal(StatusTransacaoEnum.SUCESSO, t.Status);
            Assert.Equal(string.Empty, t.MensagemErro);
        }


        [Fact]
        public void Processar_Debito_DeveFalhar_QuandoContaOrigemForNula()
        {
            var t = NovaTransacao(TipoOperacaoEnum.Debito, quantia: 10m, moeda: "BRL", origem: null);

            var result = _processadorTransacaoDomainService.Processar(t);

            Assert.False(result.Sucesso);
            Assert.Contains("Conta origem é obrigatória para débito", result.Erros);
            Assert.Equal(StatusTransacaoEnum.FALHA, t.Status);
        }

        [Fact]
        public void Processar_Debito_DeveFalhar_QuandoSaldoInsuficienteConsiderandoLimite()
        {
            var conta = NovaConta(saldoDisponivel: 10m, limiteDeCredito: 5m);
            var t = NovaTransacao(TipoOperacaoEnum.Debito, quantia: 20m, moeda: "BRL", origem: conta);

            var result = _processadorTransacaoDomainService.Processar(t);

            Assert.False(result.Sucesso);
            Assert.Contains("Saldo insuficiente considerando o limite de crédito", result.Erros);
            Assert.Equal(10m, conta.SaldoDisponivel); 
            Assert.Equal(StatusTransacaoEnum.FALHA, t.Status);
        }

        [Fact]
        public void Processar_Debito_DeveDebitarEPermitirSaldoNegativoAteOLimite()
        {
            var conta = NovaConta(saldoDisponivel: 10m, limiteDeCredito: 50m);
            var t = NovaTransacao(TipoOperacaoEnum.Debito, quantia: 40m, moeda: "BRL", origem: conta);

            var result = _processadorTransacaoDomainService.Processar(t);

            Assert.True(result.Sucesso);
            Assert.Equal(-30m, conta.SaldoDisponivel);
            Assert.Equal(StatusTransacaoEnum.SUCESSO, t.Status);
            Assert.Equal(string.Empty, t.MensagemErro);
        }


        [Fact]
        public void Processar_Reserva_DeveFalhar_QuandoContaOrigemForNula()
        {
            var t = NovaTransacao(TipoOperacaoEnum.Reserva, quantia: 10m, moeda: "BRL", origem: null);

            var result = _processadorTransacaoDomainService.Processar(t);

            Assert.False(result.Sucesso);
            Assert.Contains("Conta origem é obrigatória para reserva", result.Erros);
        }

        [Fact]
        public void Processar_Reserva_DeveFalhar_QuandoSaldoDisponivelInsuficiente()
        {
            var conta = NovaConta(saldoDisponivel: 5m, saldoReservado: 0m);
            var t = NovaTransacao(TipoOperacaoEnum.Reserva, quantia: 10m, moeda: "BRL", origem: conta);

            var result = _processadorTransacaoDomainService.Processar(t);

            Assert.False(result.Sucesso);
            Assert.Contains("Saldo disponível insuficiente para reserva", result.Erros);
            Assert.Equal(5m, conta.SaldoDisponivel);
            Assert.Equal(0m, conta.SaldoReservado);
            Assert.Equal(StatusTransacaoEnum.FALHA, t.Status);
        }

        [Fact]
        public void Processar_Reserva_DeveMoverSaldoParaReservado()
        {
            var conta = NovaConta(saldoDisponivel: 100m, saldoReservado: 20m);
            var t = NovaTransacao(TipoOperacaoEnum.Reserva, quantia: 30m, moeda: "BRL", origem: conta);

            var result = _processadorTransacaoDomainService.Processar(t);

            Assert.True(result.Sucesso);
            Assert.Equal(70m, conta.SaldoDisponivel);
            Assert.Equal(50m, conta.SaldoReservado);
            Assert.Equal(StatusTransacaoEnum.SUCESSO, t.Status);
        }


        [Fact]
        public void Processar_Captura_DeveFalhar_QuandoContaOrigemForNula()
        {
            var t = NovaTransacao(TipoOperacaoEnum.Captura, quantia: 10m, moeda: "BRL", origem: null);

            var result = _processadorTransacaoDomainService.Processar(t);

            Assert.False(result.Sucesso);
            Assert.Contains("Conta origem é obrigatória para captura", result.Erros);
        }

        [Fact]
        public void Processar_Captura_DeveFalhar_QuandoSaldoReservadoInsuficiente()
        {
            var conta = NovaConta(saldoDisponivel: 100m, saldoReservado: 5m);
            var t = NovaTransacao(TipoOperacaoEnum.Captura, quantia: 10m, moeda: "BRL", origem: conta);

            var result = _processadorTransacaoDomainService.Processar(t);

            Assert.False(result.Sucesso);
            Assert.Contains("Saldo reservado insuficiente para captura", result.Erros);
            Assert.Equal(5m, conta.SaldoReservado);
            Assert.Equal(StatusTransacaoEnum.FALHA, t.Status);
        }

        [Fact]
        public void Processar_Captura_DeveDiminuirSaldoReservado()
        {
            var conta = NovaConta(saldoDisponivel: 100m, saldoReservado: 50m);
            var t = NovaTransacao(TipoOperacaoEnum.Captura, quantia: 10m, moeda: "BRL", origem: conta);

            var result = _processadorTransacaoDomainService.Processar(t);

            Assert.True(result.Sucesso);
            Assert.Equal(40m, conta.SaldoReservado);
            Assert.Equal(100m, conta.SaldoDisponivel);
            Assert.Equal(StatusTransacaoEnum.SUCESSO, t.Status);
        }
        [Fact]
        public void Processar_Transferencia_DeveFalhar_QuandoOrigemOuDestinoForNulo()
        {
            var t1 = NovaTransacao(TipoOperacaoEnum.Transferencia, quantia: 10m, moeda: "BRL", origem: NovaConta(100m), destino: null);
            var r1 = _processadorTransacaoDomainService.Processar(t1);
            Assert.False(r1.Sucesso);
            Assert.Contains("Conta origem e destino são obrigatórias para transferência", r1.Erros);

            var t2 = NovaTransacao(TipoOperacaoEnum.Transferencia, quantia: 10m, moeda: "BRL", origem: null, destino: NovaConta(100m));
            var r2 = _processadorTransacaoDomainService.Processar(t2);
            Assert.False(r2.Sucesso);
            Assert.Contains("Conta origem e destino são obrigatórias para transferência", r2.Erros);
        }

        [Fact]
        public void Processar_Transferencia_DeveFalhar_QuandoSaldoOrigemInsuficienteConsiderandoLimite()
        {
            var origem = NovaConta(saldoDisponivel: 10m, limiteDeCredito: 0m);
            var destino = NovaConta(saldoDisponivel: 0m);
            var t = NovaTransacao(TipoOperacaoEnum.Transferencia, quantia: 20m, moeda: "BRL", origem: origem, destino: destino);

            var result = _processadorTransacaoDomainService.Processar(t);

            Assert.False(result.Sucesso);
            Assert.Contains("Saldo insuficiente na conta origem", result.Erros);
            Assert.Equal(10m, origem.SaldoDisponivel);
            Assert.Equal(0m, destino.SaldoDisponivel);
            Assert.Equal(StatusTransacaoEnum.FALHA, t.Status);
        }

        [Fact]
        public void Processar_Transferencia_DeveDebitarOrigemECreditarDestino()
        {
            var origem = NovaConta(saldoDisponivel: 100m, limiteDeCredito: 0m);
            var destino = NovaConta(saldoDisponivel: 25m);
            var t = NovaTransacao(TipoOperacaoEnum.Transferencia, quantia: 40m, moeda: "BRL", origem: origem, destino: destino);

            var result = _processadorTransacaoDomainService.Processar(t);

            Assert.True(result.Sucesso);
            Assert.Equal(60m, origem.SaldoDisponivel);
            Assert.Equal(65m, destino.SaldoDisponivel);
            Assert.Equal(StatusTransacaoEnum.SUCESSO, t.Status);
        }

        [Fact]
        public void Processar_Estorno_DeveFalhar_QuandoTransacaoOriginalForNula()
        {
            var solicitacao = NovaSolicitacaoEstorno(original: null);

            var result = _processadorTransacaoDomainService.Processar(solicitacao);

            Assert.False(result.Sucesso);
            Assert.Contains("Transação original é obrigatória para estorno", result.Erros);
            Assert.Equal(StatusTransacaoEnum.FALHA, solicitacao.Status);
        }

        [Fact]
        public void Processar_Estorno_DeveFalhar_QuandoOriginalNaoEstiverComSucesso()
        {
            var origem = NovaConta(saldoDisponivel: 100m);
            var original = NovaTransacao(TipoOperacaoEnum.Debito, quantia: 10m, moeda: "BRL", origem: origem);
            original.Status = StatusTransacaoEnum.FALHA;

            var solicitacao = NovaSolicitacaoEstorno(original);

            var result = _processadorTransacaoDomainService.Processar(solicitacao);

            Assert.False(result.Sucesso);
            Assert.Contains("Apenas transações com sucesso podem ser estornadas", result.Erros);
            Assert.Equal(StatusTransacaoEnum.FALHA, solicitacao.Status);
        }

        [Fact]
        public void Processar_Estorno_DeveEstornarCredito_RetirandoDoSaldoDisponivel()
        {
            var origem = NovaConta(saldoDisponivel: 200m);
            var original = NovaTransacao(TipoOperacaoEnum.Credito, quantia: 50m, moeda: "BRL", origem: origem);
            original.Status = StatusTransacaoEnum.SUCESSO;

            var solicitacao = NovaSolicitacaoEstorno(original);

            var result = _processadorTransacaoDomainService.Processar(solicitacao);

            Assert.True(result.Sucesso);
            Assert.Equal(150m, origem.SaldoDisponivel);
            Assert.Equal(StatusTransacaoEnum.SUCESSO, solicitacao.Status);
        }

        [Fact]
        public void Processar_Estorno_DeveFalhar_EstornoCredito_QuandoContaOrigemAusenteNaOriginal()
        {
            var original = NovaTransacao(TipoOperacaoEnum.Credito, quantia: 50m, moeda: "BRL", origem: null);
            original.Status = StatusTransacaoEnum.SUCESSO;

            var solicitacao = NovaSolicitacaoEstorno(original);

            var result = _processadorTransacaoDomainService.Processar(solicitacao);

            Assert.False(result.Sucesso);
            Assert.Contains("Conta origem ausente na transação original (Crédito).", result.Erros);
            Assert.Equal(StatusTransacaoEnum.FALHA, solicitacao.Status);
        }

        [Fact]
        public void Processar_Estorno_DeveEstornarDebito_SomandoNoSaldoDisponivel()
        {
            var origem = NovaConta(saldoDisponivel: 20m);
            var original = NovaTransacao(TipoOperacaoEnum.Debito, quantia: 10m, moeda: "BRL", origem: origem);
            original.Status = StatusTransacaoEnum.SUCESSO;

            var solicitacao = NovaSolicitacaoEstorno(original);

            var result = _processadorTransacaoDomainService.Processar(solicitacao);

            Assert.True(result.Sucesso);
            Assert.Equal(30m, origem.SaldoDisponivel);
            Assert.Equal(StatusTransacaoEnum.SUCESSO, solicitacao.Status);
        }

        [Fact]
        public void Processar_Estorno_DeveEstornarReserva_MovendoReservadoParaDisponivel()
        {
            var origem = NovaConta(saldoDisponivel: 10m, saldoReservado: 40m);
            var original = NovaTransacao(TipoOperacaoEnum.Reserva, quantia: 15m, moeda: "BRL", origem: origem);
            original.Status = StatusTransacaoEnum.SUCESSO;

            var solicitacao = NovaSolicitacaoEstorno(original);

            var result = _processadorTransacaoDomainService.Processar(solicitacao);

            Assert.True(result.Sucesso);
            Assert.Equal(25m, origem.SaldoReservado);
            Assert.Equal(25m, origem.SaldoDisponivel);
            Assert.Equal(StatusTransacaoEnum.SUCESSO, solicitacao.Status);
        }

        [Fact]
        public void Processar_Estorno_DeveFalhar_EstornoReserva_QuandoSaldoReservadoInsuficiente_SemMutarSaldos()
        {
            var origem = NovaConta(saldoDisponivel: 10m, saldoReservado: 5m);
            var original = NovaTransacao(TipoOperacaoEnum.Reserva, quantia: 15m, moeda: "BRL", origem: origem);
            original.Status = StatusTransacaoEnum.SUCESSO;

            var solicitacao = NovaSolicitacaoEstorno(original);

            var result = _processadorTransacaoDomainService.Processar(solicitacao);

            Assert.False(result.Sucesso);
            Assert.Contains("Saldo reservado insuficiente para estornar a reserva.", result.Erros);

            // garante que não houve mutação ao falhar
            Assert.Equal(5m, origem.SaldoReservado);
            Assert.Equal(10m, origem.SaldoDisponivel);

            Assert.Equal(StatusTransacaoEnum.FALHA, solicitacao.Status);
        }

        [Fact]
        public void Processar_Estorno_DeveEstornarCaptura_SomandoNoSaldoDisponivel()
        {
            var origem = NovaConta(saldoDisponivel: 100m);
            var original = NovaTransacao(TipoOperacaoEnum.Captura, quantia: 20m, moeda: "BRL", origem: origem);
            original.Status = StatusTransacaoEnum.SUCESSO;

            var solicitacao = NovaSolicitacaoEstorno(original);

            var result = _processadorTransacaoDomainService.Processar(solicitacao);

            Assert.True(result.Sucesso);
            Assert.Equal(120m, origem.SaldoDisponivel);
            Assert.Equal(StatusTransacaoEnum.SUCESSO, solicitacao.Status);
        }

        [Fact]
        public void Processar_Estorno_DeveEstornarTransferencia_QuandoDestinoTemSaldo()
        {
            var origem = NovaConta(saldoDisponivel: 10m);
            var destino = NovaConta(saldoDisponivel: 100m);

            var original = NovaTransacao(TipoOperacaoEnum.Transferencia, quantia: 30m, moeda: "BRL", origem: origem, destino: destino);
            original.Status = StatusTransacaoEnum.SUCESSO;

            var solicitacao = NovaSolicitacaoEstorno(original);

            var result = _processadorTransacaoDomainService.Processar(solicitacao);

            Assert.True(result.Sucesso);
            Assert.Equal(70m, destino.SaldoDisponivel);
            Assert.Equal(40m, origem.SaldoDisponivel);
            Assert.Equal(StatusTransacaoEnum.SUCESSO, solicitacao.Status);
        }

        [Fact]
        public void Processar_Estorno_DeveFalhar_EstornoTransferencia_QuandoDestinoNaoTemSaldo_SemMutarSaldos()
        {
            var origem = NovaConta(saldoDisponivel: 10m);
            var destino = NovaConta(saldoDisponivel: 5m);

            var original = NovaTransacao(TipoOperacaoEnum.Transferencia, quantia: 30m, moeda: "BRL", origem: origem, destino: destino);
            original.Status = StatusTransacaoEnum.SUCESSO;

            var solicitacao = NovaSolicitacaoEstorno(original);

            var result = _processadorTransacaoDomainService.Processar(solicitacao);

            Assert.False(result.Sucesso);
            Assert.Contains("Conta destino sem saldo para estornar.", result.Erros);

            Assert.Equal(5m, destino.SaldoDisponivel);
            Assert.Equal(10m, origem.SaldoDisponivel);
            Assert.Equal(StatusTransacaoEnum.FALHA, solicitacao.Status);
        }

        [Fact]
        public void Processar_Estorno_DeveFalhar_QuandoTipoOriginalNaoSuportado()
        {
            var origem = NovaConta(saldoDisponivel: 10m);
            var original = NovaTransacao((TipoOperacaoEnum)999, quantia: 10m, moeda: "BRL", origem: origem);
            original.Status = StatusTransacaoEnum.SUCESSO;

            var solicitacao = NovaSolicitacaoEstorno(original);

            var result = _processadorTransacaoDomainService.Processar(solicitacao);

            Assert.False(result.Sucesso);
            Assert.Contains("Tipo de transação não suportado para estorno.", result.Erros);
            Assert.Equal(StatusTransacaoEnum.FALHA, solicitacao.Status);
        }
    }
}

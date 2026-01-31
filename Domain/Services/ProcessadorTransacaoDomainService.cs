using Domain.Base;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces.Services;

namespace Domain.Services
{
    public sealed class ProcessadorTransacaoDomainService : IProcessadorTransacaoDomainService
    {
        public DomainPatternGeneric<Transacao?> Processar(Transacao transacao)
        {
            var erros = new List<string>();

            if (!ValidarBasico(transacao, erros))
                return Erro(transacao, erros);

            return transacao.Tipo switch
            {
                TipoOperacaoEnum.Credito => ProcessarCredito(transacao),
                TipoOperacaoEnum.Debito => ProcessarDebito(transacao),
                TipoOperacaoEnum.Reserva => ProcessarReserva(transacao),
                TipoOperacaoEnum.Captura => ProcessarCaptura(transacao),
                TipoOperacaoEnum.Transferencia => ProcessarTransferencia(transacao),
                TipoOperacaoEnum.Estorno => ProcessarEstorno(transacao),
                _ => Erro(transacao, "Tipo de operação inválido")
            };
        }

        // =========================
        // OPERAÇÕES
        // =========================

        private DomainPatternGeneric<Transacao?> ProcessarCredito(Transacao t)
        {
            if (t.ContaOrigem is null)
                return Erro(t, "Conta origem é obrigatória para crédito");

            t.ContaOrigem.SaldoDisponivel += t.Quantia;
            MarcarSucesso(t);

            return DomainPatternGeneric<Transacao?>.SucessoBuilder(null);
        }

        private DomainPatternGeneric<Transacao?> ProcessarDebito(Transacao t)
        {
            if (t.ContaOrigem is null)
                return Erro(t, "Conta origem é obrigatória para débito");

            var novoSaldo = t.ContaOrigem.SaldoDisponivel - t.Quantia;

            if (novoSaldo < -t.ContaOrigem.LimiteDeCredito)
                return Erro(t, "Saldo insuficiente considerando o limite de crédito");

            t.ContaOrigem.SaldoDisponivel = novoSaldo;
            MarcarSucesso(t);

            return DomainPatternGeneric<Transacao?>.SucessoBuilder(null);
        }

        private DomainPatternGeneric<Transacao?> ProcessarReserva(Transacao t)
        {
            if (t.ContaOrigem is null)
                return Erro(t, "Conta origem é obrigatória para reserva");

            if (t.ContaOrigem.SaldoDisponivel < t.Quantia)
                return Erro(t, "Saldo disponível insuficiente para reserva");

            t.ContaOrigem.SaldoDisponivel -= t.Quantia;
            t.ContaOrigem.SaldoReservado += t.Quantia;
            MarcarSucesso(t);

            return DomainPatternGeneric<Transacao?>.SucessoBuilder(null);
        }

        private DomainPatternGeneric<Transacao?> ProcessarCaptura(Transacao t)
        {
            if (t.ContaOrigem is null)
                return Erro(t, "Conta origem é obrigatória para captura");

            if (t.ContaOrigem.SaldoReservado < t.Quantia)
                return Erro(t, "Saldo reservado insuficiente para captura");

            t.ContaOrigem.SaldoReservado -= t.Quantia;

            MarcarSucesso(t);

            return DomainPatternGeneric<Transacao?>.SucessoBuilder(null);
        }

        private DomainPatternGeneric<Transacao?> ProcessarTransferencia(Transacao t)
        {
            if (t.ContaOrigem is null || t.ContaDestino is null)
                return Erro(t, "Conta origem e destino são obrigatórias para transferência");

            var novoSaldoOrigem = t.ContaOrigem.SaldoDisponivel - t.Quantia;

            if (novoSaldoOrigem < -t.ContaOrigem.LimiteDeCredito)
                return Erro(t, "Saldo insuficiente na conta origem");

            t.ContaOrigem.SaldoDisponivel = novoSaldoOrigem;
            t.ContaDestino.SaldoDisponivel += t.Quantia;

            MarcarSucesso(t);
            return DomainPatternGeneric<Transacao?>.SucessoBuilder(null);
        }

        // =========================
        // ESTORNO
        // =========================

        private DomainPatternGeneric<Transacao?> ProcessarEstorno(Transacao solicitacao)
        {
            var original = solicitacao.TransacaoRevertida;

            if (original is null)
                return Erro(solicitacao, "Transação original é obrigatória para estorno");

            if (original.Status != StatusTransacaoEnum.SUCESSO)
                return Erro(solicitacao, "Apenas transações com sucesso podem ser estornadas");

            var estorno = CriarTransacaoEstorno(solicitacao, original);

            var resultado = AplicarEstorno(estorno, original);

            if (!resultado.Sucesso)
            {
                // Se deu erro aplicando estorno, marca a SOLICITAÇÃO como falha também.
                // (A transação "estorno" nem será adicionada no banco se você respeitar retorno.Sucesso na Application)
                return Erro(solicitacao, resultado.Erros);
            }

            MarcarSucesso(estorno);
            MarcarSucesso(solicitacao);

            return DomainPatternGeneric<Transacao?>.SucessoBuilder(estorno);
        }

        private DomainPatternGeneric<Transacao?> AplicarEstorno(Transacao estorno, Transacao original)
        {
            // Se preferir, você pode marcar erro no próprio "estorno" aqui, mas como ele ainda nem existe no banco,
            // o mais importante é retornar erro e também marcar a solicitacao como falha (feito acima).

            switch (original.Tipo)
            {
                case TipoOperacaoEnum.Credito:
                    if (original.ContaOrigem is null)
                        return ErroBuilderSemMutacao("Conta origem ausente na transação original (Crédito).");

                    original.ContaOrigem.SaldoDisponivel -= original.Quantia;
                    break;

                case TipoOperacaoEnum.Debito:
                    if (original.ContaOrigem is null)
                        return ErroBuilderSemMutacao("Conta origem ausente na transação original (Débito).");

                    original.ContaOrigem.SaldoDisponivel += original.Quantia;
                    break;

                case TipoOperacaoEnum.Reserva:
                    if (original.ContaOrigem is null)
                        return ErroBuilderSemMutacao("Conta origem ausente na transação original (Reserva).");

                    if (original.ContaOrigem.SaldoReservado < original.Quantia)
                        return ErroBuilderSemMutacao("Saldo reservado insuficiente para estornar a reserva.");

                    original.ContaOrigem.SaldoReservado -= original.Quantia;
                    original.ContaOrigem.SaldoDisponivel += original.Quantia;
                    break;

                case TipoOperacaoEnum.Captura:
                    if (original.ContaOrigem is null)
                        return ErroBuilderSemMutacao("Conta origem ausente na transação original (Captura).");

                    original.ContaOrigem.SaldoDisponivel += original.Quantia;
                    break;

                case TipoOperacaoEnum.Transferencia:
                    if (original.ContaOrigem is null || original.ContaDestino is null)
                        return ErroBuilderSemMutacao("Conta origem/destino ausente na transação original (Transferência).");

                    if (original.ContaDestino.SaldoDisponivel < original.Quantia)
                        return ErroBuilderSemMutacao("Conta destino sem saldo para estornar.");

                    original.ContaDestino.SaldoDisponivel -= original.Quantia;
                    original.ContaOrigem.SaldoDisponivel += original.Quantia;
                    break;

                default:
                    return ErroBuilderSemMutacao("Tipo de transação não suportado para estorno.");
            }

            return DomainPatternGeneric<Transacao?>.SucessoBuilder(null);
        }

        // =========================
        // HELPERS
        // =========================

        private static Transacao CriarTransacaoEstorno(Transacao solicitacao, Transacao original)
        {
            return new Transacao
            {
                Tipo = TipoOperacaoEnum.Estorno,
                Status = StatusTransacaoEnum.PENDENTE,
                Quantia = original.Quantia,
                Moeda = original.Moeda,
                ContaOrigem = original.ContaOrigem,
                ContaDestino = original.ContaDestino,
                ContaOrigemId = original.ContaOrigemId,
                ContaDestinoId = original.ContaDestinoId,
                TransacaoRevertida = original,
                TransacaoRevertidaId = original.Id,
                MetadataJson = solicitacao.MetadataJson
            };
        }

        private static bool ValidarBasico(Transacao t, List<string> erros)
        {
            if (t is null)
            {
                erros.Add("Transação não pode ser nula.");
                return false;
            }

            if (t.Quantia <= 0)
                erros.Add("Quantia deve ser maior que zero.");

            if (string.IsNullOrWhiteSpace(t.Moeda))
                erros.Add("Moeda é obrigatória.");

            return erros.Count == 0;
        }

        private static void MarcarSucesso(Transacao t)
        {
            t.Status = StatusTransacaoEnum.SUCESSO;
            t.MensagemErro = string.Empty;
        }

        private static void MarcarFalha(Transacao t, List<string> erros)
        {
            t.Status = StatusTransacaoEnum.FALHA;

            // guarda tudo (até 2000 chars) — se quiser só o primeiro, troque por erros.First()
            var msg = string.Join(" | ", erros.Where(e => !string.IsNullOrWhiteSpace(e)));

            // se seu campo no banco é 2000, garante não estourar
            if (msg.Length > 2000) msg = msg.Substring(0, 2000);

            t.MensagemErro = msg;
        }

        private static DomainPatternGeneric<Transacao?> Erro(Transacao t, string erro)
        {
            var erros = new List<string> { erro };
            MarcarFalha(t, erros);
            return DomainPatternGeneric<Transacao?>.ErroBuilder(erros);
        }

        private static DomainPatternGeneric<Transacao?> Erro(Transacao t, List<string> erros)
        {
            var errosValidos = erros
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .ToList();

            if (errosValidos.Count == 0)
                errosValidos.Add("Falha desconhecida.");

            MarcarFalha(t, errosValidos);
            return DomainPatternGeneric<Transacao?>.ErroBuilder(errosValidos);
        }

        // Usado quando não queremos mutar uma Transacao específica (ex.: erro interno do AplicarEstorno)
        private static DomainPatternGeneric<Transacao?> ErroBuilderSemMutacao(string erro)
            => DomainPatternGeneric<Transacao?>.ErroBuilder(erro);
    }
}

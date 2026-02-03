export interface CriarTransacaoRequest {
  operacao: number;
  contaOrigemId: number;
  contaDestinoId?: number | null;
  transacaoEstornadaId?: number | null;
  quantia: number;
  moeda: string;
}

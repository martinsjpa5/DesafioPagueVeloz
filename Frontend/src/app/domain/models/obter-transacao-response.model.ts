export interface ObterTransacaoResponse {
  id: number;
  tipo: string;
  status: string;
  quantia: number;
  moeda: string;
  transacaoEstornadaId?: number | null;
  contaDestinoId?: number | null;
  nomeClienteContaDestino?: string | null;
}
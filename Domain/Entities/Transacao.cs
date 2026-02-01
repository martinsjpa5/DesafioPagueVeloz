using Domain.Enums;

namespace Domain.Entities
{
    public class Transacao : EntidadeBase
    {
        public TipoOperacaoEnum Tipo { get; set; }
        public StatusTransacaoEnum Status { get; set; }
        public decimal Quantia { get; set; }
        public string Moeda { get; set; }
        public string MetadataJson { get; set; }
        public string MensagemErro { get; set; }
        public Conta ContaOrigem { get; set; }
        public Conta ContaDestino { get; set; }
        public int ContaOrigemId { get; set; }
        public int? ContaDestinoId { get; set; }
        public int? TransacaoEstornadaId { get; set; }
        public Transacao TransacaoEstornada { get; set; }


    }
}

using Domain.Enums;

namespace Domain.Entities
{
    public class Transacao : EntidadeBase
    {
        public TipoOperacaoEnum Tipo { get; set; }
        public string ContaId { get; set; }
        public decimal Quantia { get; set; }
        public string Moeda { get; set; }
        public string MetadataJson { get; set; }
        public Guid ReferenciaId { get; set; }
        public string MensagemErro { get; set; }

    }
}


using Domain.Enums;

namespace Application.Dtos.Responses
{
    public class ObterTransacaoResponse
    {
        public int Id { get; set; }
        public string Tipo { get; set; }
        public string Status { get; set; }
        public decimal Quantia { get; set; }
        public string Moeda { get; set; }
        public int? TransacaoEstornadaId { get; set; }
        public int? ContaDestinoId { get; set; }
        public string? NomeClienteContaDestino { get; set; }
        public string? MensagemErro { get; set; }
    }
}

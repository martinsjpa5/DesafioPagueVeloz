
namespace Application.Dtos.Requests
{
    public class CriarTransacaoRequest
    {
        public int Operacao { get; set; }
        public int ContaOrigemId { get; set; }
        public int? ContaDestinoId { get; set; }
        public int? TransacaoRevertidaId { get; set; }
        public decimal Quantia { get; set; }
        public string Moeda { get; set; }
    }
}

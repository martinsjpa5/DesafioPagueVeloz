
namespace Application.Dtos.Requests
{
    public class CriarTransacaoRequest
    {
        public int Operacao { get; set; }
        public int ContaId { get; set; }
        public decimal Quantia { get; set; }
        public string Moeda { get; set; }
    }
}

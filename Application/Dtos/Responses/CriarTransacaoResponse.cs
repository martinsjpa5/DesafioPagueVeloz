
namespace Application.Dtos.Responses
{
    public class CriarTransacaoResponse
    {
        public int Id { get; set; }
        public decimal SaldoDisponivel { get; set; }
        public decimal SaldoReservado { get; set; }
        public decimal SaldoTotal { get; set; }
        public string MensagemErro { get; set; }
        public DateTime Data { get; set; }
    }
}

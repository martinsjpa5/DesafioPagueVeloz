using Domain.Enums;

namespace Application.Dtos.Responses
{
    public class ObterContaResponse
    {
        public int Id { get; set; }
        public decimal SaldoDisponivel { get; set; }
        public decimal SaldoReservado { get; set; }
        public decimal LimiteDeCredito { get; set; }
        public string Status { get; set; }
    }
}

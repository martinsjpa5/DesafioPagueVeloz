using Domain.Enums;
using Domain.Interfaces.Caching;

namespace Domain.Models
{
    public class ContaModel : ICommonCaching
    {
        public required int ContaId { get; set; }
        public required int ClienteId { get; set; }
        public decimal SaldoDisponivel { get; set; }
        public decimal SaldoReservado { get; set; }
        public decimal LimiteDeCredito { get; set; }
        public string Status { get; set; }

        public string ObterKey()
        {
            return nameof(ContaModel) + ":" + ClienteId + ":" + ContaId;
        }

    }
}

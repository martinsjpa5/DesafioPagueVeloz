
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class Conta : EntidadeBase
    {
        public string NumeroConta { get; set; }
        public decimal SaldoDisponivel { get; set; }
        public decimal SaldoReservado { get; set; }
        public decimal LimiteDeCredito { get; set; }
        public decimal Status { get; set; }
        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public DateTime DataAtualizacao { get; set; }
    }
}

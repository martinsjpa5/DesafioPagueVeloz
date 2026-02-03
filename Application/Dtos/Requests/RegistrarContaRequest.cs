
using System.ComponentModel.DataAnnotations;

namespace Application.Dtos.Requests
{
    public class RegistrarContaRequest
    {
        [Required(ErrorMessage = "O campo Saldo Inicial é obrigatório.")]
        public decimal SaldoInicial { get; set; }
        [Required(ErrorMessage = "O campo Limite de Credito é obrigatório.")]
        public decimal LimiteDeCredito { get; set; }
    }
}


using System.ComponentModel.DataAnnotations;

namespace Application.Dtos.Requests
{
    public class CriarTransacaoRequest
    {
        [Required(ErrorMessage = "O campo Operacao é obrigatório.")]
        public int Operacao { get; set; }
        [Required(ErrorMessage = "O campo Conta Origem é obrigatório.")]
        public int ContaOrigemId { get; set; }
        public int? ContaDestinoId { get; set; }
        public int? TransacaoEstornadaId { get; set; }
        [Required(ErrorMessage = "O campo Quantia é obrigatório.")]
        public decimal Quantia { get; set; }
        [Required(ErrorMessage = "O campo Moeda é obrigatório.")]
        public string Moeda { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace Application.Dtos.Requests
{
    public class LogarRequest
    {
        [Required(ErrorMessage = "O campo Email é obrigatório.")]
        public string Email { get; set; }
        [Required(ErrorMessage = "O campo Senha é obrigatório.")]
        public string Senha { get; set; }
    }
}


namespace Application.Dtos.Responses
{
    public class ObterClienteResponse
    {
        public string Nome { get; set; }
        public IEnumerable<int> Contas { get; set; }
    }
}

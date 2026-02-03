
namespace Domain.Entities
{
    public class Cliente  : EntidadeBase
    {
        public string Nome { get; set; }
        public ICollection<Conta> Contas { get; set; }
    }
}

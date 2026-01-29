
namespace Domain.Entities
{
    public class Cliente  : EntidadeBase
    {
        public ICollection<Conta> Contas { get; set; }
    }
}

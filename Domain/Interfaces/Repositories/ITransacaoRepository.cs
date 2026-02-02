
using Domain.Entities;

namespace Domain.Interfaces.Repositories
{
    public interface ITransacaoRepository
    {
        Task<Transacao?> ObterTransacaoPendenteAsync(int id);
        Task<List<Transacao>> ObterTransacoesPassiveisDeEstornoAsync(int contaId, int clienteId);
    }
}

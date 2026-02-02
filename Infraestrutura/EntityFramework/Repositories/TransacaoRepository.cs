using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces.Repositories;
using Infraestrutura.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;

namespace Infraestrutura.EntityFramework.Repositories
{
    public class TransacaoRepository : ITransacaoRepository
    {
        protected readonly AppDbContext _dataContext;

        public TransacaoRepository(AppDbContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<Transacao?> ObterTransacaoPendenteAsync(int id)
        {
            var result = await _dataContext.Set<Transacao>().Where(x => x.Id == id && x.Status == Domain.Enums.StatusTransacaoEnum.PENDENTE).Include(x => x.ContaOrigem).Include(x => x.ContaDestino).Include(x => x.TransacaoEstornada).AsTracking().FirstOrDefaultAsync();

            return result;
        }

        public async Task<List<Transacao>> ObterTransacoesPassiveisDeEstornoAsync(int contaId, int clienteId)
        {
            var query = _dataContext.Set<Transacao>()
                .AsNoTracking()
                .Where(t =>
                    t.ContaOrigemId == contaId &&
                    t.ContaOrigem.ClienteId == clienteId &&
                    t.Status == StatusTransacaoEnum.SUCESSO &&
                    t.Tipo != TipoOperacaoEnum.Estorno
                )
                .Where(t => !_dataContext.Set<Transacao>().Any(e =>
                    e.Tipo == TipoOperacaoEnum.Estorno &&
                    e.Status == StatusTransacaoEnum.SUCESSO &&
                    e.TransacaoEstornadaId == t.Id
                ))
                .Include(t => t.ContaDestino)
                    .ThenInclude(cd => cd.Cliente);

            return await query.ToListAsync();
        }


    }
}

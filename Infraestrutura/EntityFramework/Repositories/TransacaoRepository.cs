using Domain.Entities;
using Domain.Interfaces.Repositories;
using Infraestrutura.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;
using System;

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
            var result = await _dataContext.Set<Transacao>().Where(x => x.Id == id && x.Status == Domain.Enums.StatusTransacaoEnum.PENDENTE).Include(x => x.ContaOrigem).Include(x => x.ContaDestino).Include(x => x.TransacaoRevertida).AsTracking().FirstOrDefaultAsync();

            return result;
         }

    }
}

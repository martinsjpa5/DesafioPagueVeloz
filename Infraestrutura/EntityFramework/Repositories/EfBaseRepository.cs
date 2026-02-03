
using Domain.Entities;
using Infraestrutura.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace Infraestrutura.EntidadeBaseFramework.Repositories
{
    public class EfBaseRepository : IEfBaseRepository, IDisposable
    {
        protected readonly AppDbContext _dataContext;
        private bool disposed = false;

        public EfBaseRepository(AppDbContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<bool> AdicionarEntidadeBaseAsync<T>(T EntidadeBase) where T : EntidadeBase
        {
            await _dataContext.AddAsync(EntidadeBase);
            return true;
        }
        public async Task<bool> AdicionarEntidadeBasesAsync<T>(List<T> EntidadeBases) where T : EntidadeBase
        {
            await _dataContext.AddRangeAsync(EntidadeBases);
            return true;
        }

        public bool DeletarEntidadeBase<T>(T EntidadeBase) where T : EntidadeBase
        {
            _dataContext.Remove(EntidadeBase);
            return true;
        }

        public bool RastrearEntidadeBase<T>(T EntidadeBase) where T : EntidadeBase
        {
            _dataContext.Attach(EntidadeBase);
            return true;
        }


        public async Task<T?> ObterPorCondicaoAsync<T>(Expression<Func<T, bool>> predicate) where T : EntidadeBase
        {
            var result = await _dataContext.Set<T>().FirstOrDefaultAsync(predicate);
            return result;
        }

        public async Task<ICollection<T>> ObterTodosPorCondicaoAsync<T>(Expression<Func<T, bool>> predicate) where T : EntidadeBase
        {
            var result = await _dataContext.Set<T>().Where(predicate).ToListAsync();
            return result;
        }

        public async Task<ICollection<T>> ObterTodosPorCondicaoAsync<T>(
    Expression<Func<T, bool>> predicate,
    params Func<IQueryable<T>, IIncludableQueryable<T, object>>[] includes
) where T : EntidadeBase
        {
            IQueryable<T> query = _dataContext.Set<T>().Where(predicate);


            foreach (var include in includes)
            {
                query = include(query);
            }

            var result = await query.ToListAsync();
            return result;
        }

        public async Task<int> SalvarAlteracoesAsync()
        {
            return await _dataContext.SaveChangesAsync();
        }

        public async Task<ICollection<T>> ObterTodosAsync<T>(params Func<IQueryable<T>, IIncludableQueryable<T, object>>[] includes) where T : EntidadeBase
        {
            var query = _dataContext.Set<T>().AsQueryable();

            foreach (var include in includes)
            {
                query = include(query);
            }

            var result = await query.ToListAsync();
            return result;
        }

        public async Task<bool> EntidadeExisteAsync<T>(Expression<Func<T, bool>> predicate) where T : EntidadeBase
        {
            var result = await _dataContext.Set<T>().AnyAsync(predicate);

            return result;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _dataContext.Dispose();
                }
            }
            this.disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}

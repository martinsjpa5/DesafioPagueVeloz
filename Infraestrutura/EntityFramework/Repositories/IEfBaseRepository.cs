using Domain.Entities;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace Infraestrutura.EntidadeBaseFramework.Repositories
{
    public interface IEfBaseRepository
    {
        bool RastrearEntidadeBase<T>(T EntidadeBase) where T : EntidadeBase;
        Task<bool> AdicionarEntidadeBaseAsync<T>(T EntidadeBase) where T : EntidadeBase;
        Task<bool> AdicionarEntidadeBasesAsync<T>(List<T> EntidadeBases) where T : EntidadeBase;
        bool RastrearEntidadeBases<T>(ICollection<T> EntidadeBases) where T : EntidadeBase;

        bool DeletarEntidadeBase<T>(T EntidadeBase) where T : EntidadeBase;
        Task<ICollection<T>> ObterTodosAsync<T>() where T : EntidadeBase;
        Task<T?> ObterPorIdAsync<T>(int id) where T : EntidadeBase;
        Task<ICollection<T>> ObterPorIdsAsync<T>(ICollection<int> ids) where T : EntidadeBase;
        Task<T?> ObterPorCondicaoAsync<T>(Expression<Func<T, bool>> predicate) where T : EntidadeBase;
        Task<ICollection<T>> ObterTodosPorCondicaoAsync<T>(Expression<Func<T, bool>> predicate) where T : EntidadeBase;
        Task<ICollection<T>> ObterTodosPorCondicaoAsync<T>(Expression<Func<T, bool>> predicate,
            params Func<IQueryable<T>, IIncludableQueryable<T, object>>[] includes
        ) where T : EntidadeBase;

        Task<bool> EntidadeBaseExisteAsync<T>(int id) where T : EntidadeBase;
        Task<int> SalvarAlteracoesAsync();
        Task<ICollection<T>> ObterTodosAsync<T>(params Func<IQueryable<T>, IIncludableQueryable<T, object>>[] includes) where T : EntidadeBase;
        Task<T?> ObterPorIdAsync<T>(int id, params Func<IQueryable<T>, IIncludableQueryable<T, object>>[] includes) where T : EntidadeBase;
    }
}

using Domain.Interfaces.Caching;

namespace Domain.Interfaces.Repositories
{
    public interface ICommonCachingRepository
    {

        Task<T?> GetAsync<T>(string key) where T : ICommonCaching;
        Task<bool> SetAsync<T>(T value, TimeSpan expiration) where T : ICommonCaching;
        Task<bool> RemoveAsync<T>(T value) where T : ICommonCaching;

    }
}

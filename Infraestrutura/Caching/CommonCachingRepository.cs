using Domain.Interfaces.Caching;
using Domain.Interfaces.Repositories;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infraestrutura.Caching
{
    public class CommonCachingRepository : ICommonCachingRepository
    {
        private readonly IDistributedCache _cache;

        public CommonCachingRepository(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<T?> GetAsync<T>(string key) where T : ICommonCaching
        {
            string? resultString = await _cache.GetStringAsync(key);

            if (resultString == null)
            {
                return default;
            }

            T? resultModel = JsonConvert.DeserializeObject<T>(resultString);

            return resultModel;
        }

        public async Task<bool> SetAsync<T>(T value, TimeSpan expiration) where T : ICommonCaching
        {
            string stringValue = JsonConvert.SerializeObject(value);

            DistributedCacheEntryOptions options = new()
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            await _cache.SetStringAsync(value.ObterKey(), stringValue, options);

            return true;
        }

        public async Task<bool> RemoveAsync<T>(T value) where T : ICommonCaching
        {
            await _cache.RemoveAsync(value.ObterKey());

            return true;
        }

    }
}

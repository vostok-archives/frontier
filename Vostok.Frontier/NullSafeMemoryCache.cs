using System;
using Microsoft.Extensions.Caching.Memory;
using Vostok.Commons.Extensions.UnitConvertions;

namespace Vostok.Frontier
{
    public class NullSafeMemoryCache : IDisposable
    {
        private readonly MemoryCache memoryCache;
        private readonly object nullPlaceholder;

        public NullSafeMemoryCache()
        {
            memoryCache = new MemoryCache(new MemoryCacheOptions { ExpirationScanFrequency = 1.Hours()});
            nullPlaceholder = new object();
        }

        public bool Get<T>(string key, out T data)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            data = default(T);

            var cached = memoryCache.Get(key);
            if (cached == null)
                return false;

            if (!ReferenceEquals(nullPlaceholder, cached))
                data = (T)cached;

            return true;
        }

        public void Set<T>(string key, T data, TimeSpan ttl)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            var expireAt = DateTimeOffset.Now + ttl;

            memoryCache.Set(key, data != null ? data : nullPlaceholder, expireAt);
        }

        public void Del(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            memoryCache.Remove(key);
        }

        public void Dispose()
        {
            memoryCache.Dispose();
        }
    }
}
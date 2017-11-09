using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFCache
{
    public static class CacheConfiguration
    {
        private static ICache _instance;
        public static ICache Instance
        {
            get { return _instance; }
        }

        static CacheConfiguration()
        {
            _instance = new InMemoryCache();
        }

        public static void ReplaceCache<TCache>()
            where TCache : ICache
        {
            _instance = Activator.CreateInstance<TCache>() as ICache;
        }

        public static void ReplaceCache<TCache>(TCache cache)
            where TCache : ICache
        {
            _instance = Activator.CreateInstance(cache.GetType()) as ICache;
        }
    }
}

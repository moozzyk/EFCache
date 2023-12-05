// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace EFCache
{
    using System.Data.Entity;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Infrastructure.Interception;

    public static class EntityFrameworkCache
    {
        public static void Initialize(ICache cache) => Initialize(cache, new CachingPolicy());

        public static void Initialize(ICache cache, CachingPolicy cachingPolicy)
        {
            var transactionHandler = new CacheTransactionHandler(cache);

            DbConfiguration.Loaded +=
                (sender, args) => args.ReplaceService<DbProviderServices>(
                    (dbServices, _) => new CachingProviderServices(dbServices, transactionHandler, cachingPolicy));
            DbInterception.Add(transactionHandler);
        }

        public static void Initialize(ICacheProvider cache) => Initialize(cache, new CachingPolicy());
        public static void Initialize(ICacheProvider cache, CachingPolicy cachingPolicy)
        {
            var transactionHandler = new CacheTransactionHandler(cache);

            DbConfiguration.Loaded +=
                (sender, args) => args.ReplaceService<DbProviderServices>(
                    (dbServices, _) => new CachingProviderServices(dbServices, transactionHandler, cachingPolicy));
            DbInterception.Add(transactionHandler);
        }
    }
}

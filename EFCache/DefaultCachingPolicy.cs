
namespace EFCache
{
    using System;
    using System.Collections.ObjectModel;
    using System.Data.Entity.Core.Metadata.Edm;

    public class DefaultCachingPolicy : CachingPolicy
    {
        protected internal override bool CanBeCached(ReadOnlyCollection<EntitySetBase> affectedEntitySets)
        {
            return true;
        }

        protected internal override void GetCacheableRows(ReadOnlyCollection<EntitySetBase> affectedEntitySets, out int minCacheableRows, out int maxCacheableRows)
        {
            minCacheableRows = 0;
            maxCacheableRows = int.MaxValue;
        }

        protected internal override void GetExpirationTimeout(ReadOnlyCollection<EntitySetBase> affectedEntitySets, out TimeSpan slidingExpiration, out DateTimeOffset absoluteExpiration)
        {
            slidingExpiration = TimeSpan.MaxValue;
            absoluteExpiration = DateTimeOffset.MaxValue;
        }
    }
}

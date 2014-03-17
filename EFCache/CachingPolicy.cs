namespace EFCache
{
    using System;
    using System.Collections.ObjectModel;
    using System.Data.Entity.Core.Metadata.Edm;


    /// <summary>
    /// Caching policy.
    /// </summary>
    public abstract class CachingPolicy
    {
        /// <summary>
        /// Determines whether the specified command definition can be cached.
        /// </summary>
        /// /// <param name="affectedEntitySets">Entity sets affected by the command.</param>
        /// <returns>
        /// A value of <c>true</c> if the specified command definition can be cached; otherwise, <c>false</c>.
        /// </returns>
        protected internal abstract bool CanBeCached(ReadOnlyCollection<EntitySetBase> affectedEntitySets);

        /// <summary>
        /// Gets the minimum and maximum number cacheable rows for a given command definition.
        /// </summary>
        /// <param name="affectedEntitySets">Entity sets affected by the command.</param>
        /// <param name="minCacheableRows">The minimum number of cacheable rows.</param>
        /// <param name="maxCacheableRows">The maximum number of cacheable rows.</param>
        protected internal abstract void GetCacheableRows(ReadOnlyCollection<EntitySetBase> affectedEntitySets, out int minCacheableRows, out int maxCacheableRows);

        /// <summary>
        /// Gets the expiration timeout for a given command definition.
        /// </summary>
        /// <param name="affectedEntitySets">Entity sets affected by the command.</param>
        /// <param name="slidingExpiration">The sliding expiration time.</param>
        /// <param name="absoluteExpiration">The absolute expiration time.</param>
        protected internal abstract void GetExpirationTimeout(ReadOnlyCollection<EntitySetBase> affectedEntitySets,
            out TimeSpan slidingExpiration, out DateTimeOffset absoluteExpiration);
    }
}

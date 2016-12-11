// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

// Created based on the CachingPolicy.cs from Tracing and Caching Provider Wrappers for Entity Framework sample to make it easy to port exisiting applications

namespace EFCache
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Linq;

    /// <summary>
    /// Caching policy.
    /// </summary>
    public class CachingPolicy
    {
        private readonly HashSet<string> _cachedEntitySets;

        /// <summary>
        /// Initializes a new instance of the <code>CachingPolicy</code> class that allows caching results for all queries.
        /// </summary>
        public CachingPolicy()
        { }

        /// <summary>
        /// Initializes a new instance of the <code>CachingPolicy</code> class that allows caching only results for queries
        /// coming from store entity sets (tables) specified in <paramref name="cachedEntitySets"/>
        /// </summary>
        /// <param name="cachedEntitySets">
        /// Names of store entity sets (tables) for which results will be cached.
        /// </param>
        public CachingPolicy(IEnumerable<string> cachedEntitySets)
        {
            if (cachedEntitySets == null)
            {
                throw new ArgumentNullException(nameof(cachedEntitySets));
            }

            _cachedEntitySets = new HashSet<string>(cachedEntitySets);
        }

        /// <summary>
        /// Determines whether the specified command definition can be cached.
        /// </summary>
        /// <param name="affectedEntitySets">Entity sets affected by the command.</param>
        /// <param name="sql">SQL statement for the command.</param>
        /// <param name="parameters">Command parameters.</param>
        /// <returns>
        /// <c>true</c> if the specified command definition can be cached; otherwise, <c>false</c>.
        /// </returns>
        protected internal virtual bool CanBeCached(ReadOnlyCollection<EntitySetBase> affectedEntitySets, string sql,
            IEnumerable<KeyValuePair<string, object>> parameters)
        {
            if (_cachedEntitySets == null)
            {
                return true;
            }

            return _cachedEntitySets.IsSupersetOf(affectedEntitySets.Select(e => e.Table ?? e.Name));
        }

        /// <summary>
        /// Gets the minimum and maximum number cacheable rows for a given command definition.
        /// </summary>
        /// <param name="affectedEntitySets">Entity sets affected by the command.</param>
        /// <param name="minCacheableRows">The minimum number of cacheable rows.</param>
        /// <param name="maxCacheableRows">The maximum number of cacheable rows.</param>
        protected internal virtual void GetCacheableRows(ReadOnlyCollection<EntitySetBase> affectedEntitySets,
            out int minCacheableRows, out int maxCacheableRows)
        {
            minCacheableRows = 0;
            maxCacheableRows = int.MaxValue;
        }

        /// <summary>
        /// Gets the expiration timeout for a given command definition.
        /// </summary>
        /// <param name="affectedEntitySets">Entity sets affected by the command.</param>
        /// <param name="slidingExpiration">The sliding expiration time.</param>
        /// <param name="absoluteExpiration">The absolute expiration time.</param>
        protected internal virtual void GetExpirationTimeout(ReadOnlyCollection<EntitySetBase> affectedEntitySets,
            out TimeSpan slidingExpiration, out DateTimeOffset absoluteExpiration)
        {
            slidingExpiration = TimeSpan.MaxValue;
            absoluteExpiration = DateTimeOffset.MaxValue;
        }
    }
}

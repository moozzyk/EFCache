// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

// Created based on the CachingPolicy.cs from Tracing and Caching Provider Wrappers for Entity Framework sample to make it easy to port exisiting applications

namespace EFCache
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Data.Entity.Core.Metadata.Edm;

    /// <summary>
    /// Caching policy.
    /// </summary>
    public abstract class CachingPolicy
    {
        protected readonly HashSet<string> BlackListedQueries = new HashSet<string>();

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
            return !BlackListedQueries.Contains(sql);
        }

        /// <summary>
        /// Gets the minimum and maximum number cacheable rows for a given command definition.
        /// </summary>
        /// <param name="affectedEntitySets">Entity sets affected by the command.</param>
        /// <param name="minCacheableRows">The minimum number of cacheable rows.</param>
        /// <param name="maxCacheableRows">The maximum number of cacheable rows.</param>
        protected internal abstract void GetCacheableRows(ReadOnlyCollection<EntitySetBase> affectedEntitySets, 
            out int minCacheableRows, out int maxCacheableRows);

        /// <summary>
        /// Gets the expiration timeout for a given command definition.
        /// </summary>
        /// <param name="affectedEntitySets">Entity sets affected by the command.</param>
        /// <param name="slidingExpiration">The sliding expiration time.</param>
        /// <param name="absoluteExpiration">The absolute expiration time.</param>
        protected internal abstract void GetExpirationTimeout(ReadOnlyCollection<EntitySetBase> affectedEntitySets, out TimeSpan slidingExpiration, out DateTimeOffset absoluteExpiration);

        /// <summary>
        /// Adds the query to a list of queries that should not be cached.
        /// </summary>
        /// <param name="sql">Query to be added to the list of queries that should not be cached.</param>
        public void AddBlacklistedQuery(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentNullException("sql");
            }

            BlackListedQueries.Add(sql);
        }

        /// <summary>
        /// Removes the query frome a list of queries that should not be cached.
        /// </summary>
        /// <param name="sql">Query to be removed from the list of queries that should not be cached.</param>
        /// <returns><c>true</c> if query was on the list. Otherwise <c>false</c>.</returns>
        public bool RemoveBlacklistedQuery(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentNullException("sql");
            }

            return BlackListedQueries.Remove(sql);
        }
    }
}

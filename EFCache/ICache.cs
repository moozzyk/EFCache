// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

// Created based on the ICache.cs from Tracing and Caching Provider Wrappers for Entity Framework sample to make it easy to port exisiting applications

namespace EFCache
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Interface to be implemented by cache implementations.
    /// </summary>
    public interface ICache
    {
        /// <summary>
        /// Tries to the get cached entry by key.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The retrieved value.</param>
        /// <returns>A value of <c>true</c> if entry was found in the cache, <c>false</c> otherwise.</returns>
        bool GetItem(string key, out object value);

        /// <summary>
        /// Adds the specified entry to the cache.
        /// </summary>
        /// <param name="key">The entry key.</param>
        /// <param name="value">The entry value.</param>
        /// <param name="dependentEntitySets">The list of dependent entity sets.</param>
        /// <param name="slidingExpiration">The sliding expiration.</param>
        /// <param name="absoluteExpiration">The absolute expiration.</param>
        void PutItem(string key, object value, IEnumerable<string> dependentEntitySets, TimeSpan slidingExpiration, DateTimeOffset absoluteExpiration);

        /// <summary>
        /// Invalidates all cache entries which are dependent on any of the specified entity sets.
        /// </summary>
        /// <param name="entitySets">The entity sets.</param>
        void InvalidateSets(IEnumerable<string> entitySets);

        /// <summary>
        /// Invalidates cache entry with a given key.
        /// </summary>
        /// <param name="key">The cache key.</param>
        void InvalidateItem(string key);
    }
}

// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace EFCache
{
    using System;
    using System.Data.Entity.Core.Objects;
    using System.Data.Entity.Infrastructure;
    using System.Linq;
    using System.Reflection;

    public static class QueryableExtensions
    {
        /// <summary>
        /// Marks the query as non-cacheable.
        /// </summary>
        /// <typeparam name="T">Query element type.</typeparam>
        /// <param name="source">Query whose results won't be cached. Must not be null.</param>
        public static IQueryable<T> NotCached<T>(this IQueryable<T> source)
            where T : class
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");    
            }

            var objectQuery = TryGetObjectQuery(source) ?? source as ObjectQuery;

            if (objectQuery != null)
            {
                BlacklistedQueriesRegistrar.Instance.AddBlacklistedQuery(
                    objectQuery.Context.MetadataWorkspace, objectQuery.ToTraceString());
            }

            return source;
        }

        /// <summary>
        /// Forces query results to be always cached. Overrides caching policy settings and blacklisted queries. 
        /// Allows caching results for queries using functions with side effects. 
        /// </summary>
        /// <typeparam name="T">Query element type.</typeparam>
        /// <param name="source">Query whose results will always be cached. Must not be null.</param>
        public static IQueryable<T> Cached<T>(this IQueryable<T> source)
            where T : class
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            var objectQuery = TryGetObjectQuery(source) ?? source as ObjectQuery;

            if (objectQuery != null)
            {
                AlwaysCachedQueriesRegistrar.Instance.AddCachedQuery(
                    objectQuery.Context.MetadataWorkspace, objectQuery.ToTraceString());
            }

            return source;
        }

        private static ObjectQuery TryGetObjectQuery<T>(IQueryable<T> source)
        {
            var dbQuery = source as DbQuery<T>;

            if (dbQuery != null)
            {
                const BindingFlags privateFieldFlags = BindingFlags.NonPublic | BindingFlags.Instance;

                var internalQuery =
                    source.GetType().GetField("_internalQuery", privateFieldFlags)
                        .GetValue(source);

                return
                    (ObjectQuery)internalQuery.GetType().GetField("_objectQuery", privateFieldFlags)
                        .GetValue(internalQuery);
            }

            return null;
        }
    }
}
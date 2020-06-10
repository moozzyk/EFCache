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
                throw new ArgumentNullException(nameof(source));
            }

            var objectQuery = TryGetObjectQuery(source) ?? source as ObjectQuery;

            if (objectQuery != null)
            {
                BlockedQueriesRegistrar.Instance.AddBlockedQuery(
                    objectQuery.Context.MetadataWorkspace, objectQuery.ToTraceString());
            }

            return source;
        }

        /// <summary>
        /// Forces query results to be always cached. Overrides caching policy settings and blocked queries.
        /// Allows caching results for queries using non-deterministic functions.
        /// </summary>
        /// <typeparam name="T">Query element type.</typeparam>
        /// <param name="source">Query whose results will always be cached. Must not be null.</param>
        public static IQueryable<T> Cached<T>(this IQueryable<T> source)
            where T : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var objectQuery = TryGetObjectQuery(source) ?? source as ObjectQuery;

            if (objectQuery != null)
            {
                AlwaysCachedQueriesRegistrar.Instance.AddCachedQuery(
                    objectQuery.Context.MetadataWorkspace, objectQuery.ToTraceString());
            }

            return source;
        }

        public static T FirstNotCached<T>(this IQueryable<T> source)
            where T : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.Take(1).NotCached().ToArray().First();
        }

        public static T FirstOrDefaultNotCached<T>(this IQueryable<T> source)
            where T : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.Take(1).NotCached().ToArray().FirstOrDefault();
        }

        public static T SingleNotCached<T>(this IQueryable<T> source)
            where T : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.Take(2).NotCached().ToArray().Single();
        }

        public static T SingleOrDefaultNotCached<T>(this IQueryable<T> source)
            where T : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.Take(2).NotCached().ToArray().SingleOrDefault();
        }

        private static ObjectQuery TryGetObjectQuery<T>(IQueryable<T> source)
        {
            if (source is DbQuery<T>)
            {
                const BindingFlags privateFieldFlags =
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;

                var internalQuery =
                    source.GetType().GetProperty("InternalQuery", privateFieldFlags)
                        .GetValue(source, null);

                return
                    (ObjectQuery)internalQuery.GetType().GetProperty("ObjectQuery", privateFieldFlags)
                        .GetValue(internalQuery, null);
            }

            return null;
        }
    }
}
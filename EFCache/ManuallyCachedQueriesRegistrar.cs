// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

using System;

namespace EFCache
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data.Entity.Core.Metadata.Edm;

    public sealed class ManuallyCachedQueriesRegistrar
    {
        public static readonly ManuallyCachedQueriesRegistrar Instance = new ManuallyCachedQueriesRegistrar();

        private readonly ConcurrentDictionary<MetadataWorkspace, HashSet<string>> _cachedQueries =
            new ConcurrentDictionary<MetadataWorkspace, HashSet<string>>();

        private ManuallyCachedQueriesRegistrar()
        {
        }

        public void AddCachedQuery(MetadataWorkspace workspace, string sql)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException("workspace");
            }

            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentNullException("sql");
            }

            var queries = _cachedQueries.GetOrAdd(workspace, new HashSet<string>());
            lock (queries)
            {
                queries.Add(sql);
            }
        }

        public bool RemoveCachedQuery(MetadataWorkspace workspace, string sql)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException("workspace");
            }

            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentNullException("sql");
            }

            HashSet<string> queries;
            if (_cachedQueries.TryGetValue(workspace, out queries))
            {
                lock (queries)
                {
                    return queries.Remove(sql);
                }
            }

            return false;
        }

        public bool IsQueryCached(MetadataWorkspace workspace, string sql)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException("workspace");
            }

            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentNullException("sql");
            }

            HashSet<string> queries;
            if (_cachedQueries.TryGetValue(workspace, out queries))
            {
                lock (queries)
                {
                    return queries.Contains(sql);
                }
            }

            return false;
        }
    }
}

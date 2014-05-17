// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

using System;

namespace EFCache
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data.Entity.Core.Metadata.Edm;

    public sealed class BlacklistedQueriesRegistrar
    {
        public static readonly BlacklistedQueriesRegistrar Instance = new BlacklistedQueriesRegistrar();

        private readonly ConcurrentDictionary<MetadataWorkspace, HashSet<string>> _blacklistedQueries =
            new ConcurrentDictionary<MetadataWorkspace, HashSet<string>>();

        private BlacklistedQueriesRegistrar()
        {
        }

        public void AddBlacklistedQuery(MetadataWorkspace workspace, string sql)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException("workspace");
            }

            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentNullException("sql");
            }

            var queries = _blacklistedQueries.GetOrAdd(workspace, new HashSet<string>());
            lock (queries)
            {
                queries.Add(sql);
            }
        }

        public bool RemoveBlacklistedQuery(MetadataWorkspace workspace, string sql)
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
            if (_blacklistedQueries.TryGetValue(workspace, out queries))
            {
                lock (queries)
                {
                    return queries.Remove(sql);
                }
            }

            return false;
        }

        public bool IsQueryBlacklisted(MetadataWorkspace workspace, string sql)
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
            if (_blacklistedQueries.TryGetValue(workspace, out queries))
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

// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace EFCache
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data.Entity.Core.Metadata.Edm;

    internal class QueryRegistrar
    {
        private readonly ConcurrentDictionary<MetadataWorkspace, HashSet<string>> _queries =
            new ConcurrentDictionary<MetadataWorkspace, HashSet<string>>();

        public void AddQuery(MetadataWorkspace workspace, string sql)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException("workspace");
            }

            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentNullException("sql");
            }

            var queries = _queries.GetOrAdd(workspace, new HashSet<string>());
            lock (queries)
            {
                queries.Add(sql);
            }
        }

        public bool RemoveQuery(MetadataWorkspace workspace, string sql)
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
            if (_queries.TryGetValue(workspace, out queries))
            {
                lock (queries)
                {
                    return queries.Remove(sql);
                }
            }

            return false;
        }

        public bool ContainsQuery(MetadataWorkspace workspace, string sql)
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
            if (_queries.TryGetValue(workspace, out queries))
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

// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace EFCache
{
    using System.Data.Entity.Core.Metadata.Edm;

    public sealed class AlwaysCachedQueriesRegistrar
    {
        public static readonly AlwaysCachedQueriesRegistrar Instance = new AlwaysCachedQueriesRegistrar();

        private readonly QueryRegistrar _cachedQueries = new QueryRegistrar();

        private AlwaysCachedQueriesRegistrar()
        {
        }

        public void AddCachedQuery(MetadataWorkspace workspace, string sql)
        {
            _cachedQueries.AddQuery(workspace, sql);
        }

        public bool RemoveCachedQuery(MetadataWorkspace workspace, string sql)
        {
            return _cachedQueries.RemoveQuery(workspace, sql);
        }

        public bool IsQueryCached(MetadataWorkspace workspace, string sql)
        {
            return _cachedQueries.ContainsQuery(workspace, sql);
        }
    }
}

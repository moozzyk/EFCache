// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace EFCache
{
    using System.Data.Entity.Core.Metadata.Edm;

    public sealed class BlockedQueriesRegistrar
    {
        public static readonly BlockedQueriesRegistrar Instance = new BlockedQueriesRegistrar();

        private readonly QueryRegistrar _blockedQueries = new QueryRegistrar();

        private BlockedQueriesRegistrar()
        {
        }

        public void AddBlockedQuery(MetadataWorkspace workspace, string sql)
        {
            _blockedQueries.AddQuery(workspace, sql);
        }

        public bool RemoveBlockedQuery(MetadataWorkspace workspace, string sql)
        {
            return _blockedQueries.RemoveQuery(workspace, sql);
        }

        public bool IsQueryBlocked(MetadataWorkspace workspace, string sql)
        {
            return _blockedQueries.ContainsQuery(workspace, sql);
        }
    }
}

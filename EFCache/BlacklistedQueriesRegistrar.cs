// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace EFCache
{
    using System.Data.Entity.Core.Metadata.Edm;

    public sealed class BlacklistedQueriesRegistrar
    {
        public static readonly BlacklistedQueriesRegistrar Instance = new BlacklistedQueriesRegistrar();

        private ICacheQuery _blacklistedQueries => CacheConfiguration.Instance.CacheQuery;

        private BlacklistedQueriesRegistrar()
        {
        }

        public void AddBlacklistedQuery(MetadataWorkspace workspace, string sql)
        {
            _blacklistedQueries.AddQuery(workspace, sql);
        }

        public bool RemoveBlacklistedQuery(MetadataWorkspace workspace, string sql)
        {
            return _blacklistedQueries.RemoveQuery(workspace, sql);
        }

        public bool IsQueryBlacklisted(MetadataWorkspace workspace, string sql)
        {
            return _blacklistedQueries.ContainsQuery(workspace, sql);
        }
    }
}

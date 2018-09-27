// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace EFCache
{
    using System.Collections.ObjectModel;
    using System.Data.Common;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Core.Metadata.Edm;

    public class CachingCommandDefinition : DbCommandDefinition
    {
        protected readonly DbCommandDefinition _commandDefintion;
        protected readonly CommandTreeFacts _commandTreeFacts;
        protected readonly CacheTransactionHandler _cacheTransactionHandler;
        protected readonly CachingPolicy _cachingPolicy;

        public bool IsQuery
        {
            get { return _commandTreeFacts.IsQuery; }
        }

        public bool IsCacheable
        {
            get { return _commandTreeFacts.IsQuery && !_commandTreeFacts.UsesNonDeterministicFunctions; }
        }

        public ReadOnlyCollection<EntitySetBase> AffectedEntitySets
        {
            get { return _commandTreeFacts.AffectedEntitySets; }
        }

        public CachingCommandDefinition(DbCommandDefinition commandDefinition, CommandTreeFacts commandTreeFacts, CacheTransactionHandler cacheTransactionHandler, CachingPolicy cachingPolicy)
        {
            _commandDefintion = commandDefinition;
            _commandTreeFacts = commandTreeFacts;
            _cacheTransactionHandler = cacheTransactionHandler;
            _cachingPolicy = cachingPolicy;
        }

        public override DbCommand CreateCommand()
        {
            return new CachingCommand(_commandDefintion.CreateCommand(), _commandTreeFacts, _cacheTransactionHandler, _cachingPolicy);
        }
    }
}

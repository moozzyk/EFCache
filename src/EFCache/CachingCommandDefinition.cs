// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace EFCache
{
    using System.Collections.ObjectModel;
    using System.Data.Common;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Core.Metadata.Edm;

    internal class CachingCommandDefinition : DbCommandDefinition
    {
        private readonly DbCommandDefinition _commandDefintion;
        private readonly CommandTreeFacts _commandTreeFacts;
        private readonly CacheTransactionHandler _cacheTransactionHandler;
        private readonly CachingPolicy _cachingPolicy;
        private readonly CachingCommandStrategyFactory _cachingCommandStrategyFactory;

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

        public CachingCommandDefinition(DbCommandDefinition commandDefinition,
            CommandTreeFacts commandTreeFacts,
            CacheTransactionHandler cacheTransactionHandler,
            CachingPolicy cachingPolicy)
        {
            _commandDefintion = commandDefinition;
            _commandTreeFacts = commandTreeFacts;
            _cacheTransactionHandler = cacheTransactionHandler;
            _cachingPolicy = cachingPolicy;
            _cachingCommandStrategyFactory = DefaultCachingCommandFactory.Create;
        }

        public CachingCommandDefinition(DbCommandDefinition commandDefinition, 
            CommandTreeFacts commandTreeFacts, 
            CacheTransactionHandler cacheTransactionHandler, 
            CachingPolicy cachingPolicy,
            CachingCommandStrategyFactory cachingCommandStrategyFactory) : this(commandDefinition, commandTreeFacts, cacheTransactionHandler, cachingPolicy)
        {
            _cachingCommandStrategyFactory = cachingCommandStrategyFactory;
        }

        public override DbCommand CreateCommand()
        {
            return new CachingCommand(_commandDefintion.CreateCommand(), _commandTreeFacts, _cacheTransactionHandler, _cachingPolicy, _cachingCommandStrategyFactory);
        }
    }
}

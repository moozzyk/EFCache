// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace EFCache
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Data.Entity.Infrastructure.Interception;
    using System.Linq;

    public class CacheTransactionHandler : IDbTransactionInterceptor
    {
        private readonly ConcurrentDictionary<DbTransaction, List<string>> _affectedSetsInTransaction
            = new ConcurrentDictionary<DbTransaction, List<string>>();
        private readonly ICache _cache;
        private readonly ICacheProvider _cacheProvider;

        public CacheTransactionHandler(ICache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public CacheTransactionHandler(ICacheProvider cacheProvider)
        {
            _cacheProvider = cacheProvider ?? throw new ArgumentNullException(nameof(cacheProvider));
        }

        protected CacheTransactionHandler()
        {
        }

        protected virtual bool GetItem(DbTransaction transaction, string key, DbConnection connection, out object value)
        {
            if (transaction == null)
            {
                return ResolveCache(connection).GetItem(key, out value);
            }

            value = null;

            return false;
        }

        public virtual bool GetItem<TObject>(DbTransaction transaction, string key, DbConnection connection, out TObject value)
        {
            if (transaction != null)
            {
                value = default;
                return false;
            }

            try
            {
                return ResolveCacheProvider(connection).GetItem(key, out value);
            }
            catch (CacheProviderNotFoundException)
            {
                object objectValue;
                var result = GetItem(transaction, key, connection, out objectValue);
                value = (TObject)objectValue;
                return result;
            }
        }

        protected virtual void PutItem(DbTransaction transaction, string key, object value, IEnumerable<string> dependentEntitySets, TimeSpan slidingExpiration,
            DateTimeOffset absoluteExpiration, DbConnection connection)
        {
            if (transaction == null)
            {
                ResolveCache(connection).PutItem(key, value, dependentEntitySets, slidingExpiration, absoluteExpiration);
            }
        }

        public virtual void PutItem<TObject>(DbTransaction transaction, string key, TObject value, IEnumerable<string> dependentEntitySets, TimeSpan slidingExpiration,
            DateTimeOffset absoluteExpiration, DbConnection connection)
        {
            if (transaction != null) return;
            try
            {
                ResolveCacheProvider(connection).PutItem(key, value, dependentEntitySets, slidingExpiration, absoluteExpiration);
            }
            catch (CacheProviderNotFoundException)
            {
                PutItem(transaction, key, (object)value, dependentEntitySets, slidingExpiration, absoluteExpiration, connection);
            }
        }

        public virtual void InvalidateSets(DbTransaction transaction, IEnumerable<string> entitySets, DbConnection connection)
        {
            if (transaction == null)
            {
                ResolveCache(connection).InvalidateSets(entitySets);
            }
            else
            {
                AddAffectedEntitySets(transaction, entitySets);
            }
        }

        protected void AddAffectedEntitySets(DbTransaction transaction, IEnumerable<string> affectedEntitySets)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            if (affectedEntitySets == null)
            {
                throw new ArgumentNullException(nameof(affectedEntitySets));
            }

            var entitySets = _affectedSetsInTransaction.GetOrAdd(transaction, new List<string>());
            entitySets.AddRange(affectedEntitySets);
        }

        private IEnumerable<string> RemoveAffectedEntitySets(DbTransaction transaction)
        {

            _affectedSetsInTransaction.TryRemove(transaction, out List<string> affectedEntitySets);

            return affectedEntitySets;
        }

        public void Committed(DbTransaction transaction, DbTransactionInterceptionContext interceptionContext)
        {
            var entitySets = RemoveAffectedEntitySets(transaction);

            if (entitySets != null)
            {
                ResolveCache(interceptionContext.Connection).InvalidateSets(entitySets.Distinct());
            }
        }

        public void Committing(DbTransaction transaction, DbTransactionInterceptionContext interceptionContext)
        {
        }

        public void ConnectionGetting(DbTransaction transaction, DbTransactionInterceptionContext<DbConnection> interceptionContext)
        {
        }

        public void ConnectionGot(DbTransaction transaction, DbTransactionInterceptionContext<DbConnection> interceptionContext)
        {
        }

        public void Disposed(DbTransaction transaction, DbTransactionInterceptionContext interceptionContext)
        {
        }

        public void Disposing(DbTransaction transaction, DbTransactionInterceptionContext interceptionContext)
        {
        }

        public void IsolationLevelGetting(DbTransaction transaction, DbTransactionInterceptionContext<IsolationLevel> interceptionContext)
        {
        }

        public void IsolationLevelGot(DbTransaction transaction, DbTransactionInterceptionContext<IsolationLevel> interceptionContext)
        {
        }

        public void RolledBack(DbTransaction transaction, DbTransactionInterceptionContext interceptionContext)
        {
            RemoveAffectedEntitySets(transaction);
        }

        public void RollingBack(DbTransaction transaction, DbTransactionInterceptionContext interceptionContext)
        {
        }

        protected virtual ICache ResolveCache(DbConnection connection)
        {
            if (_cache != null) return _cache;
            var cacheProvider = ResolveCacheProvider(connection);
            if (cacheProvider != null) return cacheProvider;
            throw new InvalidOperationException("Cannot resolve cache because it has not been initialized.");
        }

        protected virtual ICacheProvider ResolveCacheProvider(DbConnection connection)
        {
            return _cacheProvider ??
                   throw new CacheProviderNotFoundException("Cannot resolve cache because it has not been initialized.");
        }
    }

    public class CacheProviderNotFoundException : InvalidOperationException
    {
        public CacheProviderNotFoundException(string message) : base(message) { }
    }

}

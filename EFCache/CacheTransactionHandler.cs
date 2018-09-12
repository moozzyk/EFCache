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
        private readonly ConcurrentDictionary<DbTransaction, HashSet<string>> _affectedSetsInTransaction
            = new ConcurrentDictionary<DbTransaction, HashSet<string>>();
		private readonly ConcurrentDictionary<DbTransaction, List<ILockedEntitySet>> _locksInTransaction =
			new ConcurrentDictionary<DbTransaction, List<ILockedEntitySet>>();
		private readonly ICache _cache;


		public CacheTransactionHandler(ICache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        protected CacheTransactionHandler()
        {
        }

        public virtual bool GetItem(DbTransaction transaction, string key, DbConnection connection, out object value)
        {
            if (transaction == null)
            {
                return ResolveCache(connection).GetItem(key, out value);
            }

            value = null;

            return false;
        }

        public virtual void PutItem(DbTransaction transaction, string key, object value, IEnumerable<string> dependentEntitySets, TimeSpan slidingExpiration,
            DateTimeOffset absoluteExpiration, DbConnection connection)
        {
            if (transaction == null)
            {
                ResolveCache(connection).PutItem(key, value, dependentEntitySets, slidingExpiration, absoluteExpiration);
            }
        }

        public virtual void InvalidateSets(DbTransaction transaction, IEnumerable<string> entitySets, DbConnection connection)
        {
			var cache = ResolveCache(connection);
            if (transaction == null)
			{
				var sets = entitySets as string[] ?? entitySets.ToArray();
				var lockedEntitySets = Lock(sets, connection);
                cache.InvalidateSets(sets);
				ReleaseLock(lockedEntitySets, connection);
            }
            else
            {
                AddAffectedEntitySets(transaction, entitySets, connection);
            }
        }

        public List<ILockedEntitySet> Lock(IEnumerable<string> entitySets, DbConnection connection)
        {
            if (!(ResolveCache(connection) is ILockableCache lockableCache)) return null;
			return lockableCache.Lock(entitySets);
		}

        public void ReleaseLock(IEnumerable<ILockedEntitySet> lockedEntitySets, DbConnection connection)
        {
            if (!(ResolveCache(connection) is ILockableCache lockableCache)) return;
            lockableCache.ReleaseLock(lockedEntitySets);
        }

        protected void AddAffectedEntitySets(DbTransaction transaction, IEnumerable<string> affectedEntitySets, DbConnection connection)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException("transaction");
            }

            if (affectedEntitySets == null)
            {
                throw new ArgumentNullException("affectedEntitySets");
            }

            var entitySets = _affectedSetsInTransaction.GetOrAdd(transaction, new HashSet<string>());
			if (!(ResolveCache(connection) is ILockableCache)) return;
			var locks = _locksInTransaction.GetOrAdd(transaction, new List<ILockedEntitySet>());
			foreach (var affectedEntitySet in affectedEntitySets)
			{
				if (entitySets.Add(affectedEntitySet))
					locks.AddRange(Lock(new List<string>{affectedEntitySet}, connection));

			}
		}

        private IEnumerable<string> RemoveAffectedEntitySets(DbTransaction transaction)
        {
            _affectedSetsInTransaction.TryRemove(transaction, out var affectedEntitySets);

            return affectedEntitySets;
        }

		private IEnumerable<ILockedEntitySet> RemoveAffectedLocks(DbTransaction transaction)
		{
			_locksInTransaction.TryRemove(transaction, out var locks);

			return locks;
		}
		
		public void Committed(DbTransaction transaction, DbTransactionInterceptionContext interceptionContext)
        {
            var entitySets = RemoveAffectedEntitySets(transaction);
            if (entitySets != null)
            {
                ResolveCache(interceptionContext.Connection).InvalidateSets(entitySets.Distinct());
            }

			if (!(ResolveCache(interceptionContext.Connection) is ILockableCache)) return;
			var lockedEntitySets = RemoveAffectedLocks(transaction);
			if (lockedEntitySets != null)
			{
				ReleaseLock(lockedEntitySets, interceptionContext.Connection);
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
            => _cache ?? throw new InvalidOperationException("Cannot resolve cache because it has not been initialized.");
    }
}
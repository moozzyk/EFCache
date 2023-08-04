using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EFCache
{
    public delegate ICachingCommandStrategy CachingCommandStrategyFactory(CachingPolicy cachingPolicy,
        ICommandTreeFacts commandTreeFacts,
        CacheTransactionHandler cacheTransactionHandler,
        ICachingCommandMetadata commandMetadata);

    public static class DefaultCachingCommandFactory
    {
        public static ICachingCommandStrategy Create(CachingPolicy cachingPolicy,
            ICommandTreeFacts commandTreeFacts,
            CacheTransactionHandler cacheTransactionHandler,
            ICachingCommandMetadata commandMetadata)
        {
            return new CachingCommandStrategy(cachingPolicy, commandTreeFacts, cacheTransactionHandler, commandMetadata);
        }
    }

    public class CachingCommandStrategy : ICachingCommandStrategy
    {
        protected readonly CachingPolicy _cachingPolicy;
        protected readonly ICommandTreeFacts _commandTreeFacts;
        protected readonly CacheTransactionHandler _cacheTransactionHandler;
        protected readonly ICachingCommandMetadata _command;

        public CachingCommandStrategy(CachingPolicy cachingPolicy,
            ICommandTreeFacts commandTreeFacts,
            CacheTransactionHandler cacheTransactionHandler,
            ICachingCommandMetadata commandMetadata)
        {
            _cachingPolicy = cachingPolicy;
            _commandTreeFacts = commandTreeFacts;
            _cacheTransactionHandler = cacheTransactionHandler;
            _command = commandMetadata;
        }

        protected virtual bool IsQueryAlwaysCached()
        {
            return AlwaysCachedQueriesRegistrar.Instance.IsQueryCached(
                _commandTreeFacts.MetadataWorkspace, _command.CommandText);
        }

        protected virtual bool IsQueryBlocked()
        {
            return BlockedQueriesRegistrar.Instance.IsQueryBlocked(
                _commandTreeFacts.MetadataWorkspace, _command.CommandText);
        }

        public virtual bool IsCacheable()
        {
            return _commandTreeFacts.IsQuery &&
                   (IsQueryAlwaysCached() ||
                    !_commandTreeFacts.UsesNonDeterministicFunctions &&
                    !IsQueryBlocked() &&
                    _cachingPolicy.CanBeCached(_commandTreeFacts.AffectedEntitySets, _command.CommandText,
                        _command.Parameters.Cast<DbParameter>()
                            .Select(p => new KeyValuePair<string, object>(p.ParameterName, p.Value))));
        }

        public virtual string CreateKey()
        {
            return
                string.Format(
                    "{0}_{1}_{2}",
                    _command.Connection.Database,
                    _command.CommandText,
                    string.Join(
                        "_",
                        _command.Parameters.Cast<DbParameter>()
                            .Select(p => string.Format("{0}={1}", p.ParameterName, p.Value))));
        }

        public virtual bool GetCachedDbDataReader(string key, out DbDataReader dbDataReader)
        {
            CachedResults value;
            if (_cacheTransactionHandler.GetItem(_command.Transaction, key, _command.Connection, out value))
            {
                dbDataReader = CreateDataReaderFromCachedResults(value);
                return true;
            }
            dbDataReader = null;
            return false;
        }

        public virtual DbDataReader SetCacheFromDbDataReader(string key, DbDataReader reader)
        {
            var queryResults = new List<object[]>();

            while (reader.Read())
            {
                var values = new object[reader.FieldCount];
                reader.GetValues(values);
                queryResults.Add(values);
            }

            var cachedResults =
                new CachedResults(
                    GetTableMetadata(reader), queryResults, reader.RecordsAffected);

            var cachedReader = CreateDataReaderFromCachedResults(cachedResults);

            if (!ShouldSetCache(queryResults.Count)) return cachedReader;

            SetCache(key, cachedResults);

            return cachedReader;
        }

#if !NET40
        public virtual async Task<DbDataReader> SetCacheFromDbDataReaderAsync(string key, DbDataReader reader, CancellationToken cancellationToken)
        {
            var queryResults = new List<object[]>();

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var values = new object[reader.FieldCount];
                reader.GetValues(values);
                queryResults.Add(values);
            }

            var cachedResults =
                new CachedResults(
                    GetTableMetadata(reader), queryResults, reader.RecordsAffected);

            var cachedReader = CreateDataReaderFromCachedResults(cachedResults);

            if (!ShouldSetCache(queryResults.Count)) return cachedReader;
            
            SetCache(key, cachedResults);
            
            return cachedReader;
        }
#endif

        protected virtual DbDataReader CreateDataReaderFromCachedResults(CachedResults cachedResults)
        {
            return new CachingReader(cachedResults);
        }

        protected virtual bool ShouldSetCache(int queryResultCount)
        {
            int minCacheableRows, maxCachableRows;
            _cachingPolicy.GetCacheableRows(_commandTreeFacts.AffectedEntitySets, out minCacheableRows,
                out maxCachableRows);

            return IsQueryAlwaysCached() ||
                   (queryResultCount >= minCacheableRows && queryResultCount <= maxCachableRows);
        }


        protected virtual void SetCache<TObject>(string key, TObject cachedResults)
        {
            TimeSpan slidingExpiration;
            DateTimeOffset absoluteExpiration;
            _cachingPolicy.GetExpirationTimeout(_commandTreeFacts.AffectedEntitySets, out slidingExpiration,
                out absoluteExpiration);

            _cacheTransactionHandler.PutItem(
                _command.Transaction,
                key,
                cachedResults,
                _commandTreeFacts.AffectedEntitySets.Select(s => s.Name),
                slidingExpiration,
                absoluteExpiration,
                _command.Connection);
        }

        private ColumnMetadata[] GetTableMetadata(DbDataReader reader)
        {
            var columnMetadata = new ColumnMetadata[reader.FieldCount];

            for (var i = 0; i < reader.FieldCount; i++)
            {
                columnMetadata[i] =
                    new ColumnMetadata(
                        reader.GetName(i), reader.GetDataTypeName(i), reader.GetFieldType(i));
            }

            return columnMetadata;
        }

        public virtual bool GetCachedScalarObject(string key, out object cachedObject)
        {
            return _cacheTransactionHandler.GetItem(_command.Transaction, key, _command.Connection, out cachedObject);
        }

        public virtual void SetCacheFromScalarObject(string key, object value)
        {
            SetCache(key, value);
        }

        public virtual void InvalidateSets(int recordsAffected)
        {
            if (recordsAffected > 0 && _commandTreeFacts.AffectedEntitySets.Any())
            {
                _cacheTransactionHandler.InvalidateSets(_command.Transaction, 
                    _commandTreeFacts.AffectedEntitySets.Select(s => s.Name),
                    _command.Connection);
            }
        }
    }
}

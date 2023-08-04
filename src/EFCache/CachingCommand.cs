// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace EFCache
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    internal class CachingCommand : DbCommand, ICloneable
    {
        private readonly DbCommand _command;
        private readonly CommandTreeFacts _commandTreeFacts;
        private readonly CacheTransactionHandler _cacheTransactionHandler;
        private readonly CachingPolicy _cachingPolicy;

        public CachingCommand(DbCommand command, CommandTreeFacts commandTreeFacts, CacheTransactionHandler cacheTransactionHandler, CachingPolicy cachingPolicy)
        {
            Debug.Assert(command != null, "command is null");
            Debug.Assert(commandTreeFacts != null, "commandTreeFacts is null");
            Debug.Assert(cacheTransactionHandler != null, "cacheTransactionHandler is null");
            Debug.Assert(cachingPolicy != null, "cachingPolicy is null");

            _command = command;
            _commandTreeFacts = commandTreeFacts;
            _cacheTransactionHandler = cacheTransactionHandler;
            _cachingPolicy = cachingPolicy;
        }

        internal CommandTreeFacts CommandTreeFacts
        {
            get { return _commandTreeFacts; }
        }

        internal CacheTransactionHandler CacheTransactionHandler
        {
            get { return _cacheTransactionHandler; }
        }

        internal CachingPolicy CachingPolicy
        {
            get { return _cachingPolicy; }
        }

        internal DbCommand WrappedCommand
        {
            get { return _command; }
        }

        private bool IsCacheable
        {
            get
            {
                return _commandTreeFacts.IsQuery &&
                       (IsQueryAlwaysCached ||
                       !_commandTreeFacts.UsesNonDeterministicFunctions &&
                       !IsQueryBlocked &&
                       _cachingPolicy.CanBeCached(_commandTreeFacts.AffectedEntitySets, CommandText,
                           Parameters.Cast<DbParameter>()
                               .Select(p => new KeyValuePair<string, object>(p.ParameterName, p.Value))));
            }
        }

        private bool IsQueryBlocked
        {
            get
            {
                return BlockedQueriesRegistrar.Instance.IsQueryBlocked(
                    _commandTreeFacts.MetadataWorkspace, CommandText);
            }
        }

        private bool IsQueryAlwaysCached
        {
            get
            {
                return AlwaysCachedQueriesRegistrar.Instance.IsQueryCached(
                    _commandTreeFacts.MetadataWorkspace, CommandText);
            }
        }

        public override void Cancel()
        {
            _command.Cancel();
        }

        public override string CommandText
        {
            get
            {
                return _command.CommandText;
            }
            set
            {
                _command.CommandText = value;
            }
        }

        public override int CommandTimeout
        {
            get
            {
                return _command.CommandTimeout;
            }
            set
            {
                _command.CommandTimeout = value;
            }
        }

        public override CommandType CommandType
        {
            get
            {
                return _command.CommandType;
            }
            set
            {
                _command.CommandType = value;
            }
        }

        protected override DbParameter CreateDbParameter()
        {
            return _command.CreateParameter();
        }

        protected override DbConnection DbConnection
        {
            get
            {
                return _command.Connection;
            }
            set
            {
                _command.Connection = value;
            }
        }

        protected override DbParameterCollection DbParameterCollection
        {
            get { return _command.Parameters; }
        }

        protected override DbTransaction DbTransaction
        {
            get
            {
                return _command.Transaction;
            }
            set
            {
                _command.Transaction = value;
            }
        }

        public override bool DesignTimeVisible
        {
            get
            {
                return _command.DesignTimeVisible;
            }
            set
            {
                _command.DesignTimeVisible = value;
            }
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            if (!IsCacheable)
            {
                var result = _command.ExecuteReader(behavior);

                if (!_commandTreeFacts.IsQuery)
                {
                    _cacheTransactionHandler.InvalidateSets(Transaction, _commandTreeFacts.AffectedEntitySets.Select(s => s.Name),
                        DbConnection);
                }

                return result;
            }

            var key = CreateKey();

            CachedResults value;
            if (_cacheTransactionHandler.GetItem(Transaction, key, DbConnection, out value))
            {
                return new CachingReader(value);
            }

            using (var reader = _command.ExecuteReader(behavior))
            {
                var queryResults = new List<object[]>();

                while (reader.Read())
                {
                    var values = new object[reader.FieldCount];
                    reader.GetValues(values);
                    queryResults.Add(values);
                }

                return HandleCaching(reader, key, queryResults);
            }
        }

#if !NET40
        protected async override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        {
            if (!IsCacheable)
            {
                var result = await _command.ExecuteReaderAsync(behavior, cancellationToken).ConfigureAwait(false);

                if (!_commandTreeFacts.IsQuery)
                {
                    _cacheTransactionHandler.InvalidateSets(Transaction, _commandTreeFacts.AffectedEntitySets.Select(s => s.Name), DbConnection);
                }

                return result;
            }

            var key = CreateKey();

            CachedResults value;
            if (_cacheTransactionHandler.GetItem(Transaction, key, DbConnection, out value))
            {
                return new CachingReader(value);
            }

            using (var reader = await _command.ExecuteReaderAsync(behavior, cancellationToken).ConfigureAwait(false))
            {
                var queryResults = new List<object[]>();

                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    var values = new object[reader.FieldCount];
                    reader.GetValues(values);
                    queryResults.Add(values);
                }

                return HandleCaching(reader, key, queryResults);
            }
        }
#endif

        private DbDataReader HandleCaching(DbDataReader reader, string key, List<object[]> queryResults)
        {
            var cachedResults =
                new CachedResults(
                    GetTableMetadata(reader), queryResults, reader.RecordsAffected);

            int minCacheableRows, maxCachableRows;
            _cachingPolicy.GetCacheableRows(_commandTreeFacts.AffectedEntitySets, out minCacheableRows,
                out maxCachableRows);

            if (IsQueryAlwaysCached || (queryResults.Count >= minCacheableRows && queryResults.Count <= maxCachableRows))
            {
                TimeSpan slidingExpiration;
                DateTimeOffset absoluteExpiration;
                _cachingPolicy.GetExpirationTimeout(_commandTreeFacts.AffectedEntitySets, out slidingExpiration,
                    out absoluteExpiration);

                _cacheTransactionHandler.PutItem(
                    Transaction,
                    key,
                    cachedResults,
                    _commandTreeFacts.AffectedEntitySets.Select(s => s.Name),
                    slidingExpiration,
                    absoluteExpiration,
                    DbConnection);
            }

            return new CachingReader(cachedResults);
        }

        protected override void Dispose(bool disposing)
        {
            _command?.GetType()
                .GetMethod("Dispose", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_command, new object[] { disposing });
        }

        private static ColumnMetadata[] GetTableMetadata(DbDataReader reader)
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

        public override int ExecuteNonQuery()
        {
            var recordsAffected = _command.ExecuteNonQuery();

            InvalidateSetsForNonQuery(recordsAffected);

            return recordsAffected;
        }

#if !NET40
        public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        {
            var recordsAffected = await _command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            InvalidateSetsForNonQuery(recordsAffected);

            return recordsAffected;
        }
#endif

        private void InvalidateSetsForNonQuery(int recordsAffected)
        {
            if (recordsAffected > 0 && _commandTreeFacts.AffectedEntitySets.Any())
            {
                _cacheTransactionHandler.InvalidateSets(Transaction, _commandTreeFacts.AffectedEntitySets.Select(s => s.Name),
                    DbConnection);
            }
        }

        public override object ExecuteScalar()
        {
            if (!IsCacheable)
            {
                return _command.ExecuteScalar();
            }

            var key = CreateKey();

            object value;

            if (_cacheTransactionHandler.GetItem(Transaction, key, DbConnection, out value))
            {
                return value;
            }

            value = _command.ExecuteScalar();

            TimeSpan slidingExpiration;
            DateTimeOffset absoluteExpiration;
            _cachingPolicy.GetExpirationTimeout(_commandTreeFacts.AffectedEntitySets, out slidingExpiration, out absoluteExpiration);

            _cacheTransactionHandler.PutItem(
                Transaction,
                key,
                value,
                _commandTreeFacts.AffectedEntitySets.Select(s => s.Name),
                slidingExpiration,
                absoluteExpiration,
                DbConnection);

            return value;
        }

#if !NET40
        public async override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            if (!IsCacheable)
            {
                return await _command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            }

            var key = CreateKey();

            object value;

            if (_cacheTransactionHandler.GetItem(Transaction, key, DbConnection, out value))
            {
                return value;
            }

            value = await _command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

            TimeSpan slidingExpiration;
            DateTimeOffset absoluteExpiration;
            _cachingPolicy.GetExpirationTimeout(_commandTreeFacts.AffectedEntitySets, out slidingExpiration, out absoluteExpiration);

            _cacheTransactionHandler.PutItem(
                Transaction,
                key,
                value,
                _commandTreeFacts.AffectedEntitySets.Select(s => s.Name),
                slidingExpiration,
                absoluteExpiration,
                DbConnection);

            return value;
        }
#endif

        public override void Prepare()
        {
            _command.Prepare();
        }

        public override UpdateRowSource UpdatedRowSource
        {
            get
            {
                return _command.UpdatedRowSource;
            }
            set
            {
                _command.UpdatedRowSource = value;
            }
        }

        private string CreateKey()
        {
            return
                string.Format(
                "{0}_{1}_{2}",
                Connection.Database,
                CommandText,
                string.Join(
                    "_",
                    Parameters.Cast<DbParameter>()
                    .Select(p => string.Format("{0}={1}", p.ParameterName, p.Value))));
        }

        public object Clone()
        {
            var cloneableCommand = _command as ICloneable;
            if (cloneableCommand == null)
            {
                throw new InvalidOperationException("The underlying DbCommand does not implement the ICloneable interface.");
            }

            var clonedCommand = (DbCommand)cloneableCommand.Clone();
            return new CachingCommand(clonedCommand, _commandTreeFacts, _cacheTransactionHandler, _cachingPolicy);
        }
    }
}

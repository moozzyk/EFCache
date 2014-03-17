
namespace EFCache
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    
    internal class CachingCommand : DbCommand
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
                    !_commandTreeFacts.UsesNonDeterministicFunctions && 
                    _cachingPolicy.CanBeCached(_commandTreeFacts.AffectedEntitySets);
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
                    _cacheTransactionHandler.InvalidateSets(Transaction, _commandTreeFacts.AffectedEntitySets.Select(s => s.Name));
                }

                return result;
            }

            var key = CreateKey();

            object value;
            if (_cacheTransactionHandler.GetItem(Transaction, key, out value))
            {
                return new CachingReader((CachedResults)value);
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

                var cachedResults = 
                    new CachedResults(
                        GetTableMetadata(reader), queryResults, reader.RecordsAffected);

                int minCacheableRows, maxCachableRows;
                _cachingPolicy.GetCacheableRows(_commandTreeFacts.AffectedEntitySets, out minCacheableRows, out maxCachableRows);

                if (queryResults.Count >= minCacheableRows && queryResults.Count <= maxCachableRows)
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
                        absoluteExpiration);
                }

                return new CachingReader(cachedResults);
            }
        }

        protected override void Dispose(bool disposing)
        {
            // TODO: feels wrong
            _command.GetType()
                .GetMethod("Dispose", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_command, new object[] { true });
        }

        private static ColumnMetadata[] GetTableMetadata(DbDataReader reader)
        {
            var columnMetadata = new ColumnMetadata[reader.FieldCount];

            for (int i = 0; i < reader.FieldCount; i++)
            {
                columnMetadata[i] = 
                    new ColumnMetadata(
                        reader.GetName(0), reader.GetDataTypeName(0), reader.GetFieldType(0));
            }

            return columnMetadata;
        }

        public override int ExecuteNonQuery()
        {
            var recordsAffected = _command.ExecuteNonQuery();

            if (recordsAffected > 0 && _commandTreeFacts.AffectedEntitySets.Any())
            {
                _cacheTransactionHandler.InvalidateSets(Transaction, _commandTreeFacts.AffectedEntitySets.Select(s => s.Name));
            }

            return recordsAffected;
        }

        public override object ExecuteScalar()
        {
            if (!IsCacheable)
            {
                return _command.ExecuteScalar();
            }

            var key = CreateKey();

            object value;

            if(_cacheTransactionHandler.GetItem(Transaction, key, out value))
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
                absoluteExpiration);

            return value;
        }

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

        // TODO: add other overrides

        private string CreateKey()
        {
            return 
                string.Format(
                "{0}_{1}", 
                CommandText, 
                string.Join(
                    "_", 
                    Parameters.Cast<DbParameter>()
                    .Select(p => string.Format("{0}={1}", p.ParameterName, p.Value))));
        }
    }
}

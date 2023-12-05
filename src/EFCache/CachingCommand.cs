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

    internal class CachingCommand : DbCommand, ICloneable, ICachingCommandMetadata
    {
        private readonly DbCommand _command;
        private readonly CommandTreeFacts _commandTreeFacts;
        private readonly CacheTransactionHandler _cacheTransactionHandler;
        private readonly CachingPolicy _cachingPolicy;
        private readonly CachingCommandStrategyFactory _cachingCommandStrategyFactory;
        private readonly ICachingCommandStrategy _cachingCommandStrategy;

        public CachingCommand(DbCommand command, 
            CommandTreeFacts commandTreeFacts, 
            CacheTransactionHandler cacheTransactionHandler,
            CachingPolicy cachingPolicy)
        {
            Debug.Assert(command != null, "command is null");
            Debug.Assert(commandTreeFacts != null, "commandTreeFacts is null");
            Debug.Assert(cacheTransactionHandler != null, "cacheTransactionHandler is null");
            Debug.Assert(cachingPolicy != null, "cachingPolicy is null");

            _command = command;
            _commandTreeFacts = commandTreeFacts;
            _cacheTransactionHandler = cacheTransactionHandler;
            _cachingPolicy = cachingPolicy;
            _cachingCommandStrategyFactory = DefaultCachingCommandFactory.Create;
            _cachingCommandStrategy = _cachingCommandStrategyFactory(_cachingPolicy,
                _commandTreeFacts,
                _cacheTransactionHandler,
                this);
        }

        public CachingCommand(DbCommand command,
            CommandTreeFacts commandTreeFacts,
            CacheTransactionHandler cacheTransactionHandler,
            CachingPolicy cachingPolicy,
            CachingCommandStrategyFactory cachingCommandStrategyFactory) : this(command, commandTreeFacts, cacheTransactionHandler, cachingPolicy)
        {
            _cachingCommandStrategyFactory = cachingCommandStrategyFactory;
            _cachingCommandStrategy = _cachingCommandStrategyFactory(_cachingPolicy,
                _commandTreeFacts,
                _cacheTransactionHandler,
                this);
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
            if (!_cachingCommandStrategy.IsCacheable())
            {
                var result = _command.ExecuteReader(behavior);
                if (!_commandTreeFacts.IsQuery)
                {
                    _cachingCommandStrategy.InvalidateSets(result.RecordsAffected);
                }

                return result;
            }
            
            var key = _cachingCommandStrategy.CreateKey();

            if (_cachingCommandStrategy.GetCachedDbDataReader(key, out DbDataReader cachedReader))
            {
                return cachedReader;
            }
            
            using (var reader = _command.ExecuteReader(behavior))
            {
                return _cachingCommandStrategy.SetCacheFromDbDataReader(key, reader);
            }
        }

#if !NET40
        protected async override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        {
            if (!_cachingCommandStrategy.IsCacheable())
            {
                var result = await _command.ExecuteReaderAsync(behavior, cancellationToken).ConfigureAwait(false);

                if (!_commandTreeFacts.IsQuery)
                {
                    _cachingCommandStrategy.InvalidateSets(result.RecordsAffected);
                }

                return result;
            }

            var key = _cachingCommandStrategy.CreateKey();
            
            if (_cachingCommandStrategy.GetCachedDbDataReader(key, out DbDataReader cachedReader))
            {
                return cachedReader;
            }

            using (var reader = await _command.ExecuteReaderAsync(behavior, cancellationToken).ConfigureAwait(false))
            {
                return await _cachingCommandStrategy.SetCacheFromDbDataReaderAsync(key, reader, cancellationToken);
            }
        }
#endif

        protected override void Dispose(bool disposing)
        {
            _command?.GetType()
                .GetMethod("Dispose", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_command, new object[] { disposing });
        }

        public override int ExecuteNonQuery()
        {
            var recordsAffected = _command.ExecuteNonQuery();

            _cachingCommandStrategy.InvalidateSets(recordsAffected);

            return recordsAffected;
        }

#if !NET40
        public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        {
            var recordsAffected = await _command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            _cachingCommandStrategy.InvalidateSets(recordsAffected);

            return recordsAffected;
        }
#endif

        public override object ExecuteScalar()
        {
            if (!_cachingCommandStrategy.IsCacheable())
            {
                return _command.ExecuteScalar();
            }

            var key = _cachingCommandStrategy.CreateKey();

            if (_cachingCommandStrategy.GetCachedScalarObject(key, out object cachedObject))
            {
                return cachedObject;
            }
            
            var value = _command.ExecuteScalar();

            _cachingCommandStrategy.SetCacheFromScalarObject(key, value);
            
            return value;
        }

#if !NET40
        public async override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            if (!_cachingCommandStrategy.IsCacheable())
            {
                return await _command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            }

            var key = _cachingCommandStrategy.CreateKey();

            if (_cachingCommandStrategy.GetCachedScalarObject(key, out object cachedObject))
            {
                return cachedObject;
            }

            var value = await _command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

            _cachingCommandStrategy.SetCacheFromScalarObject(key, value);

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

        public object Clone()
        {
            var cloneableCommand = _command as ICloneable;
            if (cloneableCommand == null)
            {
                throw new InvalidOperationException("The underlying DbCommand does not implement the ICloneable interface.");
            }

            var clonedCommand = (DbCommand)cloneableCommand.Clone();
            return new CachingCommand(clonedCommand, _commandTreeFacts, _cacheTransactionHandler, _cachingPolicy, _cachingCommandStrategyFactory);
        }
    }
}

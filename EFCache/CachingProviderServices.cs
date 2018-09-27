﻿// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace EFCache
{
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Core.Common.CommandTrees;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Spatial;

    public class CachingProviderServices : DbProviderServices
    {
        protected readonly DbProviderServices _providerServices;
        protected readonly CacheTransactionHandler _cacheTransactionHandler;
        protected readonly CachingPolicy _cachingPolicy;

        public CachingProviderServices(DbProviderServices providerServices, CacheTransactionHandler cacheTransactionHandler, CachingPolicy cachingPolicy = null)
        {
            _providerServices = providerServices;
            _cacheTransactionHandler = cacheTransactionHandler;
            _cachingPolicy = cachingPolicy ?? new CachingPolicy();
        }

        protected override DbCommandDefinition CreateDbCommandDefinition(DbProviderManifest providerManifest, DbCommandTree commandTree)
        {
            return new CachingCommandDefinition(
                _providerServices.CreateCommandDefinition(providerManifest, commandTree), 
                new CommandTreeFacts(commandTree),
                _cacheTransactionHandler,
                _cachingPolicy);
        }

        protected override DbProviderManifest GetDbProviderManifest(string manifestToken)
        {
            return _providerServices.GetProviderManifest(manifestToken);
        }

        protected override string GetDbProviderManifestToken(DbConnection connection)
        {
            return _providerServices.GetProviderManifestToken(connection);
        }

        protected override void DbCreateDatabase(DbConnection connection, int? commandTimeout, StoreItemCollection storeItemCollection)
        {
            _providerServices.CreateDatabase(connection, commandTimeout, storeItemCollection);
        }

        protected override string DbCreateDatabaseScript(string providerManifestToken, StoreItemCollection storeItemCollection)
        {
            return _providerServices.CreateDatabaseScript(providerManifestToken, storeItemCollection);
        }

        protected override bool DbDatabaseExists(DbConnection connection, int? commandTimeout, StoreItemCollection storeItemCollection)
        {
            return _providerServices.DatabaseExists(connection, commandTimeout, storeItemCollection);
        }

        protected override void DbDeleteDatabase(DbConnection connection, int? commandTimeout, StoreItemCollection storeItemCollection)
        {
            _providerServices.DeleteDatabase(connection, commandTimeout, storeItemCollection);
        }

        protected override void SetDbParameterValue(DbParameter parameter, TypeUsage parameterType, object value)
        {
            _providerServices.SetParameterValue(parameter, parameterType, value);
        }

        protected override DbSpatialDataReader GetDbSpatialDataReader(DbDataReader fromReader, string manifestToken)
        {
            return _providerServices.GetSpatialDataReader(fromReader, manifestToken);
        }

#pragma warning disable 618, 672
        protected override DbSpatialServices DbGetSpatialServices(string manifestToken)
        {
            return _providerServices.GetSpatialServices(manifestToken);
        }
#pragma warning restore 618, 672

        public override object GetService(Type type, object key)
        {
            return _providerServices.GetService(type, key);
        }

        public override IEnumerable<object> GetServices(Type type, object key)
        {
            return _providerServices.GetServices(type, key);
        }

        public override void RegisterInfoMessageHandler(DbConnection connection, Action<string> handler)
        {
            _providerServices.RegisterInfoMessageHandler(connection, handler);
        }

        public override DbCommandDefinition CreateCommandDefinition(DbCommand prototype)
        {
            var cachingCommand = prototype as CachingCommand;

            var commandDefinition =
                _providerServices.CreateCommandDefinition(
                    cachingCommand != null
                        ? cachingCommand.WrappedCommand
                        : prototype);

            return cachingCommand != null
                ? new CachingCommandDefinition(commandDefinition, cachingCommand.CommandTreeFacts,
                    cachingCommand.CacheTransactionHandler, cachingCommand.CachingPolicy)
                : commandDefinition; 
        }
    }
}

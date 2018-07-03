// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace EFCache
{
    using Moq;
    using Moq.Protected;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Data;
    using System.Data.Common;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class CachingCommandTests
    {
        [Fact]
        public void CachingCommand_initialized_correctly()
        {
            var command = Mock.Of<DbCommand>();
            var commandTreeFacts = new CommandTreeFacts(null, true, true);
            var transactionHandler = new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object;
            var cachingPolicy = Mock.Of<CachingPolicy>();

            var cachingCommand = new CachingCommand(command, commandTreeFacts, transactionHandler, cachingPolicy);

            Assert.Same(command, cachingCommand.WrappedCommand);
            Assert.Same(commandTreeFacts, cachingCommand.CommandTreeFacts);
            Assert.Same(transactionHandler, cachingCommand.CacheTransactionHandler);
            Assert.Same(cachingPolicy, cachingCommand.CachingPolicy);
        }

        [Fact]
        public void Cancel_invokes_Cancel_on_wrapped_command()
        {
            var mockCommand = new Mock<DbCommand>();

            new CachingCommand(
                mockCommand.Object,
                new CommandTreeFacts(null, true, true),
                new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object,
                Mock.Of<CachingPolicy>()).Cancel();

            mockCommand.Verify(c => c.Cancel(), Times.Once);
        }

        [Fact]
        public void CommandText_invokes_CommandText_getter_and_setter_on_wrapped_command()
        {
            var mockCommand = new Mock<DbCommand>();
            mockCommand.Setup(c => c.CommandText)
                .Returns("xyz");

            var cachingCommand = 
                new CachingCommand(
                    mockCommand.Object,
                    new CommandTreeFacts(null, true, true),
                    new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object,
                    Mock.Of<CachingPolicy>());

            Assert.Equal("xyz", cachingCommand.CommandText);

            cachingCommand.CommandText = "abc";
            mockCommand.VerifySet(c => c.CommandText = "abc");
        }

        [Fact]
        public void CommandTimeout_invokes_CommandTimeout_getter_and_setter_on_wrapped_command()
        {
            var mockCommand = new Mock<DbCommand>();
            mockCommand.Setup(c => c.CommandTimeout)
                .Returns(42);

            var cachingCommand = 
                new CachingCommand(
                    mockCommand.Object,
                    new CommandTreeFacts(null, true, true),
                    new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object,
                    Mock.Of<CachingPolicy>());

            Assert.Equal(42, cachingCommand.CommandTimeout);

            cachingCommand.CommandTimeout = 99;
            mockCommand.VerifySet(c => c.CommandTimeout = 99);
        }

        [Fact]
        public void CommandType_invokes_CommandType_getter_and_setter_on_wrapped_command()
        {
            var mockCommand = new Mock<DbCommand>();
            mockCommand.Setup(c => c.CommandType)
                .Returns(CommandType.StoredProcedure);

            var cachingCommand =
                new CachingCommand(
                    mockCommand.Object,
                    new CommandTreeFacts(null, true, true),
                    new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object,
                    Mock.Of<CachingPolicy>());

            Assert.Equal(CommandType.StoredProcedure, cachingCommand.CommandType);

            cachingCommand.CommandType = CommandType.TableDirect;
            mockCommand.VerifySet(c => c.CommandType = CommandType.TableDirect);
        }


        [Fact]
        public void DbConnection_invokes_Connection_getter_and_setter_on_wrapped_command()
        {
            var connection = new Mock<DbConnection>().Object;

            var mockCommand = new Mock<DbCommand>();
            mockCommand.Protected()
                .Setup<DbConnection>("DbConnection")
                .Returns(connection);

            var cachingCommand =
                new CachingCommand(
                    mockCommand.Object,
                    new CommandTreeFacts(null, true, true),
                    new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object,
                    Mock.Of<CachingPolicy>());

            Assert.Same(connection, cachingCommand.Connection);

            cachingCommand.Connection = connection;
            mockCommand.Protected()
                .VerifySet<DbConnection>("DbConnection", Times.Once(), connection);
        }

        [Fact]
        public void DbTransaction_invokes_Transaction_getter_and_setter_on_wrapped_command()
        {
            var transaction = new Mock<DbTransaction>().Object;

            var mockCommand = new Mock<DbCommand>();
            mockCommand.Protected()
                .Setup<DbTransaction>("DbTransaction")
                .Returns(transaction);

            var cachingCommand = 
                new CachingCommand(
                    mockCommand.Object,
                    new CommandTreeFacts(null, true, true),
                    new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object,
                    Mock.Of<CachingPolicy>());

            Assert.Same(transaction, cachingCommand.Transaction);

            cachingCommand.Transaction = transaction;
            mockCommand.Protected() 
                .VerifySet<DbTransaction>("DbTransaction", Times.Once(), transaction);
        }

        [Fact]
        public void UpdatedRowSource_invokes_UpdatedRowSource_getter_and_setter_on_wrapped_command()
        {
            var mockCommand = new Mock<DbCommand>();
            mockCommand.Setup(c => c.UpdatedRowSource)
                .Returns(UpdateRowSource.FirstReturnedRecord);

            var cachingCommand = 
                new CachingCommand(
                    mockCommand.Object,
                    new CommandTreeFacts(null, true, true),
                    new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object,
                    Mock.Of<CachingPolicy>());

            Assert.Equal(UpdateRowSource.FirstReturnedRecord, cachingCommand.UpdatedRowSource);

            cachingCommand.UpdatedRowSource = UpdateRowSource.OutputParameters;
            mockCommand.VerifySet(c => c.UpdatedRowSource = UpdateRowSource.OutputParameters);
        }

        [Fact]
        public void DbParameterCollection_invokes_Parameters_getter_on_wrapped_command()
        {
            var parameterCollection = new Mock<DbParameterCollection>().Object;

            var mockCommand = new Mock<DbCommand>();
            mockCommand.Protected()
                .Setup<DbParameterCollection>("DbParameterCollection")
                .Returns(parameterCollection);

            var cachingCommand = 
                new CachingCommand(
                    mockCommand.Object,
                    new CommandTreeFacts(null, true, true),
                    new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object,
                    Mock.Of<CachingPolicy>());

            Assert.Same(parameterCollection, cachingCommand.Parameters);
        }

        [Fact]
        public void ExecuteDbDataReader_invokes_ExecuteReader_method_on_wrapped_command()
        {
            var mockCommand = new Mock<DbCommand>();
            mockCommand
                .Protected()
                .Setup<DbDataReader>("ExecuteDbDataReader", ItExpr.IsAny<CommandBehavior>())
                .Returns(Mock.Of<DbDataReader>());

            mockCommand.Setup(c => c.CommandText).Returns("Query");

            new CachingCommand(
                mockCommand.Object,
                new CommandTreeFacts(new List<EntitySetBase>().AsReadOnly(), true, true),
                new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object,
                Mock.Of<CachingPolicy>())
                .ExecuteReader(CommandBehavior.SequentialAccess);

            mockCommand
                .Protected()
                .Verify<DbDataReader>(
                    "ExecuteDbDataReader", Times.Once(), CommandBehavior.SequentialAccess);
        }

        [Fact]
        public void ExecuteNonQuery_invokes_ExecuteNonQuery_method_on_wrapped_command()
        {
            var mockCommand = new Mock<DbCommand>();
            mockCommand
                .Setup(c => c.ExecuteNonQuery())
                .Returns(42);

            Assert.Equal(42, 
                new CachingCommand(
                    mockCommand.Object,
                    new CommandTreeFacts(new List<EntitySetBase>().AsReadOnly(), true, false),
                    new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object,
                    Mock.Of<CachingPolicy>()).ExecuteNonQuery());

            mockCommand.Verify(c => c.ExecuteNonQuery(), Times.Once);
        }

        [Fact]
        public void ExecuteScalar_invokes_ExecuteScalar_method_on_wrapped_command()
        {
            var retValue = new object();

            var mockCommand = new Mock<DbCommand>();
            mockCommand
                .Setup(c => c.ExecuteScalar())
                .Returns(retValue);

            mockCommand.Setup(c => c.CommandText).Returns("Query");

            Assert.Same(retValue, 
                new CachingCommand(
                    mockCommand.Object,
                    new CommandTreeFacts(new List<EntitySetBase>().AsReadOnly(), true, true),
                    new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object,
                    Mock.Of<CachingPolicy>()).ExecuteScalar());

            mockCommand.Verify(c => c.ExecuteScalar(), Times.Once);
        }

        [Fact]
        public void Prepare_invokes_Prepare_method_on_wrapped_command()
        {
            var mockCommand = new Mock<DbCommand>();

            new CachingCommand(
                mockCommand.Object,
                new CommandTreeFacts(null, true, false),
                new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object,
                Mock.Of<CachingPolicy>()).Prepare();

            mockCommand.Verify(c => c.Prepare(), Times.Once);
        }

        [Fact]
        public void CreateDbParameter_invokes_CreateDbParameter_method_on_wrapped_command()
        {
            var dbParam = new Mock<DbParameter>().Object;

            var mockCommand = new Mock<DbCommand>();
            mockCommand
                .Protected()
                .Setup<DbParameter>("CreateDbParameter")
                .Returns(dbParam);

            Assert.Same(dbParam,
                new CachingCommand(
                    mockCommand.Object,
                    new CommandTreeFacts(null, true, false),
                    new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object,
                    Mock.Of<CachingPolicy>()).CreateParameter());

            mockCommand
                .Protected()
                .Verify("CreateDbParameter", Times.Once());
        }

        [Fact]
        public void ExecuteDbDataReader_consumes_results_and_creates_CachingReader_if_query_cacheable()
        {
            var mockReader = CreateMockReader(1);
            var mockCommand =
                CreateMockCommand(reader: mockReader.Object);

            var cachingCommand =
                new CachingCommand(
                    mockCommand.Object,
                    new CommandTreeFacts(new List<EntitySetBase>().AsReadOnly(), true, false),
                    new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object,
                    new CachingPolicy());
            var reader = cachingCommand.ExecuteReader();

            Assert.IsType<CachingReader>(reader);
            mockReader
                .Protected()
                .Verify("Dispose", Times.Once(), true);

            Assert.True(reader.Read());
            Assert.Equal("int", reader.GetDataTypeName(0));
            Assert.Equal(typeof(int), reader.GetFieldType(0));
            Assert.Equal("Id", reader.GetName(0));
            Assert.Equal("nvarchar", reader.GetDataTypeName(1));
            Assert.Equal(typeof(string), reader.GetFieldType(1));
            Assert.Equal("Name", reader.GetName(1));
        }

        [Fact]
        public void ExecuteDbDataReader_does_not_create_CachingReader_if_query_non_cacheable()
        {
            var mockReader = CreateMockReader(1);
            var mockCommand =
                CreateMockCommand(reader: mockReader.Object);

            var cachingCommand =
                new CachingCommand(
                    mockCommand.Object,
                    new CommandTreeFacts(new List<EntitySetBase>().AsReadOnly(), true, true),
                    new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object,
                    Mock.Of<CachingPolicy>());

            using (var reader = cachingCommand.ExecuteReader())
            {
                Assert.IsNotType<CachingReader>(reader);
                mockReader
                    .Protected()
                    .Verify("Dispose", Times.Never(), true);
            }
        }

        [Fact]
        public void Results_cached_for_cacheable_queries()
        {
            var mockReader = CreateMockReader(1);
            var transaction = Mock.Of<DbTransaction>();

            var mockCommand =
                CreateMockCommand(CreateParameterCollection(
                        new[] { "Param1", "Param2" },
                        new object[] { 123, "abc" }), 
                        mockReader.Object,
                        transaction); 

            var mockTransactionHandler = new Mock<CacheTransactionHandler>(Mock.Of<ICache>());

            var slidingExpiration = new TimeSpan(20, 0 ,0);
            var absoluteExpiration = DateTimeOffset.Now.AddMinutes(20);
            var mockCachingPolicy = new Mock<CachingPolicy>();
            mockCachingPolicy
                .Setup(p => p.GetExpirationTimeout(
                            It.IsAny<ReadOnlyCollection<EntitySetBase>>(),
                            out slidingExpiration, out absoluteExpiration));
            mockCachingPolicy
                .Setup(p => p.CanBeCached(It.IsAny<ReadOnlyCollection<EntitySetBase>>(), It.IsAny<string>(),
                            It.IsAny<IEnumerable<KeyValuePair<string, object>>>()))
                .Returns(true);

            int minCachableRows = 0, maxCachableRows = int.MaxValue;
            mockCachingPolicy
                .Setup(p => p.GetCacheableRows(It.IsAny<ReadOnlyCollection<EntitySetBase>>(),
                    out minCachableRows, out maxCachableRows));

            var cachingCommand = new CachingCommand(
                mockCommand.Object,
                new CommandTreeFacts(
                    TestUtils.CreateEntitySets("ES1", "ES2"), isQuery: true, usesNonDeterministicFunctions: false),
                    mockTransactionHandler.Object,
                    mockCachingPolicy.Object);

            cachingCommand.ExecuteReader();

            mockTransactionHandler.Verify(
                h => h.PutItem(
                    transaction,
                    "db_Query_Param1=123_Param2=abc",
                    It.Is<CachedResults>(r => r.Results.Count == 1 && r.RecordsAffected == 1 && r.TableMetadata.Length == 2),
                    It.Is<IEnumerable<string>>(es => es.SequenceEqual(new [] { "ES1", "ES2"})),
                    slidingExpiration,
                    absoluteExpiration,
                    It.IsNotNull<DbConnection>()),
                Times.Once);
        }

        [Fact]
        public void Results_not_cached_for_non_cacheable_queries()
        {
            var reader = new Mock<DbDataReader>().Object;
            var mockCommand = CreateMockCommand(reader: reader);

            var mockTransactionHandler = new Mock<CacheTransactionHandler>(Mock.Of<ICache>());

            var cachingCommand = new CachingCommand(
                mockCommand.Object,
                new CommandTreeFacts(
                    TestUtils.CreateEntitySets("ES1", "ES2"), isQuery: true, usesNonDeterministicFunctions: true),
                    mockTransactionHandler.Object,
                    Mock.Of<CachingPolicy>());

            using (var r = cachingCommand.ExecuteReader())
            {
                Assert.Same(reader, r);

                object value;

                mockTransactionHandler.Verify(
                    c => c.GetItem(It.IsAny<DbTransaction>(), It.IsAny<string>(), It.IsAny<DbConnection>(), out value),
                    Times.Never());

                mockTransactionHandler.Verify(
                    h => h.PutItem(
                        It.IsAny<DbTransaction>(),
                        It.IsAny<string>(),
                        It.IsAny<object>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<DateTimeOffset>(),
                        It.IsAny<DbConnection>()),
                    Times.Never);
            }
        }

        [Fact]
        public void Results_not_cached_if_results_non_cacheable_per_caching_policy()
        {
            var reader = new Mock<DbDataReader>().Object;
            var mockCommand = CreateMockCommand(reader: reader);

            var mockTransactionHandler = new Mock<CacheTransactionHandler>(Mock.Of<ICache>());

            var cachingCommand = new CachingCommand(
                mockCommand.Object,
                new CommandTreeFacts(
                    TestUtils.CreateEntitySets("ES1", "ES2"), isQuery: true, usesNonDeterministicFunctions: false),
                    mockTransactionHandler.Object,
                    Mock.Of<CachingPolicy>());

            using (var r = cachingCommand.ExecuteReader())
            {
                Assert.Same(reader, r);

                object value;

                mockTransactionHandler.Verify(
                    c => c.GetItem(It.IsAny<DbTransaction>(), It.IsAny<string>(), It.IsAny<DbConnection>(), out value),
                    Times.Never());

                mockTransactionHandler.Verify(
                    h => h.PutItem(
                        It.IsAny<DbTransaction>(),
                        It.IsAny<string>(),
                        It.IsAny<object>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<DateTimeOffset>(),
                        It.IsAny<DbConnection>()),
                    Times.Never);
            }
        }

        [Fact]
        public void Results_not_cached_if_too_many_or_to_few_rows()
        {
            var cacheableRowLimits = new[]
            {
                new { MinCacheableRows = 0, MaxCacheableRows = 4 },
                new { MinCacheableRows = 6, MaxCacheableRows = 100 }
            };

            foreach (var cachableRowLimit in cacheableRowLimits)
            {
                var mockCommand = CreateMockCommand(reader: CreateMockReader(5).Object);

                var mockTransactionHandler = new Mock<CacheTransactionHandler>(Mock.Of<ICache>());

                var minCacheableRows = cachableRowLimit.MinCacheableRows;
                var maxCacheableRows = cachableRowLimit.MaxCacheableRows;

                var mockCachingPolicy = new Mock<CachingPolicy>();
                mockCachingPolicy
                    .Setup(p => p.GetCacheableRows(
                        It.IsAny<ReadOnlyCollection<EntitySetBase>>(),
                        out minCacheableRows, out maxCacheableRows));
                mockCachingPolicy
                    .Setup(p => p.CanBeCached(It.IsAny<ReadOnlyCollection<EntitySetBase>>(), It.IsAny<string>(), 
                                    It.IsAny<IEnumerable<KeyValuePair<string, object>>>()))
                    .Returns(true);

                var cachingCommand = new CachingCommand(
                    mockCommand.Object,
                    new CommandTreeFacts(
                        TestUtils.CreateEntitySets("ES1", "ES2"), isQuery: true, usesNonDeterministicFunctions: false),
                    mockTransactionHandler.Object,
                    mockCachingPolicy.Object);

                cachingCommand.ExecuteReader();

                mockTransactionHandler.Verify(
                    h => h.PutItem(
                        It.IsAny<DbTransaction>(),
                        It.IsAny<string>(),
                        It.IsAny<object>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<DateTimeOffset>(),
                        It.IsAny<DbConnection>()),
                    Times.Never);
            }
        }

        [Fact]
        public void Results_cached_if_too_many_or_to_few_rows_if_query_exists_in_AlwaysCachedQueriesRegistrar()
        {
            var cacheableRowLimits = new[]
            {
                new { MinCacheableRows = 0, MaxCacheableRows = 4 },
                new { MinCacheableRows = 6, MaxCacheableRows = 100 }
            };

            var metadataWorkspace = new MetadataWorkspace();
            AlwaysCachedQueriesRegistrar.Instance.AddCachedQuery(metadataWorkspace, "Query");

            foreach (var cachableRowLimit in cacheableRowLimits)
            {
                var mockCommand = CreateMockCommand(reader: CreateMockReader(5).Object);

                var mockTransactionHandler = new Mock<CacheTransactionHandler>(Mock.Of<ICache>());

                var minCacheableRows = cachableRowLimit.MinCacheableRows;
                var maxCacheableRows = cachableRowLimit.MaxCacheableRows;

                var mockCachingPolicy = new Mock<CachingPolicy>();
                mockCachingPolicy
                    .Setup(p => p.GetCacheableRows(
                        It.IsAny<ReadOnlyCollection<EntitySetBase>>(),
                        out minCacheableRows, out maxCacheableRows));
                mockCachingPolicy
                    .Setup(p => p.CanBeCached(It.IsAny<ReadOnlyCollection<EntitySetBase>>(), It.IsAny<string>(),
                                    It.IsAny<IEnumerable<KeyValuePair<string, object>>>()))
                    .Returns(true);

                var cachingCommand = new CachingCommand(
                    mockCommand.Object,
                    new CommandTreeFacts(
                        TestUtils.CreateEntitySets("ES1", "ES2"), isQuery: true, usesNonDeterministicFunctions: false, metadataWorkspace: metadataWorkspace),
                    mockTransactionHandler.Object,
                    mockCachingPolicy.Object);

                cachingCommand.ExecuteReader();

                mockTransactionHandler.Verify(
                    h => h.PutItem(
                        It.IsAny<DbTransaction>(),
                        It.IsAny<string>(),
                        It.IsAny<object>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<DateTimeOffset>(),
                        It.IsNotNull<DbConnection>()),
                    Times.Once);
            }
        }

        [Fact]
        public void ExecuteReader_on_wrapped_command_not_invoked_for_cached_results()
        {
            var mockCommand = CreateMockCommand();

            object value = new CachedResults(new ColumnMetadata[0], new List<object[]>(), 42);

            var mockTransactionHandler = new Mock<CacheTransactionHandler>(Mock.Of<ICache>());
            mockTransactionHandler
                .Setup(h => h.GetItem(It.IsAny<DbTransaction>(), It.IsAny<string>(), It.IsAny<DbConnection>(), out value))
                .Returns(true);

            var cachingCommand = new CachingCommand(
                mockCommand.Object,
                new CommandTreeFacts(new List<EntitySetBase>().AsReadOnly(), true, false),
                mockTransactionHandler.Object,
                new CachingPolicy());

            using(var reader = cachingCommand.ExecuteReader(CommandBehavior.SequentialAccess))
            {
                Assert.Equal(42, reader.RecordsAffected);
            }

            mockCommand
                .Protected()
                .Verify<DbDataReader>(
                    "ExecuteDbDataReader", Times.Never(), ItExpr.IsAny<CommandBehavior>());
        }

        [Fact]
        public void ExecuteScalar_caches_result_for_cacheable_command()
        {
            var retValue = new object();
            var transaction = Mock.Of<DbTransaction>();

            var mockCommand = 
                CreateMockCommand(
                    CreateParameterCollection(new[] { "P1", "P2" }, new object[] { "ZZZ", 123 }),
                    transaction: transaction);

            mockCommand
                .Setup(c => c.ExecuteScalar())
                .Returns(retValue);
            mockCommand
                .Setup(c => c.CommandText)
                .Returns("Exec");

            var mockTransactionHandler = new Mock<CacheTransactionHandler>(Mock.Of<ICache>());

            var slidingExpiration = new TimeSpan(20, 0, 0);
            var absoluteExpiration = DateTimeOffset.Now.AddMinutes(20);
            var mockCachingPolicy = new Mock<CachingPolicy>();
            mockCachingPolicy
                .Setup(p => p.GetExpirationTimeout(
                        It.IsAny<ReadOnlyCollection<EntitySetBase>>(),
                        out slidingExpiration, out absoluteExpiration));
            mockCachingPolicy
                .Setup(p => p.CanBeCached(It.IsAny<ReadOnlyCollection<EntitySetBase>>(), It.IsAny<string>(),
                            It.IsAny<IEnumerable<KeyValuePair<string, object>>>()))
                .Returns(true);

            var result =
                new CachingCommand(
                    mockCommand.Object,
                    new CommandTreeFacts(TestUtils.CreateEntitySets("ES1", "ES2"), true, false),
                    mockTransactionHandler.Object,
                    mockCachingPolicy.Object).ExecuteScalar();

            Assert.Same(retValue, result);
            object value;

            mockTransactionHandler.Verify(
                h => h.GetItem(transaction, "db_Exec_P1=ZZZ_P2=123", It.IsNotNull<DbConnection>(), out value), Times.Once);

            mockCommand.Verify(c => c.ExecuteScalar(), Times.Once);

            mockTransactionHandler.Verify(
                h => h.PutItem(
                    transaction,
                    "db_Exec_P1=ZZZ_P2=123",
                    retValue,
                    new[] {"ES1", "ES2"},
                    slidingExpiration,
                    absoluteExpiration,
                    It.IsNotNull<DbConnection>()),
                    Times.Once);
        }

        [Fact]
        public void ExecuteScalar_returns_cached_result_if_exists()
        {
            var retValue = new object();
            var transaction = Mock.Of<DbTransaction>();

            var mockCommand =
                CreateMockCommand(
                    CreateParameterCollection(new[] { "P1", "P2" }, new object[] { "ZZZ", 123 }),
                    transaction: transaction);

            mockCommand
                .Setup(c => c.ExecuteScalar())
                .Returns(retValue);
            mockCommand
                .Setup(c => c.CommandText)
                .Returns("Exec");

            var mockTransactionHandler = new Mock<CacheTransactionHandler>(Mock.Of<ICache>());
            mockTransactionHandler
                .Setup(h => h.GetItem(transaction, "db_Exec_P1=ZZZ_P2=123", It.IsAny<DbConnection>(), out retValue))
                .Returns(true);

            var result =
                new CachingCommand(
                    mockCommand.Object,
                    new CommandTreeFacts(TestUtils.CreateEntitySets("ES1", "ES2"), true, false),
                    mockTransactionHandler.Object,
                    new CachingPolicy()).ExecuteScalar();

            Assert.Same(retValue, result);

            object value;
            mockTransactionHandler.Verify(
                h => h.GetItem(transaction, "db_Exec_P1=ZZZ_P2=123", It.IsNotNull<DbConnection>(), out value), Times.Once);

            mockCommand.Verify(h => h.ExecuteScalar(), Times.Never);

            mockTransactionHandler.Verify(
                h => h.PutItem(
                    It.IsAny<DbTransaction>(),
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<DbConnection>()),
                    Times.Never);
        }

        [Fact]
        public void ExecuteScalar_does_not_cache_results_for_non_cacheable_queries()
        {
            var retValue = new object();

            var mockCommand = new Mock<DbCommand>();
            mockCommand
                .Setup(c => c.ExecuteScalar())
                .Returns(retValue);

            mockCommand.Setup(c => c.CommandText).Returns("Query");

            var mockTransactionHandler = new Mock<CacheTransactionHandler>(Mock.Of<ICache>());

            var result =
                new CachingCommand(
                    mockCommand.Object,
                    new CommandTreeFacts(TestUtils.CreateEntitySets("ES1", "ES2"), true, true),
                    mockTransactionHandler.Object,
                    Mock.Of<CachingPolicy>()).ExecuteScalar();

            Assert.Same(retValue, result);
            object value;
            mockTransactionHandler.Verify(
                h => h.GetItem(It.IsAny<DbTransaction>(), It.IsAny<string>(), It.IsAny<DbConnection>(), out value), Times.Never);

            mockCommand.Verify(c => c.ExecuteScalar(), Times.Once);

            mockTransactionHandler.Verify(
                h => h.PutItem(
                    It.IsAny<DbTransaction>(),
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<DbConnection>()),
                    Times.Never);
        }

        [Fact]
        public void ExecuteScalar_does_not_cache_results_if_non_cacheable_per_CachingPolicy()
        {
            var retValue = new object();

            var mockCommand = new Mock<DbCommand>();
            mockCommand
                .Setup(c => c.ExecuteScalar())
                .Returns(retValue);
            mockCommand
                .Setup(c => c.CommandText)
                .Returns("Exec");
            mockCommand
                .Protected()
                .Setup<DbParameterCollection>("DbParameterCollection")
                .Returns(CreateParameterCollection(new[] { "P1", "P2" }, new object[] { "ZZZ", 123 }));

            var mockTransactionHandler = new Mock<CacheTransactionHandler>(Mock.Of<ICache>());

            var result =
                new CachingCommand(
                    mockCommand.Object,
                    new CommandTreeFacts(TestUtils.CreateEntitySets("ES1", "ES2"), true, false),
                    mockTransactionHandler.Object,
                    Mock.Of<CachingPolicy>()).ExecuteScalar();

            Assert.Same(retValue, result);
            object value;
            mockTransactionHandler.Verify(
                h => h.GetItem(It.IsAny<DbTransaction>(), It.IsAny<string>(), It.IsAny<DbConnection>(), out value), Times.Never);

            mockCommand.Verify(c => c.ExecuteScalar(), Times.Once);

            mockTransactionHandler.Verify(
                h => h.PutItem(
                    It.IsAny<DbTransaction>(),
                    It.IsAny<string>(),
                    It.IsAny<object>(),
                    It.IsAny<IEnumerable<string>>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<DbConnection>()),
                    Times.Never);
        }

        [Fact]
        public void ExecuteNonQuery_invalidates_cache_for_given_entity_sets_if_any_affected_records()
        {
            var transaction = Mock.Of<DbTransaction>();
            var mockCommand = CreateMockCommand(transaction: transaction);
            mockCommand
                .Setup(c => c.ExecuteNonQuery())
                .Returns(1);

            var mockTransactionHandler = new Mock<CacheTransactionHandler>(Mock.Of<ICache>());

            var rowsAffected = new CachingCommand(
                mockCommand.Object,
                new CommandTreeFacts(TestUtils.CreateEntitySets("ES1", "ES2"), true, true),
                mockTransactionHandler.Object,
                Mock.Of<CachingPolicy>()).ExecuteNonQuery();

            Assert.Equal(rowsAffected, 1);
            mockCommand.Verify(c => c.ExecuteNonQuery(), Times.Once());
            mockTransactionHandler
                .Verify(h => h.InvalidateSets(transaction, new[] { "ES1", "ES2" }, It.IsNotNull<DbConnection>()), Times.Once());
        }

        [Fact]
        public void ExecuteNonQuery_does_not_invalidate_cache_if_no_records_affected()
        {
            var mockCommand = new Mock<DbCommand>();
            mockCommand
                .Setup(c => c.ExecuteNonQuery())
                .Returns(0);

            var mockTransactionHandler = new Mock<CacheTransactionHandler>(Mock.Of<ICache>());

            var rowsAffected = new CachingCommand(
                mockCommand.Object,
                new CommandTreeFacts(TestUtils.CreateEntitySets("ES1", "ES2"), true, true),
                mockTransactionHandler.Object,
                Mock.Of<CachingPolicy>()).ExecuteNonQuery();

            Assert.Equal(rowsAffected, 0);
            mockCommand.Verify(c => c.ExecuteNonQuery(), Times.Once());
            mockTransactionHandler
                .Verify(h => h.InvalidateSets(It.IsAny<DbTransaction>(), It.IsAny<IEnumerable<string>>(), It.IsAny<DbConnection>()), Times.Never());
        }

        [Fact]
        public void ExecuteNonQuery_does_not_invalidate_cache_if_no_entitysets_affected()
        {
            var mockCommand = new Mock<DbCommand>();
            mockCommand
                .Setup(c => c.ExecuteNonQuery())
                .Returns(1);

            var mockTransactionHandler = new Mock<CacheTransactionHandler>(Mock.Of<ICache>());

            var rowsAffected = new CachingCommand(
                mockCommand.Object,
                new CommandTreeFacts(new List<EntitySetBase>().AsReadOnly(), true, true),
                mockTransactionHandler.Object,
                Mock.Of<CachingPolicy>()).ExecuteNonQuery();

            Assert.Equal(rowsAffected, 1);
            mockCommand.Verify(c => c.ExecuteNonQuery(), Times.Once());
            mockTransactionHandler
                .Verify(h => h.InvalidateSets(It.IsAny<DbTransaction>(), It.IsAny<IEnumerable<string>>(), It.IsNotNull<DbConnection>()), Times.Never());
        }

        [Fact]
        public void Dispose_invokes_Dispose_on_wrapped_command()
        {
            var mockCommand = new Mock<DbCommand>();

            var mockTransactionHandler = new Mock<CacheTransactionHandler>(Mock.Of<ICache>());

            new CachingCommand(mockCommand.Object,
                new CommandTreeFacts(new List<EntitySetBase>().AsReadOnly(), true, true),
                mockTransactionHandler.Object,
                Mock.Of<CachingPolicy>()).Dispose();

            mockCommand.Protected().Verify("Dispose", Times.Once(), true);
        }

        private static Mock<DbCommand> CreateMockCommand(DbParameterCollection parameterCollection = null, DbDataReader reader = null, DbTransaction transaction = null)
        {
            var mockConnection = new Mock<DbConnection>();
            mockConnection
                .Setup(c => c.Database)
                .Returns("db");

            var mockCommand = new Mock<DbCommand>();
            mockCommand
                .Setup(c => c.CommandText)
                .Returns("Query");

            mockCommand
                .Protected()
                .Setup<DbParameterCollection>("DbParameterCollection")
                .Returns(parameterCollection ?? CreateParameterCollection(new string[0], new object[0]));

            mockCommand
                .Protected()
                .Setup<DbConnection>("DbConnection")
                .Returns(mockConnection.Object);

            if (reader != null)
            {
                mockCommand
                    .Protected()
                    .Setup<DbDataReader>("ExecuteDbDataReader", ItExpr.IsAny<CommandBehavior>())
                    .Returns(reader);

                mockCommand
                    .Protected()
                    .Setup<Task<DbDataReader>>("ExecuteDbDataReaderAsync", ItExpr.IsAny<CommandBehavior>(), ItExpr.IsAny<CancellationToken>())
                    .Returns(Task.FromResult(reader));
            }

            mockCommand
                .Protected()
                .Setup<DbTransaction>("DbTransaction")
                .Returns(transaction ?? Mock.Of<DbTransaction>());

            return mockCommand;
        }

        private static Mock<DbDataReader> CreateMockReader(int resultCount)
        {
            var mockReader = new Mock<DbDataReader>();
            mockReader
                .Setup(r => r.FieldCount)
                .Returns(2);
            mockReader
                .Setup(r => r.GetDataTypeName(0))
                .Returns("int");
            mockReader
                .Setup(r => r.GetDataTypeName(1))
                .Returns("nvarchar");
            mockReader
                .Setup(r => r.GetFieldType(0))
                .Returns(typeof(int));
            mockReader
                .Setup(r => r.GetFieldType(1))
                .Returns(typeof(string));
            mockReader
                .Setup(r => r.GetName(0))
                .Returns("Id");
            mockReader
                .Setup(r => r.GetName(1))
                .Returns("Name");
            mockReader
                .Setup(r => r.RecordsAffected)
                .Returns(resultCount);
            mockReader.As<IDisposable>()
                .CallBase = true;

            mockReader
                .Setup(r => r.GetValues(It.IsAny<object[]>()))
                .Callback((object[] values) =>
                {
                    values[0] = 1;
                    values[1] = "test";
                });

            mockReader
                .Setup(r => r.Read())
                .Returns(() => resultCount-- > 0);

            mockReader
                .Setup(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(resultCount-- > 0));

            return mockReader;
        }

        private static DbParameterCollection CreateParameterCollection(string[] names, object[] values)
        {
            Debug.Assert(names.Length == values.Length, "names.Length is not equal values.Length");

            var parameters = new List<DbParameter>(names.Length);

            for (var i = 0; i < names.Length; i++)
            {
                var mockParameter = new Mock<DbParameter>();
                mockParameter
                    .Setup(p => p.ParameterName)
                    .Returns(names[i]);
                mockParameter
                    .Setup(p => p.Value)
                    .Returns(values[i]);

                parameters.Add(mockParameter.Object);
            }

            var mockParameterCollection = new Mock<DbParameterCollection>();
            mockParameterCollection.As<IEnumerable>();
            mockParameterCollection
                .Setup(c => c.GetEnumerator())
                .Returns(parameters.GetEnumerator());

            return mockParameterCollection.Object;
        }

        public class AsyncTests
        {
            [Fact]
            public async Task ExecuteNonQueryAsync_invokes_ExecuteNonQueryAsync_method_on_wrapped_command()
            {
                var mockCommand = new Mock<DbCommand>();
                mockCommand
                    .Setup(c => c.ExecuteNonQueryAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(42));

                Assert.Equal(42,
                    await new CachingCommand(
                        mockCommand.Object,
                        new CommandTreeFacts(new List<EntitySetBase>().AsReadOnly(), true, false),
                        new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object,
                        Mock.Of<CachingPolicy>()).ExecuteNonQueryAsync());

                mockCommand.Verify(c => c.ExecuteNonQueryAsync(It.IsAny<CancellationToken>()), Times.Once);
            }

            [Fact]
            public async Task ExecuteNonQueryAsync_invalidates_cache_for_given_entity_sets_if_any_affected_records()
            {
                var transaction = Mock.Of<DbTransaction>();
                var mockCommand = CreateMockCommand(transaction: transaction);
                mockCommand
                    .Setup(c => c.ExecuteNonQueryAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(1));

                var mockTransactionHandler = new Mock<CacheTransactionHandler>(Mock.Of<ICache>());

                var rowsAffected = await new CachingCommand(
                    mockCommand.Object,
                    new CommandTreeFacts(TestUtils.CreateEntitySets("ES1", "ES2"), true, true),
                    mockTransactionHandler.Object,
                    Mock.Of<CachingPolicy>()).ExecuteNonQueryAsync();

                Assert.Equal(rowsAffected, 1);
                mockCommand.Verify(c => c.ExecuteNonQueryAsync(It.IsAny<CancellationToken>()), Times.Once());
                mockTransactionHandler
                    .Verify(h => h.InvalidateSets(transaction, new[] {"ES1", "ES2"}, It.IsNotNull<DbConnection>()), Times.Once());
            }

            [Fact]
            public async Task ExecuteNonQueryAsync_does_not_invalidate_cache_if_no_records_affected()
            {
                var mockCommand = new Mock<DbCommand>();
                mockCommand
                    .Setup(c => c.ExecuteNonQueryAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(0));

                var mockTransactionHandler = new Mock<CacheTransactionHandler>(Mock.Of<ICache>());

                var rowsAffected = await new CachingCommand(
                    mockCommand.Object,
                    new CommandTreeFacts(TestUtils.CreateEntitySets("ES1", "ES2"), true, true),
                    mockTransactionHandler.Object,
                    Mock.Of<CachingPolicy>()).ExecuteNonQueryAsync();

                Assert.Equal(rowsAffected, 0);
                mockCommand.Verify(c => c.ExecuteNonQueryAsync(It.IsAny<CancellationToken>()), Times.Once());
                mockTransactionHandler
                    .Verify(h => h.InvalidateSets(It.IsAny<DbTransaction>(), It.IsAny<IEnumerable<string>>(), It.IsAny<DbConnection>()),
                        Times.Never());
            }

            [Fact]
            public async Task ExecuteNonQueryAsync_does_not_invalidate_cache_if_no_entitysets_affected()
            {
                var mockCommand = new Mock<DbCommand>();
                mockCommand
                    .Setup(c => c.ExecuteNonQueryAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(1));

                mockCommand.Setup(c => c.CommandText).Returns("Query");

                var mockTransactionHandler = new Mock<CacheTransactionHandler>(Mock.Of<ICache>());

                var rowsAffected = await new CachingCommand(
                    mockCommand.Object,
                    new CommandTreeFacts(new List<EntitySetBase>().AsReadOnly(), true, true),
                    mockTransactionHandler.Object,
                    Mock.Of<CachingPolicy>()).ExecuteNonQueryAsync();

                Assert.Equal(rowsAffected, 1);
                mockCommand.Verify(c => c.ExecuteNonQueryAsync(It.IsAny<CancellationToken>()), Times.Once());
                mockTransactionHandler
                    .Verify(h => h.InvalidateSets(It.IsAny<DbTransaction>(), It.IsAny<IEnumerable<string>>(), It.IsAny<DbConnection>()),
                        Times.Never());
            }

            [Fact]
            public async Task ExecuteScalarAsync_invokes_ExecuteScalarAsync_method_on_wrapped_command()
            {
                var retValue = new object();

                var mockCommand = new Mock<DbCommand>();
                mockCommand
                    .Setup(c => c.ExecuteScalarAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(retValue));

                mockCommand.Setup(c => c.CommandText).Returns("Query");

                Assert.Same(retValue,
                    await new CachingCommand(
                        mockCommand.Object,
                        new CommandTreeFacts(new List<EntitySetBase>().AsReadOnly(), true, true),
                        new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object,
                        Mock.Of<CachingPolicy>()).ExecuteScalarAsync());

                mockCommand.Verify(c => c.ExecuteScalarAsync(It.IsAny<CancellationToken>()), Times.Once);
            }

            [Fact]
            public async Task ExecuteScalarAsync_caches_result_for_cacheable_command()
            {
                var retValue = new object();
                var transaction = Mock.Of<DbTransaction>();

                var mockCommand =
                    CreateMockCommand(
                        CreateParameterCollection(new[] {"P1", "P2"}, new object[] {"ZZZ", 123}),
                        transaction: transaction);

                mockCommand
                    .Setup(c => c.ExecuteScalarAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(retValue));
                mockCommand
                    .Setup(c => c.CommandText)
                    .Returns("Exec");

                var mockTransactionHandler = new Mock<CacheTransactionHandler>(Mock.Of<ICache>());

                var slidingExpiration = new TimeSpan(20, 0, 0);
                var absoluteExpiration = DateTimeOffset.Now.AddMinutes(20);
                var mockCachingPolicy = new Mock<CachingPolicy>();
                mockCachingPolicy
                    .Setup(p => p.GetExpirationTimeout(
                        It.IsAny<ReadOnlyCollection<EntitySetBase>>(),
                        out slidingExpiration, out absoluteExpiration));
                mockCachingPolicy
                    .Setup(p => p.CanBeCached(It.IsAny<ReadOnlyCollection<EntitySetBase>>(), It.IsAny<string>(),
                        It.IsAny<IEnumerable<KeyValuePair<string, object>>>()))
                    .Returns(true);

                var result =
                    await new CachingCommand(
                        mockCommand.Object,
                        new CommandTreeFacts(TestUtils.CreateEntitySets("ES1", "ES2"), true, false),
                        mockTransactionHandler.Object,
                        mockCachingPolicy.Object).ExecuteScalarAsync();

                Assert.Same(retValue, result);
                object value;

                mockTransactionHandler.Verify(
                    h => h.GetItem(transaction, "db_Exec_P1=ZZZ_P2=123", It.IsNotNull<DbConnection>(), out value), Times.Once);

                mockCommand.Verify(c => c.ExecuteScalarAsync(It.IsAny<CancellationToken>()), Times.Once);

                mockTransactionHandler.Verify(
                    h => h.PutItem(
                        transaction,
                        "db_Exec_P1=ZZZ_P2=123",
                        retValue,
                        new[] {"ES1", "ES2"},
                        slidingExpiration,
                        absoluteExpiration,
                        It.IsNotNull<DbConnection>()),
                    Times.Once);
            }

            [Fact]
            public async Task ExecuteScalarAsync_returns_cached_result_if_exists()
            {
                var retValue = new object();
                var transaction = Mock.Of<DbTransaction>();

                var mockCommand =
                    CreateMockCommand(
                        CreateParameterCollection(new[] {"P1", "P2"}, new object[] {"ZZZ", 123}),
                        transaction: transaction);

                mockCommand
                    .Setup(c => c.ExecuteScalarAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(retValue));
                mockCommand
                    .Setup(c => c.CommandText)
                    .Returns("Exec");

                var mockTransactionHandler = new Mock<CacheTransactionHandler>(Mock.Of<ICache>());
                mockTransactionHandler
                    .Setup(h => h.GetItem(transaction, "db_Exec_P1=ZZZ_P2=123", It.IsAny<DbConnection>(), out retValue))
                    .Returns(true);

                var result =
                    await new CachingCommand(
                        mockCommand.Object,
                        new CommandTreeFacts(TestUtils.CreateEntitySets("ES1", "ES2"), true, false),
                        mockTransactionHandler.Object,
                        new CachingPolicy()).ExecuteScalarAsync();

                Assert.Same(retValue, result);

                object value;
                mockTransactionHandler.Verify(
                    h => h.GetItem(transaction, "db_Exec_P1=ZZZ_P2=123", It.IsNotNull<DbConnection>(), out value), Times.Once);

                mockCommand.Verify(h => h.ExecuteScalarAsync(It.IsAny<CancellationToken>()), Times.Never);

                mockTransactionHandler.Verify(
                    h => h.PutItem(
                        It.IsAny<DbTransaction>(),
                        It.IsAny<string>(),
                        It.IsAny<object>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<DateTimeOffset>(),
                        It.IsAny<DbConnection>()),
                    Times.Never);
            }

            [Fact]
            public async Task ExecuteScalarAsync_does_not_cache_results_for_non_cacheable_queries()
            {
                var retValue = new object();

                var mockCommand = new Mock<DbCommand>();
                mockCommand
                    .Setup(c => c.ExecuteScalarAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(retValue));

                mockCommand.Setup(c => c.CommandText).Returns("Query");

                var mockTransactionHandler = new Mock<CacheTransactionHandler>(Mock.Of<ICache>());

                var result =
                    await new CachingCommand(
                        mockCommand.Object,
                        new CommandTreeFacts(TestUtils.CreateEntitySets("ES1", "ES2"), true, true),
                        mockTransactionHandler.Object,
                        Mock.Of<CachingPolicy>()).ExecuteScalarAsync();

                Assert.Same(retValue, result);
                object value;
                mockTransactionHandler.Verify(
                    h => h.GetItem(It.IsAny<DbTransaction>(), It.IsAny<string>(), It.IsAny<DbConnection>(), out value), Times.Never);

                mockCommand.Verify(c => c.ExecuteScalarAsync(It.IsAny<CancellationToken>()), Times.Once);

                mockTransactionHandler.Verify(
                    h => h.PutItem(
                        It.IsAny<DbTransaction>(),
                        It.IsAny<string>(),
                        It.IsAny<object>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<DateTimeOffset>(),
                        It.IsAny<DbConnection>()),
                    Times.Never);
            }

            [Fact]
            public async Task ExecuteScalarAsync_does_not_cache_results_if_non_cacheable_per_CachingPolicy()
            {
                var retValue = new object();

                var mockCommand = new Mock<DbCommand>();
                mockCommand
                    .Setup(c => c.ExecuteScalarAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(retValue));
                mockCommand
                    .Setup(c => c.CommandText)
                    .Returns("Exec");
                mockCommand
                    .Protected()
                    .Setup<DbParameterCollection>("DbParameterCollection")
                    .Returns(CreateParameterCollection(new[] {"P1", "P2"}, new object[] {"ZZZ", 123}));

                var mockTransactionHandler = new Mock<CacheTransactionHandler>(Mock.Of<ICache>());

                var result =
                    await new CachingCommand(
                        mockCommand.Object,
                        new CommandTreeFacts(TestUtils.CreateEntitySets("ES1", "ES2"), true, false),
                        mockTransactionHandler.Object,
                        Mock.Of<CachingPolicy>()).ExecuteScalarAsync();

                Assert.Same(retValue, result);
                object value;
                mockTransactionHandler.Verify(
                    h => h.GetItem(It.IsAny<DbTransaction>(), It.IsAny<string>(), It.IsAny<DbConnection>(), out value), Times.Never);

                mockCommand.Verify(c => c.ExecuteScalarAsync(It.IsAny<CancellationToken>()), Times.Once);

                mockTransactionHandler.Verify(
                    h => h.PutItem(
                        It.IsAny<DbTransaction>(),
                        It.IsAny<string>(),
                        It.IsAny<object>(),
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<DateTimeOffset>(), 
                        It.IsAny<DbConnection>()),
                    Times.Never);
            }

            [Fact]
            public async Task ExecuteDbDataReaderAsync_invokes_ExecuteReaderAsync_method_on_wrapped_command()
            {
                var mockCommand = new Mock<DbCommand>();
                mockCommand
                    .Protected()
                    .Setup<Task<DbDataReader>>("ExecuteDbDataReaderAsync", ItExpr.IsAny<CommandBehavior>(),
                        ItExpr.IsAny<CancellationToken>())
                    .Returns(Task.FromResult(Mock.Of<DbDataReader>()));

                mockCommand.Setup(c => c.CommandText).Returns("Query");

                await new CachingCommand(
                    mockCommand.Object,
                    new CommandTreeFacts(new List<EntitySetBase>().AsReadOnly(), true, true),
                    new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object,
                    Mock.Of<CachingPolicy>())
                    .ExecuteReaderAsync(CommandBehavior.SequentialAccess);

                mockCommand
                    .Protected()
                    .Verify<Task<DbDataReader>>(
                        "ExecuteDbDataReaderAsync", Times.Once(), CommandBehavior.SequentialAccess,
                        ItExpr.IsAny<CancellationToken>());
            }

            [Fact]
            public async Task ExecuteDbDataReaderAsync_consumes_results_and_creates_CachingReader_if_query_cacheable()
            {
                var mockReader = CreateMockReader(1);
                var mockCommand =
                    CreateMockCommand(reader: mockReader.Object);

                var cachingCommand =
                    new CachingCommand(
                        mockCommand.Object,
                        new CommandTreeFacts(new List<EntitySetBase>().AsReadOnly(), true, false),
                        new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object,
                        new CachingPolicy());
                var reader = await cachingCommand.ExecuteReaderAsync();

                Assert.IsType<CachingReader>(reader);
                mockReader
                    .Protected()
                    .Verify("Dispose", Times.Once(), true);

                Assert.True(reader.Read());
                Assert.Equal("int", reader.GetDataTypeName(0));
                Assert.Equal(typeof (int), reader.GetFieldType(0));
                Assert.Equal("Id", reader.GetName(0));
                Assert.Equal("nvarchar", reader.GetDataTypeName(1));
                Assert.Equal(typeof (string), reader.GetFieldType(1));
                Assert.Equal("Name", reader.GetName(1));
            }

            [Fact]
            public async Task ExecuteDbDataReaderAsync_does_not_create_CachingReader_if_query_non_cacheable()
            {
                var mockReader = CreateMockReader(1);
                var mockCommand =
                    CreateMockCommand(reader: mockReader.Object);

                var cachingCommand =
                    new CachingCommand(
                        mockCommand.Object,
                        new CommandTreeFacts(new List<EntitySetBase>().AsReadOnly(), true, true),
                        new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object,
                        Mock.Of<CachingPolicy>());

                using (var reader = await cachingCommand.ExecuteReaderAsync())
                {
                    Assert.IsNotType<CachingReader>(reader);
                    mockReader
                        .Protected()
                        .Verify("Dispose", Times.Never(), true);
                }
            }

            [Fact]
            public async Task Results_cached_for_cacheable_queries_Async()
            {
                var mockReader = CreateMockReader(1);
                var transaction = Mock.Of<DbTransaction>();

                var mockCommand =
                    CreateMockCommand(CreateParameterCollection(
                        new[] {"Param1", "Param2"},
                        new object[] {123, "abc"}),
                        mockReader.Object,
                        transaction);

                var mockTransactionHandler = new Mock<CacheTransactionHandler>(Mock.Of<ICache>());

                var slidingExpiration = new TimeSpan(20, 0, 0);
                var absoluteExpiration = DateTimeOffset.Now.AddMinutes(20);
                var mockCachingPolicy = new Mock<CachingPolicy>();
                mockCachingPolicy
                    .Setup(p => p.GetExpirationTimeout(
                        It.IsAny<ReadOnlyCollection<EntitySetBase>>(),
                        out slidingExpiration, out absoluteExpiration));
                mockCachingPolicy
                    .Setup(p => p.CanBeCached(It.IsAny<ReadOnlyCollection<EntitySetBase>>(), It.IsAny<string>(),
                        It.IsAny<IEnumerable<KeyValuePair<string, object>>>()))
                    .Returns(true);

                int minCachableRows = 0, maxCachableRows = int.MaxValue;
                mockCachingPolicy
                    .Setup(p => p.GetCacheableRows(It.IsAny<ReadOnlyCollection<EntitySetBase>>(),
                        out minCachableRows, out maxCachableRows));

                var cachingCommand = new CachingCommand(
                    mockCommand.Object,
                    new CommandTreeFacts(
                        TestUtils.CreateEntitySets("ES1", "ES2"), isQuery: true, usesNonDeterministicFunctions: false),
                    mockTransactionHandler.Object,
                    mockCachingPolicy.Object);

                await cachingCommand.ExecuteReaderAsync();

                mockTransactionHandler.Verify(
                    h => h.PutItem(
                        transaction,
                        "db_Query_Param1=123_Param2=abc",
                        It.Is<CachedResults>(
                            r => r.Results.Count == 1 && r.RecordsAffected == 1 && r.TableMetadata.Length == 2),
                        It.Is<IEnumerable<string>>(es => es.SequenceEqual(new[] {"ES1", "ES2"})),
                        slidingExpiration,
                        absoluteExpiration,
                        It.IsNotNull<DbConnection>()),
                    Times.Once);
            }

            [Fact]
            public async Task Results_not_cached_for_non_cacheable_queries_Async()
            {
                var reader = new Mock<DbDataReader>().Object;
                var mockCommand = CreateMockCommand(reader: reader);

                var mockTransactionHandler = new Mock<CacheTransactionHandler>(Mock.Of<ICache>());

                var cachingCommand = new CachingCommand(
                    mockCommand.Object,
                    new CommandTreeFacts(
                        TestUtils.CreateEntitySets("ES1", "ES2"), isQuery: true, usesNonDeterministicFunctions: true),
                    mockTransactionHandler.Object,
                    Mock.Of<CachingPolicy>());

                using (var r = await cachingCommand.ExecuteReaderAsync())
                {
                    Assert.Same(reader, r);

                    object value;

                    mockTransactionHandler.Verify(
                        c => c.GetItem(It.IsAny<DbTransaction>(), It.IsAny<string>(), It.IsAny<DbConnection>(), out value),
                        Times.Never());

                    mockTransactionHandler.Verify(
                        h => h.PutItem(
                            It.IsAny<DbTransaction>(),
                            It.IsAny<string>(),
                            It.IsAny<object>(),
                            It.IsAny<IEnumerable<string>>(),
                            It.IsAny<TimeSpan>(),
                            It.IsAny<DateTimeOffset>(),
                            It.IsAny<DbConnection>()),
                        Times.Never);
                }
            }

            [Fact]
            public async Task Results_not_cached_if_results_non_cacheable_per_caching_policy_Async()
            {
                var reader = new Mock<DbDataReader>().Object;
                var mockCommand = CreateMockCommand(reader: reader);

                var mockTransactionHandler = new Mock<CacheTransactionHandler>(Mock.Of<ICache>());

                var cachingCommand = new CachingCommand(
                    mockCommand.Object,
                    new CommandTreeFacts(
                        TestUtils.CreateEntitySets("ES1", "ES2"), isQuery: true, usesNonDeterministicFunctions: false),
                    mockTransactionHandler.Object,
                    Mock.Of<CachingPolicy>());

                using (var r = await cachingCommand.ExecuteReaderAsync())
                {
                    Assert.Same(reader, r);

                    object value;

                    mockTransactionHandler.Verify(
                        c => c.GetItem(It.IsAny<DbTransaction>(), It.IsAny<string>(), It.IsAny<DbConnection>(), out value),
                        Times.Never());

                    mockTransactionHandler.Verify(
                        h => h.PutItem(
                            It.IsAny<DbTransaction>(),
                            It.IsAny<string>(),
                            It.IsAny<object>(),
                            It.IsAny<IEnumerable<string>>(),
                            It.IsAny<TimeSpan>(),
                            It.IsAny<DateTimeOffset>(),
                            It.IsAny<DbConnection>()),
                        Times.Never);
                }
            }

            [Fact]
            public async Task Results_not_cached_if_too_many_or_to_few_rows_Async()
            {
                var cacheableRowLimits = new[]
                {
                    new {MinCacheableRows = 0, MaxCacheableRows = 4},
                    new {MinCacheableRows = 6, MaxCacheableRows = 100}
                };

                foreach (var cachableRowLimit in cacheableRowLimits)
                {
                    var mockCommand = CreateMockCommand(reader: CreateMockReader(5).Object);

                    var mockTransactionHandler = new Mock<CacheTransactionHandler>(Mock.Of<ICache>());

                    var minCacheableRows = cachableRowLimit.MinCacheableRows;
                    var maxCacheableRows = cachableRowLimit.MaxCacheableRows;

                    var mockCachingPolicy = new Mock<CachingPolicy>();
                    mockCachingPolicy
                        .Setup(p => p.GetCacheableRows(
                            It.IsAny<ReadOnlyCollection<EntitySetBase>>(),
                            out minCacheableRows, out maxCacheableRows));
                    mockCachingPolicy
                        .Setup(p => p.CanBeCached(It.IsAny<ReadOnlyCollection<EntitySetBase>>(), It.IsAny<string>(),
                            It.IsAny<IEnumerable<KeyValuePair<string, object>>>()))
                        .Returns(true);

                    var cachingCommand = new CachingCommand(
                        mockCommand.Object,
                        new CommandTreeFacts(
                            TestUtils.CreateEntitySets("ES1", "ES2"), isQuery: true, usesNonDeterministicFunctions: false),
                        mockTransactionHandler.Object,
                        mockCachingPolicy.Object);

                    await cachingCommand.ExecuteReaderAsync();

                    mockTransactionHandler.Verify(
                        h => h.PutItem(
                            It.IsAny<DbTransaction>(),
                            It.IsAny<string>(),
                            It.IsAny<object>(),
                            It.IsAny<IEnumerable<string>>(),
                            It.IsAny<TimeSpan>(),
                            It.IsAny<DateTimeOffset>(),
                            It.IsAny<DbConnection>()),
                        Times.Never);
                }
            }

            [Fact]
            public async Task ExecuteReader_on_wrapped_command_not_invoked_for_cached_results_Async()
            {
                var mockCommand = CreateMockCommand();

                object value = new CachedResults(new ColumnMetadata[0], new List<object[]>(), 42);

                var mockTransactionHandler = new Mock<CacheTransactionHandler>(Mock.Of<ICache>());
                mockTransactionHandler
                    .Setup(h => h.GetItem(It.IsAny<DbTransaction>(), It.IsAny<string>(), It.IsNotNull<DbConnection>(), out value))
                    .Returns(true);

                var cachingCommand = new CachingCommand(
                    mockCommand.Object,
                    new CommandTreeFacts(new List<EntitySetBase>().AsReadOnly(), true, false),
                    mockTransactionHandler.Object,
                    new CachingPolicy());

                using (var reader = await cachingCommand.ExecuteReaderAsync(CommandBehavior.SequentialAccess))
                {
                    Assert.Equal(42, reader.RecordsAffected);
                }

                mockCommand
                    .Protected()
                    .Verify<Task<DbDataReader>>(
                        "ExecuteDbDataReaderAsync", Times.Never(), ItExpr.IsAny<CommandBehavior>(),
                        ItExpr.IsAny<CancellationToken>());
            }
        }

        [Fact]
        public void CanBeCached_invoked_with_correct_parameters()
        {
            var affectedEntitySets = TestUtils.CreateEntitySets("ES1", "ES2");

            var mockCommand = new Mock<DbCommand>();
            mockCommand.Setup(c => c.ExecuteScalar()).Returns(new object());
            mockCommand.Setup(c => c.CommandText).Returns("SELECT FROM");
            mockCommand.Protected().Setup<DbParameterCollection>("DbParameterCollection")
                .Returns(CreateParameterCollection(new[] { "P1", "P2" }, new object[] { "ZZZ", 123 }));

            var mockTransactionHandler = new Mock<CacheTransactionHandler>(Mock.Of<ICache>());
            var mockCachingPolicy = new Mock<CachingPolicy>();

            new CachingCommand(
                mockCommand.Object,
                new CommandTreeFacts(affectedEntitySets, true, false),
                mockTransactionHandler.Object,
                mockCachingPolicy.Object).ExecuteScalar();

            mockCachingPolicy.Verify(m => m.CanBeCached(affectedEntitySets, "SELECT FROM",
                It.Is<IEnumerable<KeyValuePair<string, object>>>(
                    p => p.SequenceEqual(new Dictionary<string, object> { { "P1", "ZZZ" }, { "P2", 123 } }))), 
                    Times.Once);
        }

        [Fact]
        public void CachingCommand_can_be_cloned()
        {
            var mockCommand = new Mock<DbCommand>();
            var clonedWrappedCommand = Mock.Of<DbCommand>();
            mockCommand.As<ICloneable>().Setup(c => c.Clone()).Returns(clonedWrappedCommand);

            var command = mockCommand.Object;
            var commandTreeFacts = new CommandTreeFacts(null, true, true);
            var transactionHandler = new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object;
            var cachingPolicy = Mock.Of<CachingPolicy>();

            var cachingCommand = new CachingCommand(command, commandTreeFacts, transactionHandler, cachingPolicy);

            var clonedCommand = Assert.IsType<CachingCommand>(cachingCommand.Clone());
            Assert.Same(clonedWrappedCommand, clonedCommand.WrappedCommand);
            Assert.Same(cachingCommand.CommandTreeFacts, clonedCommand.CommandTreeFacts);
            Assert.Same(cachingCommand.CacheTransactionHandler, clonedCommand.CacheTransactionHandler);
            Assert.Same(cachingCommand.CachingPolicy, clonedCommand.CachingPolicy);
        }

        [Fact]
        public void CachingCommand_throws_if_underlying_command_cannot_be_cloned()
        {
            var command = Mock.Of<DbCommand>();
            var commandTreeFacts = new CommandTreeFacts(null, true, true);
            var transactionHandler = new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object;
            var cachingPolicy = Mock.Of<CachingPolicy>();

            var cachingCommand = new CachingCommand(command, commandTreeFacts, transactionHandler, cachingPolicy);

            var exception = Assert.Throws<InvalidOperationException>(() => cachingCommand.Clone());
            Assert.Equal("The underlying DbCommand does not implement the ICloneable interface.", exception.Message);
        }
    }
}

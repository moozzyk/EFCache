// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace EFCache
{
    using Moq;
    using Moq.Protected;
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Data.Entity.Infrastructure.Interception;
    using Xunit;

    public class CacheTransactionHandlerTests
    {
        [Fact]
        public void CacheTransactionHandler_cannot_be_initialized_with_null_cache()
        {
            Assert.Equal(
                "cache",
                Assert.Throws<ArgumentNullException>(() => new CacheTransactionHandler(cache:null)).ParamName);
        }

        [Fact]
        public void GetItem_calls_GetItem_on_cache_if_transaction_is_null()
        {
            object value;

            var mockCache = new Mock<ICache>();
            mockCache.Setup(c => c.GetItem(It.IsAny<string>(), out value)).Returns(true);

            Assert.True(
                new CacheTransactionHandler(mockCache.Object).GetItem(null, "key", Mock.Of<DbConnection>(), out value));

            mockCache.Verify(c => c.GetItem("key", out value), Times.Once());
        }

        [Fact]
        public void GetItem_does_not_calls_GetItem_on_cache_if_transaction_is_not_null()
        {
            object value;

            var mockCache = new Mock<ICache>();
            mockCache.Setup(c => c.GetItem(It.IsAny<string>(), out value)).Returns(true);

            Assert.False(
                new CacheTransactionHandler(mockCache.Object)
                    .GetItem(Mock.Of<DbTransaction>(), "key", Mock.Of<DbConnection>(), out value));

            mockCache.Verify(c => c.GetItem(It.IsAny<string>(), out value), Times.Never());
        }

        [Fact]
        public void PutItem_calls_PutItem_on_cache_if_transaction_is_null()
        {
            var mockCache = new Mock<ICache>();

            var key = "key";
            var value = new object();
            var sets = new string[0];
            var timeSpan = new TimeSpan(1500);
            var dateTime = new DateTime(1499);

            new CacheTransactionHandler(mockCache.Object)
                .PutItem(null, key, value, sets, timeSpan, dateTime, Mock.Of<DbConnection>());

            mockCache.Verify(c => c.PutItem(key, value, sets, timeSpan, dateTime), Times.Once());
        }

        [Fact]
        public void PutItem_doe_not_call_PutItem_on_cache_if_transaction_is_not_null()
        {
            var mockCache = new Mock<ICache>();

            new CacheTransactionHandler(mockCache.Object)
                .PutItem(Mock.Of<DbTransaction>(), "key", new object(), new string[0], new TimeSpan(), new DateTime(), Mock.Of<DbConnection>());

            mockCache.Verify(
                c => c.PutItem(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IEnumerable<string>>(),
                    It.IsAny<TimeSpan>(), It.IsAny<DateTime>()), Times.Never());
        }

        [Fact]
        public void InvalidateSets_calls_InvalidateSets_on_cache_if_transaction_null()
        {
            var mockCache = new Mock<ICache>();

            var sets = new string[0];

            new CacheTransactionHandler(mockCache.Object)
                .InvalidateSets(null, sets, Mock.Of<DbConnection>());

            mockCache.Verify(c => c.InvalidateSets(sets), Times.Once());
        }

        [Fact]
        public void Committed_invalidate_sets_collected_during_transaction()
        {
            var mockCache = new Mock<ICache>();
            var transactionHandler = new CacheTransactionHandler(mockCache.Object);

            var transaction = Mock.Of<DbTransaction>();
            transactionHandler.InvalidateSets(transaction, new[] {"ES1", "ES2"}, Mock.Of<DbConnection>());
            transactionHandler.InvalidateSets(transaction, new[] {"ES3", "ES2"}, Mock.Of<DbConnection>());

            transactionHandler.Committed(transaction, Mock.Of<DbTransactionInterceptionContext>());

            mockCache.Verify(c =>c.InvalidateSets(new[] {"ES1", "ES2", "ES3"}), Times.Once());
        }

        [Fact]
        public void RolledBack_clears_affected_sets_collected_during_transaction()
        {
            var mockCache = new Mock<ICache>();
            var transactionHandler = new CacheTransactionHandler(mockCache.Object);

            var transaction = Mock.Of<DbTransaction>();
            transactionHandler.InvalidateSets(transaction, new[] { "ES1", "ES2" }, Mock.Of<DbConnection>());
            transactionHandler.InvalidateSets(transaction, new[] { "ES3", "ES2" }, Mock.Of<DbConnection>());

            transactionHandler.RolledBack(transaction, Mock.Of<DbTransactionInterceptionContext>());
            transactionHandler.Committed(transaction, Mock.Of<DbTransactionInterceptionContext>());

            mockCache.Verify(c => c.InvalidateSets(It.IsAny<IEnumerable<string>>()), Times.Never());
        }

        [Fact]
        public void ResolveCache_throws_for_uninitialized_cache()
        {
            var transactionHandler = new Mock<CacheTransactionHandler>{ CallBase = true }.Object;

            Assert.ThrowsAny<InvalidOperationException>(() =>
                transactionHandler.GetItem(null, "key", Mock.Of<DbConnection>(), out object _));
        }

        [Fact]
        public void GetItem_resolves_cache()
        {
            var mockTransactionHandler = new Mock<CacheTransactionHandler> { CallBase = true };
            mockTransactionHandler.Protected()
                .Setup<ICache>("ResolveCache", ItExpr.IsAny<DbConnection>())
                .Returns(Mock.Of<ICache>());
            var dbConnection = Mock.Of<DbConnection>();

            mockTransactionHandler.Object.GetItem(null, "key", dbConnection, out object _);

            mockTransactionHandler.Protected()
                .Verify("ResolveCache", Times.Once(), dbConnection);
        }

        [Fact]
        public void PutItem_resolves_cache()
        {
            var mockTransactionHandler = new Mock<CacheTransactionHandler> { CallBase = true };
            mockTransactionHandler.Protected()
                .Setup<ICache>("ResolveCache", ItExpr.IsAny<DbConnection>())
                .Returns(Mock.Of<ICache>());
            var dbConnection = Mock.Of<DbConnection>();

            mockTransactionHandler.Object.PutItem(null, "key", new object(), new string[0], TimeSpan.MaxValue,
                DateTimeOffset.MaxValue, dbConnection);

            mockTransactionHandler.Protected()
                .Verify("ResolveCache", Times.Once(), dbConnection);
        }

        [Fact]
        public void InvalidateSets_resolves_cache()
        {
            var mockTransactionHandler = new Mock<CacheTransactionHandler> { CallBase = true };
            mockTransactionHandler.Protected()
                .Setup<ICache>("ResolveCache", ItExpr.IsAny<DbConnection>())
                .Returns(Mock.Of<ICache>());
            var dbConnection = Mock.Of<DbConnection>();

            mockTransactionHandler.Object.InvalidateSets(null, new string[0], dbConnection);

            mockTransactionHandler.Protected()
                .Verify("ResolveCache", Times.Once(), dbConnection);
        }

        [Fact]
        public void Committed_resolves_cache()
        {
            var mockTransactionHandler = new Mock<CacheTransactionHandler> { CallBase = true };
            mockTransactionHandler.Protected()
                .Setup<ICache>("ResolveCache", ItExpr.IsAny<DbConnection>())
                .Returns(Mock.Of<ICache>());
            var dbConnection = Mock.Of<DbConnection>();
            var interceptionContext = new DbTransactionInterceptionContext().WithConnection(dbConnection);
            var mockTransaction = new Mock<DbTransaction>();
            mockTransaction.Protected().SetupGet<DbConnection>("DbConnection").Returns(dbConnection);
            var entitySets = new[] { "ES1" };

            mockTransactionHandler.Object.InvalidateSets(mockTransaction.Object, entitySets, dbConnection);
            mockTransaction.Protected().SetupGet<DbConnection>("DbConnection").Returns((DbConnection)null);
            mockTransactionHandler.Object.Committed(mockTransaction.Object, interceptionContext);

            mockTransactionHandler.Protected()
                .Verify("ResolveCache", Times.Once(), dbConnection);
        }
    }
}

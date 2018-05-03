// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace EFCache
{
    using Moq;
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
                Assert.Throws<ArgumentNullException>(() => new CacheTransactionHandler(null)).ParamName);
        }

        [Fact]
        public void GetItem_calls_GetItem_on_cache_if_transaction_is_null()
        {
            object value;

            var mockCache = new Mock<ICache>();
            mockCache.Setup(c => c.GetItem(It.IsAny<string>(), out value,
					It.IsAny<DbConnection>())).Returns(true);

            Assert.True(
                new CacheTransactionHandler(mockCache.Object).GetItem(null, "key", out value));

            mockCache.Verify(c => c.GetItem("key", out value,
				It.IsAny<DbConnection>()), Times.Once());
        }

        [Fact]
        public void GetItem_does_not_calls_GetItem_on_cache_if_transaction_is_not_null()
        {
            object value;

            var mockCache = new Mock<ICache>();
            mockCache.Setup(c => c.GetItem(It.IsAny<string>(), out value,
				It.IsAny<DbConnection>())).Returns(true);

            Assert.False(
                new CacheTransactionHandler(mockCache.Object).GetItem(Mock.Of<DbTransaction>(), "key", out value));

            mockCache.Verify(c => c.GetItem(It.IsAny<string>(), out value,
				It.IsAny<DbConnection>()), Times.Never());
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
                .PutItem(null, key, value, sets, timeSpan, dateTime);

            mockCache.Verify(c => c.PutItem(key, value, sets, timeSpan, dateTime,
				It.IsAny<DbConnection>()), Times.Once());
        }

        [Fact]
        public void PutItem_doe_not_call_PutItem_on_cache_if_transaction_is_not_null()
        {
            var mockCache = new Mock<ICache>();

            new CacheTransactionHandler(mockCache.Object)
                .PutItem(Mock.Of<DbTransaction>(), "key", new object(), new string[0], new TimeSpan(), new DateTime());

            mockCache.Verify(
                c => c.PutItem(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IEnumerable<string>>(),
                    It.IsAny<TimeSpan>(), It.IsAny<DateTime>(),
					It.IsAny<DbConnection>()), Times.Never());
        }

        [Fact]
        public void InvalidateSets_calls_InvalidateSets_on_cache_if_transaction_null()
        {
            var mockCache = new Mock<ICache>();

            var sets = new string[0];

            new CacheTransactionHandler(mockCache.Object)
                .InvalidateSets(null, sets);

            mockCache.Verify(c => c.InvalidateSets(sets,
				It.IsAny<DbConnection>()), Times.Once());
        }

        [Fact]
        public void Committed_invalidate_sets_collected_during_transaction()
        {
            var mockCache = new Mock<ICache>();
            var transactionHandler = new CacheTransactionHandler(mockCache.Object);

            var transaction = Mock.Of<DbTransaction>();
            transactionHandler.InvalidateSets(transaction, new[] {"ES1", "ES2"});
            transactionHandler.InvalidateSets(transaction, new[] {"ES3", "ES2"});

            transactionHandler.Committed(transaction, Mock.Of<DbTransactionInterceptionContext>());

            mockCache.Verify(c =>c.InvalidateSets(new[] {"ES1", "ES2", "ES3"},
				It.IsAny<DbConnection>()), Times.Once());
        }

        [Fact]
        public void RolledBack_clears_affected_sets_collected_during_transaction()
        {
            var mockCache = new Mock<ICache>();
            var transactionHandler = new CacheTransactionHandler(mockCache.Object);

            var transaction = Mock.Of<DbTransaction>();
            transactionHandler.InvalidateSets(transaction, new[] { "ES1", "ES2" });
            transactionHandler.InvalidateSets(transaction, new[] { "ES3", "ES2" });

            transactionHandler.RolledBack(transaction, Mock.Of<DbTransactionInterceptionContext>());
            transactionHandler.Committed(transaction, Mock.Of<DbTransactionInterceptionContext>());

            mockCache.Verify(c => c.InvalidateSets(It.IsAny<IEnumerable<string>>(),
				It.IsAny<DbConnection>()), Times.Never());
        }

    }
}

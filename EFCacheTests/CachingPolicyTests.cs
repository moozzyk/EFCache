// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

using System;

namespace EFCache
{
    using System.Data.Entity.Core.Metadata.Edm;
    using Moq;
    using Xunit;
    using System.Collections.Generic;

    public class CachingPolicyTests
    {
        [Fact]
        public void CanBeCached_returns_true_for_non_blacklisted_queries()
        {
            var cachingPolicy = new Mock<CachingPolicy> { CallBase = true }.Object;
            Assert.True(cachingPolicy.CanBeCached(new List<EntitySetBase>().AsReadOnly(), "A", null));
        }

        [Fact]
        public void CanBeCached_returns_false_for_blacklisted_queries()
        {
            var cachingPolicy = new Mock<CachingPolicy> { CallBase = true }.Object;
            cachingPolicy.AddBlacklistedQuery("A");
            Assert.False(cachingPolicy.CanBeCached(new List<EntitySetBase>().AsReadOnly(), "A", null));
        }

        [Fact]
        public void CanBeCached_returns_true_for_if_query_removed_from_blacklisted_queries()
        {
            var cachingPolicy = new Mock<CachingPolicy> { CallBase = true }.Object;
            cachingPolicy.AddBlacklistedQuery("A");
            Assert.True(cachingPolicy.RemoveBlacklistedQuery("A"));
            Assert.True(cachingPolicy.CanBeCached(new List<EntitySetBase>().AsReadOnly(), "A", null));
        }

        [Fact]
        public void RemoveBlacklistedQuery_returns_false_if_query_was_not_blacklisted()
        {
           Assert.False(new Mock<CachingPolicy> { CallBase = true }.Object.RemoveBlacklistedQuery("A"));
        }

        [Fact]
        public void AddBlacklistedQuery_throws_for_null_argument()
        {
            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => new Mock<CachingPolicy> {CallBase = true}.Object.AddBlacklistedQuery(null)).ParamName);

            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => new Mock<CachingPolicy> { CallBase = true }.Object.AddBlacklistedQuery("")).ParamName);
        }

        [Fact]
        public void RemoveBlacklistedQuery_throws_for_null_argument()
        {
            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => new Mock<CachingPolicy> { CallBase = true }.Object.RemoveBlacklistedQuery(null)).ParamName);

            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => new Mock<CachingPolicy> { CallBase = true }.Object.RemoveBlacklistedQuery("")).ParamName);
        }
    }
}

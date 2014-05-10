// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

using System;

namespace EFCache
{
    using System.Data.Entity.Core.Metadata.Edm;
    using Xunit;
    using System.Collections.Generic;

    public class CachingPolicyTests
    {
        [Fact]
        public void CanBeCached_returns_true_for_non_blacklisted_queries()
        {
            Assert.True(new CachingPolicy().CanBeCached(new List<EntitySetBase>().AsReadOnly(), "A", null));
        }

        [Fact]
        public void CanBeCached_returns_false_for_blacklisted_queries()
        {
            var cachingPolicy = new CachingPolicy();
            cachingPolicy.AddBlacklistedQuery("A");
            Assert.False(cachingPolicy.CanBeCached(new List<EntitySetBase>().AsReadOnly(), "A", null));
        }

        [Fact]
        public void CanBeCached_returns_true_for_if_query_removed_from_blacklisted_queries()
        {
            var cachingPolicy = new CachingPolicy();
            cachingPolicy.AddBlacklistedQuery("A");
            Assert.True(cachingPolicy.RemoveBlacklistedQuery("A"));
            Assert.True(cachingPolicy.CanBeCached(new List<EntitySetBase>().AsReadOnly(), "A", null));
        }

        [Fact]
        public void RemoveBlacklistedQuery_returns_false_if_query_was_not_blacklisted()
        {
           Assert.False(new CachingPolicy().RemoveBlacklistedQuery("A"));
        }

        [Fact]
        public void AddBlacklistedQuery_throws_for_null_argument()
        {
            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => new CachingPolicy().AddBlacklistedQuery(null)).ParamName);

            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => new CachingPolicy().AddBlacklistedQuery("")).ParamName);
        }

        [Fact]
        public void RemoveBlacklistedQuery_throws_for_null_argument()
        {
            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => new CachingPolicy().RemoveBlacklistedQuery(null)).ParamName);

            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => new CachingPolicy().RemoveBlacklistedQuery("")).ParamName);
        }

        [Fact]
        public void GetCacheableRows_returns_full_range()
        {
            int minRows = int.MinValue,
                maxRows = int.MinValue;

            new CachingPolicy().GetCacheableRows(new List<EntitySetBase>().AsReadOnly(), out minRows, out maxRows);

            Assert.Equal(0, minRows);
            Assert.Equal(int.MaxValue, maxRows);
        }

        [Fact]
        public void GetExpirationTimeout_returns_max_expiration_timoeut()
        {
            var slidingExpiration = TimeSpan.MinValue;
            var absoluteExpiration = DateTimeOffset.MinValue;

            new CachingPolicy().GetExpirationTimeout(
                new List<EntitySetBase>().AsReadOnly(), out slidingExpiration, out absoluteExpiration);

            Assert.Equal(TimeSpan.MaxValue, slidingExpiration);
            Assert.Equal(DateTimeOffset.MaxValue, absoluteExpiration);
        }
    }
}

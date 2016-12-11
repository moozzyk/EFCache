// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using Moq;

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
        public void CanBeCached_returns_true_if_all_affected_entity_sets_should_be_cached()
        {
            // table takes precedence override name
            var entitySets = TestUtils.CreateEntitySetsEx(new[] { "tbl1", "t" }, new[] { null, "aaa" });

            Assert.True(new CachingPolicy(new[] { "t", "aaa", "r", "tbl1" }).CanBeCached(entitySets, "A", null));
        }

        [Fact]
        public void CanBeCached_returns_false_if_some_affected_entity_sets_should_not_be_cached()
        {
            // table takes precedence override name
            var entitySets = TestUtils.CreateEntitySetsEx(new[] { "tbl1", "t" }, new[] { null, "aaa"});

            Assert.False(new CachingPolicy(new[] { "t", "r", "tbl1" }).CanBeCached(entitySets, "A", null));
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

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

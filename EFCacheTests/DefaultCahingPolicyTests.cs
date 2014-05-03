// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace EFCache
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity.Core.Metadata.Edm;
    using Xunit;

    public class DefaultCahingPolicyTests
    {
        [Fact]
        public void CanBeCached_returns_true()
        {
            Assert.True(new DefaultCachingPolicy().CanBeCached(new List<EntitySetBase>().AsReadOnly(), "sql", new Dictionary<string, object>()));
        }

        [Fact]
        public void GetCacheableRows_returns_full_range()
        {
            int minRows = int.MinValue,
                maxRows = int.MinValue;

            new DefaultCachingPolicy().GetCacheableRows(new List<EntitySetBase>().AsReadOnly(), out minRows, out maxRows);

            Assert.Equal(0, minRows);
            Assert.Equal(int.MaxValue, maxRows);
        }

        public void GetExpirationTimeout_returns_max_expiration_timoeut()
        {
            var slidingExpiration = TimeSpan.MinValue;
            var absoluteExpiration = DateTimeOffset.MinValue;

            new DefaultCachingPolicy().GetExpirationTimeout(new List<EntitySetBase>().AsReadOnly(), out slidingExpiration, out absoluteExpiration);

            Assert.Equal(TimeSpan.MaxValue, slidingExpiration);
            Assert.Equal(DateTime.MaxValue, absoluteExpiration);
        }
    }
}

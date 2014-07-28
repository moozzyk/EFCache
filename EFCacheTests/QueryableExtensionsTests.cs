// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace EFCache
{
    using System;
    using System.Linq;
    using Xunit;

    public class QueryableExtensionsTests
    {
        [Fact]
        public void NotCached_validates_parameters()
        {
            Assert.Equal(
                "source",
                Assert.Throws<ArgumentNullException>(
                    () => ((IQueryable<object>) null).NotCached()).ParamName);
        }

        [Fact]
        public void Cached_validates_parameters()
        {
            Assert.Equal(
                "source",
                Assert.Throws<ArgumentNullException>(
                    () => ((IQueryable<object>)null).Cached()).ParamName);
        }
    }
}

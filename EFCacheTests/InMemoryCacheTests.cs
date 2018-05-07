// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace EFCache
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;

    public class InMemoryCacheTests
    {
        [Fact]
        public void Item_cached()
        {
            var cache = new InMemoryCache();
            var item = new object();

            cache.PutItem("key", item, new string[0], TimeSpan.MaxValue, DateTimeOffset.MaxValue);

            object fromCache;
            Assert.True(cache.GetItem("key", out fromCache));
            Assert.Same(item, fromCache);

            Assert.True(cache.GetItem("key", out fromCache));
            Assert.Same(item, fromCache);
        }

        [Fact]
        public void Item_not_returned_after_absolute_expiration_expired()
        {
            var cache = new InMemoryCache();
            var item = new object();

            cache.PutItem("key", item, new string[0], TimeSpan.MaxValue, DateTimeOffset.Now.AddMinutes(-10));

            object fromCache;
            Assert.False(cache.GetItem("key", out fromCache));
            Assert.Null(fromCache);
        }

        [Fact]
        public void Item_not_returned_after_sliding_expiration_expired()
        {
            var cache = new InMemoryCache();
            var item = new object();

            cache.PutItem("key", item, new string[0], TimeSpan.Zero.Subtract(new TimeSpan(10000)), DateTimeOffset.MaxValue);

            object fromCache;
            Assert.False(cache.GetItem("key", out fromCache));
            Assert.Null(fromCache);
        }

        [Fact]
        public void InvalidateSets_invalidate_items_with_given_sets()
        {
            var cache = new InMemoryCache();

            cache.PutItem("1", new object(), new[] { "ES1", "ES2" }, TimeSpan.MaxValue, DateTimeOffset.MaxValue);
            cache.PutItem("2", new object(), new[] { "ES2", "ES3" }, TimeSpan.MaxValue, DateTimeOffset.MaxValue);
            cache.PutItem("3", new object(), new[] { "ES1", "ES3", "ES4" }, TimeSpan.MaxValue, DateTimeOffset.MaxValue);
            cache.PutItem("4", new object(), new[] { "ES3", "ES4" }, TimeSpan.MaxValue, DateTimeOffset.MaxValue);

            cache.InvalidateSets(new[] { "ES1", "ES2" });

            object item;
            Assert.False(cache.GetItem("1", out item));
            Assert.False(cache.GetItem("2", out item));
            Assert.False(cache.GetItem("3", out item));
            Assert.True(cache.GetItem("4", out item));
        }

        [Fact]
        public void InvalidateItem_invalidates_item()
        {
            var cache = new InMemoryCache();

            cache.PutItem("1", new object(), new[] { "ES1", "ES2" }, TimeSpan.MaxValue, DateTimeOffset.MaxValue);
            cache.InvalidateItem("1");

            object item;
            Assert.False(cache.GetItem("1", out item));
        }

        [Fact]
        public void Count_returns_numers_of_cached_entries()
        {
            var cache = new InMemoryCache();

            Assert.Equal(0, cache.Count);

            cache.PutItem("1", new object(), new[] { "ES1", "ES2" }, TimeSpan.MaxValue, DateTimeOffset.MaxValue);

            Assert.Equal(1, cache.Count);

            cache.InvalidateItem("1");

            Assert.Equal(0, cache.Count);
        }

        [Fact]
        public void Purge_removes_stale_items_from_cache()
        {
            var cache = new InMemoryCache();

            cache.PutItem("1", new object(), new[] { "ES1", "ES2" }, TimeSpan.MaxValue, DateTimeOffset.Now.AddMinutes(-1));
            cache.PutItem("2", new object(), new[] { "ES1", "ES2" }, TimeSpan.MaxValue, DateTimeOffset.MaxValue);

            Assert.Equal(2, cache.Count);

            cache.Purge();

            Assert.Equal(1, cache.Count);

            object item;
            Assert.False(cache.GetItem("1", out item));
            Assert.True(cache.GetItem("2", out item));
        }

        [Fact]
        public void Purge_removing_unexpired_items_removes_all_items_from_cache()
        {
            var cache = new InMemoryCache();

            cache.PutItem("1", new object(), new[] { "ES1", "ES2" }, TimeSpan.MaxValue, DateTimeOffset.Now.AddMinutes(-1));
            cache.PutItem("2", new object(), new[] { "ES1", "ES2" }, TimeSpan.MaxValue, DateTimeOffset.MaxValue);

            Assert.Equal(2, cache.Count);

            cache.Purge(true);

            Assert.Equal(0, cache.Count);
        }

        [Fact]
        public void GetItem_validates_parametes()
        {
            object item;

            Assert.Equal(
                "key",
                Assert.Throws<ArgumentNullException>(() => new InMemoryCache().GetItem(null, out item)).ParamName);
        }

        [Fact]
        public void PutItem_validates_parametes()
        {
            Assert.Equal(
                "key",
                Assert.Throws<ArgumentNullException>(()
                    => new InMemoryCache().PutItem(null, 42, new string[0], TimeSpan.Zero, DateTimeOffset.Now))
                    .ParamName);

            Assert.Equal(
                "dependentEntitySets",
                Assert.Throws<ArgumentNullException>(()
                    => new InMemoryCache().PutItem("1", 42, null, TimeSpan.Zero, DateTimeOffset.Now)).ParamName);
        }

        [Fact]
        public void InvalidateSets_validates_parametes()
        {
            Assert.Equal(
                "entitySets",
                Assert.Throws<ArgumentNullException>(() => new InMemoryCache().InvalidateSets(null)).ParamName);
        }

        [Fact]
        public void InvalidateItem_validates_parametes()
        {
            Assert.Equal(
                "key",
                Assert.Throws<ArgumentNullException>(() => new InMemoryCache().InvalidateItem(null)).ParamName);
        }

        [Fact]
        public void GetEntitySets_returns_all_cached_entitysets()
        {
            var cache = new InMemoryCache();
            var item = new object();

            var entitySets = new List<string> { "table1", "table2", "table3" };

            cache.PutItem("key", item, entitySets, TimeSpan.MaxValue, DateTimeOffset.MaxValue);

            var inCache = cache.EntitySetsInCache.ToList();

            Assert.Equal(entitySets, inCache);
        }

        [Fact]
        public void GetCacheSize_returns_current_size_of_object()
        {
            var cache = new InMemoryCache();
            var item = new object();

            var entitySets = new List<string> { "table1", "table2", "table3" };

            cache.PutItem("key", item, entitySets, TimeSpan.MaxValue, DateTimeOffset.MaxValue);

            var size = cache.GetCacheSize();

            Assert.True(size > 0);
        }
    }
}
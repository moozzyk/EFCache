// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace EFCache
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Infrastructure;
    using System.Linq;
    using System.Transactions;
    using Xunit;

    public class Entity
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public bool? Flag { get; set; }
    }

    public class Item
    {
        // using client generated key ensures inserting new entities with 
        // ExecuteNonQuery(Async) instead of ExecuteDbDataReader(Async) 
        public Guid Id { get; set; }
    }

    public class MyContext : DbContext
    {
        static MyContext()
        {
            Database.SetInitializer(new DropCreateDatabaseIfModelChanges<MyContext>());
        }

        public DbSet<Entity> Entities { get; set; }

        public DbSet<Item> Items { get; set; }
    }

    public class Configuration : DbConfiguration
    {
        public Configuration()
        {
            var transactionHandler = new CacheTransactionHandler(E2ETest.Cache);
            
            AddInterceptor(transactionHandler);

            Loaded +=
                (sender, args) => args.ReplaceService<DbProviderServices>(
                    (s, _) => new CachingProviderServices(s, transactionHandler));
        }
    }

    public class Cache : ICache
    {
        public readonly Dictionary<string, object> CacheDictionary = 
            new Dictionary<string, object>();

        private readonly Dictionary<string, List<string>> _setToCacheKey 
            = new Dictionary<string, List<string>>();

        public bool GetItem(string key, out object value)
        {
            return CacheDictionary.TryGetValue(key, out value);
        }

        public void PutItem(string key, object value, IEnumerable<string> dependentEntitySets, TimeSpan slidingExpiration, DateTimeOffset absoluteExpiration)
        {
            CacheDictionary[key] = value;

            foreach (var set in dependentEntitySets)
            {
                List<string> keys;
                if (!_setToCacheKey.TryGetValue(set, out keys))
                {
                    keys = new List<string>();
                    _setToCacheKey[set] = keys;
                }

                keys.Add(key);
            }
        }

        public void InvalidateSets(IEnumerable<string> entitySets)
        {
            foreach (var set in entitySets)
            {
                List<string> keys;
                if (_setToCacheKey.TryGetValue(set, out keys))
                {
                    foreach (var key in keys)
                    {
                        CacheDictionary.Remove(key);
                    }

                    _setToCacheKey.Remove(set);
                }
            }
        }

        public void InvalidateItem(string key)
        {
            throw new NotImplementedException();
        }
    }

    public class E2ETest : TestBase
    {
        internal static readonly Cache Cache = new Cache();

        [Fact]
        public void Cached_data_returned_from_cache()
        {
            using (var ctx = new MyContext())
            {
                var id = 3;
                var q = ctx.Entities.Where(e => e.Id == id);
                q.ToList();
                Assert.True(Cache.CacheDictionary.ContainsKey(q + "_p__linq__0=3"));
            }
        }

        [Fact]
        public void Cached_data_returned_from_cache_Async()
        {
            using (var ctx = new MyContext())
            {
                var id = 3;
                var q = ctx.Entities.Where(e => e.Id == id);
                q.ToListAsync().GetAwaiter().GetResult();

                Assert.True(Cache.CacheDictionary.ContainsKey(q + "_p__linq__0=3"));
            }
        }

        [Fact]
        public void Cache_cleared_on_implicit_transaction_commit()
        {
            Cache.PutItem("s", new object(), new[] {"Item", "Entity"}, new TimeSpan(), new DateTime());

            using (var ctx = new MyContext())
            {
                ctx.Entities.Add(new Entity());
                ctx.Items.Add(new Item { Id = Guid.NewGuid()});

                ctx.SaveChanges();

                Assert.False(Cache.CacheDictionary.ContainsKey("s"));
            }
        }

        [Fact]
        public void Cache_cleared_on_explicit_transaction_commit()
        {
            Cache.PutItem("s", new object(), new[] { "Item", "Entity" }, new TimeSpan(), new DateTime());

            using (var ctx = new MyContext())
            {
                using (var entityConnection = ((IObjectContextAdapter)ctx).ObjectContext.Connection)
                {
                    entityConnection.Open();
                    var trx = entityConnection.BeginTransaction();

                    ctx.Entities.Add(new Entity());
                    ctx.Items.Add(new Item { Id = Guid.NewGuid() });

                    ctx.SaveChanges();

                    Assert.True(Cache.CacheDictionary.ContainsKey("s"));

                    trx.Commit();

                    Assert.False(Cache.CacheDictionary.ContainsKey("s"));
                }
            }
        }

        [Fact]
        public void Cache_cleared_on_explicit_transaction_commit_Async()
        {
            Cache.PutItem("s", new object(), new[] { "Item", "Entity" }, new TimeSpan(), new DateTime());

            using (var ctx = new MyContext())
            {
                using (var entityConnection = ((System.Data.Entity.Infrastructure.IObjectContextAdapter)ctx).ObjectContext.Connection)
                {
                    entityConnection.Open();
                    var trx = entityConnection.BeginTransaction();

                    ctx.Entities.Add(new Entity());
                    ctx.Items.Add(new Item { Id = Guid.NewGuid() });

                    var x = ctx.SaveChangesAsync().GetAwaiter().GetResult();

                    Assert.True(Cache.CacheDictionary.ContainsKey("s"));

                    trx.Commit();

                    Assert.False(Cache.CacheDictionary.ContainsKey("s"));
                }
            }
        }

        [Fact]
        public void Cache_not_cleared_on_transaction_rollback()
        {
            Cache.PutItem("s", new object(), new[] { "Item", "Entity" }, new TimeSpan(), new DateTime());

            using (var ctx = new MyContext())
            {
                using(var entityConnection = ((IObjectContextAdapter)ctx).ObjectContext.Connection)
                {
                    entityConnection.Open();
                    var trx = entityConnection.BeginTransaction();

                    ctx.Entities.Add(new Entity());
                    ctx.Items.Add(new Item { Id = Guid.NewGuid() });

                    ctx.SaveChanges();

                    Assert.True(Cache.CacheDictionary.ContainsKey("s"));

                    trx.Rollback();

                    Assert.True(Cache.CacheDictionary.ContainsKey("s"));
                }                
            }
        }

        [Fact]
        public void Can_read_null_values()
        {
            using (var trx = new TransactionScope())
            {
                using (var ctx = new MyContext())
                {
                    ctx.Entities.Add(new Entity());
                    ctx.SaveChanges();
                    var e = ctx.Entities.First();
                    Assert.Null(e.Name);
                    Assert.Null(e.Flag);
                }
            }
        }

        [Fact]
        public void ObjectContext_GetCachingProviderServices_returns_correct_value()
        {
            using (var ctx = new MyContext())
            {
                Assert.NotNull(((IObjectContextAdapter) ctx).ObjectContext.GetCachingProviderServices());
            }
        }

        [Fact]
        public void DbContext_GetCachingProviderServices_returns_correct_value()
        {
            using (var ctx = new MyContext())
            {
                Assert.NotNull(ctx.GetCachingProviderServices());
            }
        }

        [Fact]
        public void DbQuery_not_cached_if_NotCached_used()
        {
            using (var ctx = new MyContext())
            {
                var q = ctx.Entities.Where(e => e.Flag != null).OrderBy(e => e.Id).NotCached();
                Assert.True(BlacklistedQueriesRegistrar.Instance.IsQueryBlacklisted(
                    ((IObjectContextAdapter)ctx).ObjectContext.MetadataWorkspace, q.ToString()));
                q.ToList();
                Assert.False(Cache.CacheDictionary.Keys.Any(k => k.StartsWith(q.ToString())));
            }
        }

        [Fact]
        public void Can_manually_add_query_to_blacklisted_queries()
        {
            using (var ctx = new MyContext())
            {
                const string query = @"SELECT 
    [GroupBy1].[A1] AS [C1]
    FROM ( SELECT 
        COUNT(1) AS [A1]
        FROM [dbo].[Entities] AS [Extent1]
        WHERE [Extent1].[Flag] IS NOT NULL
    )  AS [GroupBy1]";

                BlacklistedQueriesRegistrar.Instance.AddBlacklistedQuery(
                    ((IObjectContextAdapter)ctx).ObjectContext.MetadataWorkspace, query);

                var q = ctx.Entities.Count(e => e.Flag != null);
                Assert.False(Cache.CacheDictionary.Keys.Any(k => k.StartsWith(query.ToString())));
            }
        }
    }
}

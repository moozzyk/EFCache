// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace EFCache
{
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;
#if NET462
    using SQLite.CodeFirst;
    using System.Data.SQLite;
#endif

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

    public class EntityMappedToSprocs
    {
        public int Id { get; set; }

        public int Data { get; set; }
    }

    public class MyContext : DbContext
    {
        public MyContext()
        {
        }

        public MyContext(DbConnection connection)
            : base(connection, true)
        {
        }

        public DbSet<Entity> Entities { get; set; }

        public DbSet<Item> Items { get; set; }

        public DbSet<EntityMappedToSprocs> EntitiesMappedToSprocs { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            Database.SetInitializer(GetDatabaseInitializerStrategy(modelBuilder));

            modelBuilder.Entity<EntityMappedToSprocs>()
                .MapToStoredProcedures()
                .ToTable("EntitiesMappedToSprocs");
        }

        private IDatabaseInitializer<MyContext> GetDatabaseInitializerStrategy(DbModelBuilder modelBuilder)
        {

#if NET462
            if (this.Database.Connection is SQLiteConnection)
            {
                return new SqliteDropCreateDatabaseAlways<MyContext>(modelBuilder);
            }
#endif
            return new DropCreateDatabaseAlways<MyContext>();
        }
    }

    /*
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
    }*/

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

    public class E2ETest
    {
        internal static readonly Cache Cache = new Cache();

        static E2ETest()
        {
            EntityFrameworkCache.Initialize(E2ETest.Cache);
        }

        public E2ETest()
        {
            // reset database
            using (var ctx = new MyContext())
            {
                ctx.Database.Delete();
                ctx.Database.Create();
            }
        }

        [Fact]
        public void Cached_data_returned_from_cache()
        {
            using (var ctx = new MyContext())
            {
                var id = 3;
                var q = ctx.Entities.Where(e => e.Id == id);
                q.ToList();
                Assert.True(Cache.CacheDictionary.ContainsKey("EFCache.MyContext_" + q + "_p__linq__0=3"));
            }
        }

        [Fact]
        public async Task Cached_data_returned_from_cache_Async()
        {
            using (var ctx = new MyContext())
            {
                var id = 3;
                var q = ctx.Entities.Where(e => e.Id == id);
                await q.ToListAsync();

                Assert.True(Cache.CacheDictionary.ContainsKey("EFCache.MyContext_" + q + "_p__linq__0=3"));
            }
        }

        [Fact]
        public void Cache_cleared_on_implicit_transaction_commit()
        {
            Cache.PutItem("s", new object(), new[] { "Item", "Entity" }, new TimeSpan(), new DateTime());

            using (var ctx = new MyContext())
            {
                ctx.Entities.Add(new Entity());
                ctx.Items.Add(new Item { Id = Guid.NewGuid() });

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
        public async Task Cache_cleared_on_explicit_transaction_commit_Async()
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

                    var x = await ctx.SaveChangesAsync();

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
                using (var entityConnection = ((IObjectContextAdapter)ctx).ObjectContext.Connection)
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
            using (var ctx = new MyContext())
            {
                ctx.Entities.RemoveRange(ctx.Entities);
            }

            using (var ctx = new MyContext())
            {
                ctx.Entities.Add(new Entity());
                ctx.SaveChanges();
                var e = ctx.Entities.First();
                Assert.Null(e.Name);
                Assert.Null(e.Flag);
            }
        }

        [Fact]
        public void ObjectContext_GetCachingProviderServices_returns_correct_value()
        {
            using (var ctx = new MyContext())
            {
                Assert.NotNull(((IObjectContextAdapter)ctx).ObjectContext.GetCachingProviderServices());
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

#if NET462
        [Fact]
        public void Query_Any_using_sqlite_returns_correct_value()
        {
            var tempFile = Path.GetTempFileName();

            try
            {
                var factory = DbProviderFactories.GetFactory("System.Data.SQLite.EF6");

                using (var connection = factory.CreateConnection())
                {
                    connection.ConnectionString = $"data source={tempFile};initial catalog=EFCache.MyContext;";

                    using (var ctx = new MyContext(connection))
                    {
                        ctx.Entities.Add(new Entity() { Name = "test" });

                        ctx.SaveChanges();

                        Assert.True(ctx.Entities.Any(x => x.Name == "test"));
                        Assert.False(ctx.Entities.Any(x => x.Name == "test2"));
                    }
                }
            }
            finally
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                File.Delete(tempFile);
            }
        }
#endif

        [Fact]
        public void Query_results_not_cached_if_NotCached_used()
        {
            using (var ctx = new MyContext())
            {
                var q = ctx.Entities.Where(e => e.Flag != null).OrderBy(e => e.Id).NotCached();
                Assert.True(BlockedQueriesRegistrar.Instance.IsQueryBlocked(
                    ((IObjectContextAdapter)ctx).ObjectContext.MetadataWorkspace, q.ToString()));
                q.ToList();
                Assert.DoesNotContain(Cache.CacheDictionary.Keys, k => k.Contains(q.ToString()));
            }
        }

        [Fact]
        public void Query_results_not_cached_if_FirstNotCached_used()
        {
            using (var ctx = new MyContext())
            {
                ctx.Entities.Add(new Entity());
                ctx.SaveChanges();

                var e = ctx.Entities.FirstNotCached();
                var q = @"SELECT TOP (1)
    [c].[Id] AS [Id],
    [c].[Name] AS [Name],
    [c].[Flag] AS [Flag]
    FROM [dbo].[Entities] AS [c]";
                Assert.True(BlockedQueriesRegistrar.Instance.IsQueryBlocked(
                    ((IObjectContextAdapter)ctx).ObjectContext.MetadataWorkspace, q));

                Assert.DoesNotContain(Cache.CacheDictionary.Keys, k => k.Contains(q));
            }
        }

        [Fact]
        public void Query_results_not_cached_if_FirstOrDefaultNotCached_used()
        {
            using (var ctx = new MyContext())
            {
                var e = ctx.Entities.FirstOrDefaultNotCached();
                var q = @"SELECT TOP (1)
    [c].[Id] AS [Id],
    [c].[Name] AS [Name],
    [c].[Flag] AS [Flag]
    FROM [dbo].[Entities] AS [c]";
                Assert.True(BlockedQueriesRegistrar.Instance.IsQueryBlocked(
                    ((IObjectContextAdapter)ctx).ObjectContext.MetadataWorkspace, q));

                Assert.DoesNotContain(Cache.CacheDictionary.Keys, k => k.Contains(q));
            }
        }

        [Fact]
        public void Query_results_not_cached_if_SingleNotCached_used()
        {
            using (var ctx = new MyContext())
            {
                ctx.Entities.Add(new Entity());
                ctx.SaveChanges();

                var e = ctx.Entities.SingleNotCached();
                var q = @"SELECT TOP (2)
    [c].[Id] AS [Id],
    [c].[Name] AS [Name],
    [c].[Flag] AS [Flag]
    FROM [dbo].[Entities] AS [c]";
                Assert.True(BlockedQueriesRegistrar.Instance.IsQueryBlocked(
                    ((IObjectContextAdapter)ctx).ObjectContext.MetadataWorkspace, q));

                Assert.DoesNotContain(Cache.CacheDictionary.Keys, k => k.Contains(q));
            }
        }

        [Fact]
        public void Query_results_not_cached_if_SingleOrDefaultNotCached_used()
        {
            using (var ctx = new MyContext())
            {
                var e = ctx.Entities.SingleOrDefaultNotCached();
                var q = @"SELECT TOP (2)
    [c].[Id] AS [Id],
    [c].[Name] AS [Name],
    [c].[Flag] AS [Flag]
    FROM [dbo].[Entities] AS [c]";
                Assert.True(BlockedQueriesRegistrar.Instance.IsQueryBlocked(
                    ((IObjectContextAdapter)ctx).ObjectContext.MetadataWorkspace, q));

                Assert.DoesNotContain(Cache.CacheDictionary.Keys, k => k.Contains(q));
            }
        }

        [Fact]
        public void Can_manually_add_query_to_blocked_queries()
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

                BlockedQueriesRegistrar.Instance.AddBlockedQuery(
                    ((IObjectContextAdapter)ctx).ObjectContext.MetadataWorkspace, query);

                ctx.Entities.Count(e => e.Flag != null);
                Assert.DoesNotContain(Cache.CacheDictionary.Keys, k => k.Contains(query.ToString()));
            }
        }

        [Fact]
        public void Query_results_cached_even_if_Cached_used_on_blocked_query()
        {
            using (var ctx = new MyContext())
            {
                var q = ctx.Entities.Where(e => e.Flag == true).NotCached().Cached();
                Assert.True(AlwaysCachedQueriesRegistrar.Instance.IsQueryCached(
                    ((IObjectContextAdapter)ctx).ObjectContext.MetadataWorkspace, q.ToString()));
                q.ToList();
                Assert.Contains(Cache.CacheDictionary.Keys, k => k.Contains(q.ToString()));
            }
        }

        [Fact]
        public void Query_results_cached_if_Cached_used_on_query_with_side_effects()
        {
            using (var ctx = new MyContext())
            {
                var q = ctx.Entities.Where(e => e.Name == Guid.NewGuid().ToString()).Cached();
                Assert.True(AlwaysCachedQueriesRegistrar.Instance.IsQueryCached(
                    ((IObjectContextAdapter)ctx).ObjectContext.MetadataWorkspace, q.ToString()));
                q.ToList();
                Assert.Contains(Cache.CacheDictionary.Keys, k => k.Contains(q.ToString()));
            }
        }

        [Fact]
        public void CUD_mapped_to_sprocs_reset_cache()
        {
            const string cachedItemKey =
                "EFCache.MyContext_SELECT TOP (1) \r\n    [c].[Id] AS [Id], \r\n    [c].[Data] AS [Data]\r\n    FROM [dbo].[EntitiesMappedToSprocs] AS [c]_";

            using (var ctx = new MyContext())
            {
                ctx.Database.ExecuteSqlCommand("INSERT INTO EntitiesMappedToSprocs VALUES(42)");
                var entity = ctx.EntitiesMappedToSprocs.FirstOrDefault();
                ctx.Entry(entity).State = EntityState.Modified;

                Assert.True(Cache.CacheDictionary.ContainsKey(cachedItemKey));

                ctx.SaveChanges();

                Assert.False(Cache.CacheDictionary.ContainsKey(cachedItemKey));
            }
        }
    }
}
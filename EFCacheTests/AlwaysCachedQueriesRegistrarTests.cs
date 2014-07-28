// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace EFCache
{
    using System;
    using System.Data.Entity.Core.Metadata.Edm;
    using Xunit;

    public class AlwaysCachedQueriesRegistrarTests
    {
        [Fact]
        public void Can_add_query_to_cached_queries()
        {
            var workspace = new MetadataWorkspace();

            AlwaysCachedQueriesRegistrar.Instance.AddCachedQuery(workspace, "A");
            Assert.True(AlwaysCachedQueriesRegistrar.Instance.IsQueryCached(workspace, "A"));
        }

        [Fact]
        public void Can_remove_query_from_cached_queries()
        {
            var workspace = new MetadataWorkspace();

            AlwaysCachedQueriesRegistrar.Instance.AddCachedQuery(workspace, "A");
            Assert.True(AlwaysCachedQueriesRegistrar.Instance.RemoveCachedQuery(workspace, "A"));
            Assert.False(AlwaysCachedQueriesRegistrar.Instance.RemoveCachedQuery(workspace, "A"));
        }

        [Fact]
        public void AddCachedQuery_checks_parameters()
        {
            Assert.Equal("workspace",
                Assert.Throws<ArgumentNullException>(
                    () => AlwaysCachedQueriesRegistrar.Instance.AddCachedQuery(null, "A")).ParamName);

            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => AlwaysCachedQueriesRegistrar.Instance.AddCachedQuery(
                        new MetadataWorkspace(), null)).ParamName);

            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => AlwaysCachedQueriesRegistrar.Instance.AddCachedQuery(
                        new MetadataWorkspace(), string.Empty)).ParamName);

            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => AlwaysCachedQueriesRegistrar.Instance.AddCachedQuery(
                        new MetadataWorkspace(), " ")).ParamName);
        }

        [Fact]
        public void RemoveCachedQuery_checks_parameters()
        {
            Assert.Equal("workspace",
                Assert.Throws<ArgumentNullException>(
                    () => AlwaysCachedQueriesRegistrar.Instance.RemoveCachedQuery(null, "A")).ParamName);

            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => AlwaysCachedQueriesRegistrar.Instance.RemoveCachedQuery(
                        new MetadataWorkspace(), null)).ParamName);

            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => AlwaysCachedQueriesRegistrar.Instance.RemoveCachedQuery(
                        new MetadataWorkspace(), string.Empty)).ParamName);

            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => AlwaysCachedQueriesRegistrar.Instance.RemoveCachedQuery(
                        new MetadataWorkspace(), " ")).ParamName);
        }

        [Fact]
        public void IsQueryCached_checks_parameters()
        {
            Assert.Equal("workspace",
                Assert.Throws<ArgumentNullException>(
                    () => AlwaysCachedQueriesRegistrar.Instance.IsQueryCached(null, "A")).ParamName);

            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => AlwaysCachedQueriesRegistrar.Instance.IsQueryCached(
                        new MetadataWorkspace(), null)).ParamName);

            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => AlwaysCachedQueriesRegistrar.Instance.IsQueryCached(
                        new MetadataWorkspace(), string.Empty)).ParamName);

            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => AlwaysCachedQueriesRegistrar.Instance.IsQueryCached(
                        new MetadataWorkspace(), " ")).ParamName);
        }
    }
}

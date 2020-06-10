// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace EFCache
{
    using System;
    using System.Data.Entity.Core.Metadata.Edm;
    using Xunit;

    public class BlockedQueriesRegistrarTests
    {
        [Fact]
        public void Can_add_query_to_blocked_queries()
        {
            var workspace = new MetadataWorkspace();

            BlockedQueriesRegistrar.Instance.AddBlockedQuery(workspace, "A");
            Assert.True(BlockedQueriesRegistrar.Instance.IsQueryBlocked(workspace, "A"));
        }

        [Fact]
        public void Can_remove_query_from_blocked_queries()
        {
            var workspace = new MetadataWorkspace();

            BlockedQueriesRegistrar.Instance.AddBlockedQuery(workspace, "A");
            Assert.True(BlockedQueriesRegistrar.Instance.RemoveBlockedQuery(workspace, "A"));
            Assert.False(BlockedQueriesRegistrar.Instance.RemoveBlockedQuery(workspace, "A"));
        }

        [Fact]
        public void AddBlockedQuery_checks_parameters()
        {
            Assert.Equal("workspace",
                Assert.Throws<ArgumentNullException>(
                    () => BlockedQueriesRegistrar.Instance.AddBlockedQuery(null, "A")).ParamName);

            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => BlockedQueriesRegistrar.Instance.AddBlockedQuery(
                        new MetadataWorkspace(), null)).ParamName);

            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => BlockedQueriesRegistrar.Instance.AddBlockedQuery(
                        new MetadataWorkspace(), string.Empty)).ParamName);

            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => BlockedQueriesRegistrar.Instance.AddBlockedQuery(
                        new MetadataWorkspace(), " ")).ParamName);
        }

        [Fact]
        public void RemoveBlockedQuery_checks_parameters()
        {
            Assert.Equal("workspace",
                Assert.Throws<ArgumentNullException>(
                    () => BlockedQueriesRegistrar.Instance.RemoveBlockedQuery(null, "A")).ParamName);

            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => BlockedQueriesRegistrar.Instance.RemoveBlockedQuery(
                        new MetadataWorkspace(), null)).ParamName);

            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => BlockedQueriesRegistrar.Instance.RemoveBlockedQuery(
                        new MetadataWorkspace(), string.Empty)).ParamName);

            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => BlockedQueriesRegistrar.Instance.RemoveBlockedQuery(
                        new MetadataWorkspace(), " ")).ParamName);
        }

        [Fact]
        public void IsQueryBlocked_checks_parameters()
        {
            Assert.Equal("workspace",
                Assert.Throws<ArgumentNullException>(
                    () => BlockedQueriesRegistrar.Instance.IsQueryBlocked(null, "A")).ParamName);

            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => BlockedQueriesRegistrar.Instance.IsQueryBlocked(
                        new MetadataWorkspace(), null)).ParamName);

            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => BlockedQueriesRegistrar.Instance.IsQueryBlocked(
                        new MetadataWorkspace(), string.Empty)).ParamName);

            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => BlockedQueriesRegistrar.Instance.IsQueryBlocked(
                        new MetadataWorkspace(), " ")).ParamName);
        }
    }
}

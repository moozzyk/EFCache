// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace EFCache
{
    using System;
    using System.Data.Entity.Core.Metadata.Edm;
    using Xunit;

    public class BlacklistedQueriesRegistrarTests
    {
        [Fact]
        public void Can_add_query_to_blacklisted_queries()
        {
            var workspace = new MetadataWorkspace();

            BlacklistedQueriesRegistrar.Instance.AddBlacklistedQuery(workspace, "A");
            Assert.True(BlacklistedQueriesRegistrar.Instance.IsQueryBlacklisted(workspace, "A"));
        }

        [Fact]
        public void Can_remove_query_from_blacklisted_queries()
        {
            var workspace = new MetadataWorkspace();

            BlacklistedQueriesRegistrar.Instance.AddBlacklistedQuery(workspace, "A");
            Assert.True(BlacklistedQueriesRegistrar.Instance.RemoveBlacklistedQuery(workspace, "A"));
            Assert.False(BlacklistedQueriesRegistrar.Instance.RemoveBlacklistedQuery(workspace, "A"));
        }

        [Fact]
        public void AddBlackListedQuery_checks_parameters()
        {
            Assert.Equal("workspace",
                Assert.Throws<ArgumentNullException>(
                    () => BlacklistedQueriesRegistrar.Instance.AddBlacklistedQuery(null, "A")).ParamName);

            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => BlacklistedQueriesRegistrar.Instance.AddBlacklistedQuery(
                        new MetadataWorkspace(), null)).ParamName);

            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => BlacklistedQueriesRegistrar.Instance.AddBlacklistedQuery(
                        new MetadataWorkspace(), string.Empty)).ParamName);

            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => BlacklistedQueriesRegistrar.Instance.AddBlacklistedQuery(
                        new MetadataWorkspace(), " ")).ParamName);
        }

        [Fact]
        public void RemoveBlackListedQuery_checks_parameters()
        {
            Assert.Equal("workspace",
                Assert.Throws<ArgumentNullException>(
                    () => BlacklistedQueriesRegistrar.Instance.RemoveBlacklistedQuery(null, "A")).ParamName);

            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => BlacklistedQueriesRegistrar.Instance.RemoveBlacklistedQuery(
                        new MetadataWorkspace(), null)).ParamName);

            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => BlacklistedQueriesRegistrar.Instance.RemoveBlacklistedQuery(
                        new MetadataWorkspace(), string.Empty)).ParamName);

            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => BlacklistedQueriesRegistrar.Instance.RemoveBlacklistedQuery(
                        new MetadataWorkspace(), " ")).ParamName);
        }

        [Fact]
        public void IsQueryBlacklisted_checks_parameters()
        {
            Assert.Equal("workspace",
                Assert.Throws<ArgumentNullException>(
                    () => BlacklistedQueriesRegistrar.Instance.IsQueryBlacklisted(null, "A")).ParamName);

            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => BlacklistedQueriesRegistrar.Instance.IsQueryBlacklisted(
                        new MetadataWorkspace(), null)).ParamName);

            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => BlacklistedQueriesRegistrar.Instance.IsQueryBlacklisted(
                        new MetadataWorkspace(), string.Empty)).ParamName);

            Assert.Equal("sql",
                Assert.Throws<ArgumentNullException>(
                    () => BlacklistedQueriesRegistrar.Instance.IsQueryBlacklisted(
                        new MetadataWorkspace(), " ")).ParamName);
        }
    }
}

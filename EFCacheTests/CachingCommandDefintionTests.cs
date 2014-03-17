namespace EFCache
{
    using Moq;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Core.Metadata.Edm;
    using Xunit;

    public class CachingCommandDefintionTests : TestBase
    {
        [Fact]
        public void Ctor_sets_parameters()
        {
            var entityType = EntityType.Create("Entity", "ns", DataSpace.CSpace, new string[0], new EdmMember[0], null);
            var entitySet = EntitySet.Create("EntitySet", "ns", null, null, entityType, null);

            var commandDefinition = new CachingCommandDefinition(
                new Mock<DbCommandDefinition>().Object, 
                new CommandTreeFacts(
                    new List<EntitySetBase> { entitySet }.AsReadOnly(), true, true),
                new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object,
                Mock.Of<CachingPolicy>());

            Assert.Equal(new[] { entitySet }, commandDefinition.AffectedEntitySets);
            Assert.True(commandDefinition.IsQuery);
            Assert.False(commandDefinition.IsCacheable);
        }

        [Fact]
        public void IsCacheable_is_true_for_queries_without_non_determtsinistic_functions()
        {
            var commandDefinition = new CachingCommandDefinition(
                new Mock<DbCommandDefinition>().Object, 
                new CommandTreeFacts(null, true, false),
                new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object,
                Mock.Of<CachingPolicy>());

            Assert.True(commandDefinition.IsQuery);
            Assert.True(commandDefinition.IsCacheable);
        }

        [Fact]
        public void IsCacheable_is_false_for_commands_or_queries_with_non_deterministic_functions()
        {
            var commandDefinition = new CachingCommandDefinition(
                new Mock<DbCommandDefinition>().Object, 
                new CommandTreeFacts(null, true, true),
                new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object,
                Mock.Of<CachingPolicy>());

            Assert.True(commandDefinition.IsQuery);
            Assert.False(commandDefinition.IsCacheable);

            commandDefinition = new CachingCommandDefinition(
                new Mock<DbCommandDefinition>().Object, 
                new CommandTreeFacts(null, false, true),
                new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object,
                Mock.Of<CachingPolicy>());

            Assert.False(commandDefinition.IsQuery);
            Assert.False(commandDefinition.IsCacheable);
        }

        [Fact]
        public void CreateCommand_creates_CachingCommand()
        {
            var mockCommandDefinition = new Mock<DbCommandDefinition>();
            mockCommandDefinition
                .Setup(d => d.CreateCommand())
                .Returns(new Mock<DbCommand>().Object);

            var commandDefintion = 
                new CachingCommandDefinition(
                    mockCommandDefinition.Object,
                    new CommandTreeFacts(null, true, false),
                    new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object,
                    Mock.Of<CachingPolicy>())
                .CreateCommand();

            Assert.IsType<CachingCommand>(commandDefintion);
            
            mockCommandDefinition.Verify(d => d.CreateCommand(), Times.Once);
        }
    }
}

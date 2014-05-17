// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace EFCache
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Common.CommandTrees;
    using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Linq;
    using Xunit;

    public class CommandTreeFactsTests : TestBase
    {
        [Fact]
        public void IsQuery_true_for_DbQueryCommandTree()
        {
            var commandTreeFacts = 
                new CommandTreeFacts(
                    new DbQueryCommandTree(
                        new MetadataWorkspace(), 
                        DataSpace.CSpace, 
                        TypeUsage.CreateDefaultTypeUsage(
                            PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String)).Null(), 
                            validate: false));

            Assert.True(commandTreeFacts.IsQuery);
        }

        [Fact]
        public void IsQuery_false_for_non_DbQueryCommandTree()
        {
            var collectionExpression =
                TypeUsage.CreateDefaultTypeUsage(
                    PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String)
                        .GetCollectionType())
                    .NewEmptyCollection();

            var commandTrees = new DbCommandTree[]
                {
                    new DbInsertCommandTree(
                        new MetadataWorkspace(),
                        DataSpace.CSpace,
                        collectionExpression.Bind(),
                        new List<DbModificationClause>().AsReadOnly(),
                        collectionExpression),

                    new DbUpdateCommandTree(
                        new MetadataWorkspace(),
                        DataSpace.CSpace,
                        collectionExpression.Bind(),
                        DbExpressionBuilder.Constant(3),
                        new List<DbModificationClause>().AsReadOnly(),
                        collectionExpression),
                };

            foreach (var commandTree in commandTrees)
            {
                Assert.False(new CommandTreeFacts(commandTree).IsQuery);
            }
        }

        [Fact]
        public void Affected_entity_sets_and_functions_discovered_for_queries()
        {
            var e1 = EntityType.Create("e1", "ns", DataSpace.CSpace, new string[0], new EdmMember[0], null);
            var e2 = EntityType.Create("e2", "ns", DataSpace.CSpace, new string[0], new EdmMember[0], null);
            var es1 = EntitySet.Create("es1", "ns", null, null, e1, null);
            var es2 = EntitySet.Create("es2", "ns", null, null, e2, null);
            EntityContainer.Create("ec", DataSpace.CSpace, new[] { es1, es2 }, null, null);

            var commandTreeFacts = 
                new CommandTreeFacts(
                    new DbQueryCommandTree(
                        new MetadataWorkspace(),
                        DataSpace.CSpace,
                        es1.Scan().Exists().And(es2.Scan().Exists()),
                        validate: false));

            Assert.Equal(
                new[] { "es1", "es2" }, 
                commandTreeFacts.AffectedEntitySets.Select(s => s.Name));

            Assert.False(commandTreeFacts.UsesNonDeterministicFunctions);
        }

        [Fact]
        public void Affected_entity_sets_discovered_for_modification_commands()
        {
            var entityType = EntityType.Create("e", "ns", DataSpace.CSpace, new string[0], new EdmMember[0], null);
            var entitySet = EntitySet.Create("es", "ns", null, null, entityType, null);
            EntityContainer.Create("ec", DataSpace.CSpace, new[] { entitySet }, null, null);

            var commandTreeFacts =
                new CommandTreeFacts(
                    new DbInsertCommandTree(
                        new MetadataWorkspace(),
                        DataSpace.CSpace,
                        entitySet.Scan().Bind(),
                        new List<DbModificationClause>().AsReadOnly(),
                        TypeUsage.CreateDefaultTypeUsage(entityType).Null()));

            Assert.Equal(
                new[] { "es" },
                commandTreeFacts.AffectedEntitySets.Select(s => s.Name));
        }

        [Fact]
        public void Nondeterministic_functions_discovered_for_queries()
        {
            var f =
                EdmFunction.Create(
                    "CURRENTDATETIME", "EDM", DataSpace.CSpace,
                    new EdmFunctionPayload()
                    {
                        ReturnParameters = new [] 
                        {
                            FunctionParameter.Create(
                                "ReturnValue", 
                                PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String), 
                                ParameterMode.ReturnValue)
                        }
                    },
                    null);

            var commandTreeFacts =
                new CommandTreeFacts(
                    new DbQueryCommandTree(
                        new MetadataWorkspace(),
                        DataSpace.CSpace,
                        f.Invoke(),
                        validate: false));

            Assert.True(commandTreeFacts.UsesNonDeterministicFunctions);
        }

        [Fact]
        public void MetadataWorkspace_initialized_from_DbQueryCommandTree()
        {
            var workspace = new MetadataWorkspace();

            var commandTreeFacts =
                new CommandTreeFacts(
                    new DbQueryCommandTree(
                        workspace,
                        DataSpace.CSpace,
                        TypeUsage.CreateDefaultTypeUsage(
                            PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String)).Null(),
                            validate: false));

            Assert.Same(workspace, commandTreeFacts.MetadataWorkspace);
        }
    }
}

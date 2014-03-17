
namespace EFCache
{
    using Moq;
    using Moq.Protected;
    using System;
    using System.Data.Common;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Core.Common.CommandTrees;
    using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Spatial;
    using System.IO;
    using System.Xml;
    using System.Xml.Linq;
    using Xunit;

    public class CachingProviderServicesTests : TestBase
    {
        [Fact]
        public void CreateDbCommandDefinition_invokes_CreateCommandDefinition_on_wrapped_provider()
        {
            var mockProviderServices = new Mock<DbProviderServices>();

            var workspace = new MetadataWorkspace(
                () => new EdmItemCollection(
                    new[] { 
                    XmlReader.Create(
                        new StringReader(
                            @"<Schema xmlns=""http://schemas.microsoft.com/ado/2009/11/edm"" Namespace=""ExampleModel"" />"))
                }), () => null, () => null);

            var commandDefinition =
                new CachingProviderServices(mockProviderServices.Object, new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object)
                    .CreateCommandDefinition(
                        new Mock<DbProviderManifest>().Object, 
                        new DbQueryCommandTree(workspace, DataSpace.CSpace, DbExpressionBuilder.True));

            Assert.IsType<CachingCommandDefinition>(commandDefinition);            
            Assert.True(((CachingCommandDefinition)commandDefinition).IsCacheable);
            Assert.True(((CachingCommandDefinition)commandDefinition).IsQuery);

            mockProviderServices
                .Protected()
                .Verify("CreateDbCommandDefinition",
                    Times.Once(),
                    ItExpr.IsAny<DbProviderManifest>(),
                    ItExpr.IsAny<DbCommandTree>());
        }

        [Fact]
        public void GetProviderManifestToken_invokes_GetProviderManifestToken_on_wrapped_provider()
        {
            var mockProviderServices = new Mock<DbProviderServices>();
            mockProviderServices.Protected()
                .Setup<string>("GetDbProviderManifestToken", ItExpr.IsAny<DbConnection>())
                .Returns("FakeManifestToken");

            Assert.Equal(
                "FakeManifestToken",
                new CachingProviderServices(mockProviderServices.Object, new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object)
                .GetProviderManifestToken(new Mock<DbConnection>().Object));

            mockProviderServices
                .Protected()
                .Verify("GetDbProviderManifestToken", Times.Once(), ItExpr.IsAny<DbConnection>());
        }

        [Fact]
        public void GetProviderManifest_invokes_GetProviderManifest_on_wrapped_provider()
        {
            var mockProviderServices = new Mock<DbProviderServices>();
            var providerManifest = new Mock<DbProviderManifest>().Object;
            mockProviderServices.Protected()
                .Setup<DbProviderManifest>("GetDbProviderManifest", ItExpr.IsAny<string>())
                .Returns(providerManifest);

            Assert.Same(
                providerManifest,
                new CachingProviderServices(mockProviderServices.Object, new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object)
                .GetProviderManifest("FakeManifestToken"));

            mockProviderServices
                .Protected()
                .Verify("GetDbProviderManifest", Times.Once(), ItExpr.IsAny<string>());
        }

        [Fact]
        public void DbDatabaseExists_invokes_DatabaseExists_on_wrapped_provider()
        {
            var mockProviderServices = new Mock<DbProviderServices>();
            mockProviderServices
                .Protected()
                .Setup<bool>("DbDatabaseExists", ItExpr.IsAny<DbConnection>(), ItExpr.IsAny<int?>(),
                    ItExpr.IsAny<StoreItemCollection>())
                .Returns(true);

            var storeItemCollection = CreateStoreItemCollection();

            Assert.True(
                new CachingProviderServices(mockProviderServices.Object,
                    new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object)
                    .DatabaseExists(Mock.Of<DbConnection>(), null, storeItemCollection));

            mockProviderServices
                .Protected()
                .Verify("DbDatabaseExists", Times.Once(), ItExpr.IsAny<DbConnection>(), ItExpr.IsAny<int?>(), storeItemCollection);
        }

        [Fact]
        public void DbDeleteDatabase_invokes_DeleteDatabase_on_wrapped_provider()
        {
            var mockProviderServices = new Mock<DbProviderServices>();
            mockProviderServices
                .Protected()
                .Setup("DbDeleteDatabase", ItExpr.IsAny<DbConnection>(), ItExpr.IsAny<int?>(),
                    ItExpr.IsAny<StoreItemCollection>());

            var storeItemCollection = CreateStoreItemCollection();

            new CachingProviderServices(mockProviderServices.Object,
                    new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object)
                    .DeleteDatabase(Mock.Of<DbConnection>(), null, storeItemCollection);

            mockProviderServices
                .Protected()
                .Verify("DbDeleteDatabase", Times.Once(), ItExpr.IsAny<DbConnection>(), ItExpr.IsAny<int?>(), storeItemCollection);
        }

        [Fact]
        public void DbCreateDatabase_invokes_CreateDatabase_on_wrapped_provider()
        {
            var mockProviderServices = new Mock<DbProviderServices>();

            var storeItemCollection = CreateStoreItemCollection();

            new CachingProviderServices(mockProviderServices.Object, new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object)
                .CreateDatabase(Mock.Of<DbConnection>(), null, storeItemCollection);

            mockProviderServices
                .Protected()
                .Verify("DbCreateDatabase", Times.Once(), ItExpr.IsAny<DbConnection>(), ItExpr.IsAny<int?>(), storeItemCollection);
        }

        [Fact]
        public void DbCreateDatabaseScript_invokes_CreateDatabaseScript_on_wrapped_provider()
        {
            var mockProviderServices = new Mock<DbProviderServices>();
            mockProviderServices
                .Protected()
                .Setup<string>("DbCreateDatabaseScript", ItExpr.IsAny<string>(), ItExpr.IsAny<StoreItemCollection>())
                .Returns("CREATE DB");

            var storeItemCollection = CreateStoreItemCollection();

            Assert.Equal(
                "CREATE DB",
                new CachingProviderServices(mockProviderServices.Object, new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object)
                    .CreateDatabaseScript("manifestToken", storeItemCollection));

            mockProviderServices
                .Protected()
                .Verify("DbCreateDatabaseScript", Times.Once(), "manifestToken", storeItemCollection);
        }

        [Fact]
        public void SetDbParameterValue_invokes_SetParameterValue_on_wrapped_provider()
        {
            var mockProviderServices = new Mock<DbProviderServices>();

            var typeUsage = TypeUsage.CreateStringTypeUsage(PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String), true, false);
            
            new CachingProviderServices(mockProviderServices.Object, new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object)
                .SetParameterValue(Mock.Of<DbParameter>(), typeUsage, "abc");

            mockProviderServices
                .Protected()
                .Verify("SetDbParameterValue", Times.Once(), ItExpr.IsAny<DbParameter>(), typeUsage, "abc");
        }

        [Fact]
        public void GetDbSpatialDataReader_invokes_GetSpatialReader_on_wrapped_provider()
        {
            var spatialReader = Mock.Of<DbSpatialDataReader>();

            var mockProviderServices = new Mock<DbProviderServices>();
            mockProviderServices
                .Protected()
                .Setup<DbSpatialDataReader>("GetDbSpatialDataReader", ItExpr.IsAny<DbDataReader>(), ItExpr.IsAny<string>())
                .Returns(spatialReader);

            Assert.Same(spatialReader,
            new CachingProviderServices(mockProviderServices.Object, new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object)
                .GetSpatialDataReader(Mock.Of<DbDataReader>(), "abc"));

            mockProviderServices
                .Protected()
                .Verify("GetDbSpatialDataReader", Times.Once(), ItExpr.IsAny<DbDataReader>(), "abc");
        }

        [Fact]
        public void DbGetSpatialServices_invokes_GetSpatialServices_on_wrapped_provider()
        {
            var spatialServices = Mock.Of<DbSpatialServices>();

            var mockProviderServices = new Mock<DbProviderServices>();
            mockProviderServices
                .Protected()
                .Setup<DbSpatialServices>("DbGetSpatialServices", ItExpr.IsAny<string>())
                .Returns(spatialServices);

#pragma warning disable 618

            Assert.Same(
                spatialServices,
                new CachingProviderServices(mockProviderServices.Object, new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object)
                    .GetSpatialServices("abc"));

#pragma warning restore 618

            mockProviderServices
                .Protected()
                .Verify("DbGetSpatialServices", Times.Once(), "abc");
        }
        
        private static StoreItemCollection CreateStoreItemCollection()
        {
            const string ssdl =
                @"<Schema Namespace=""NorthwindEFModel.Store"" Alias=""Self"" Provider=""System.Data.SqlClient"" ProviderManifestToken=""2008"" xmlns=""http://schemas.microsoft.com/ado/2009/02/edm/ssdl"">
    <EntityContainer Name=""NorthwindEFModelStoreContainer"" />
</Schema>";

            return new StoreItemCollection(new[] {XDocument.Parse(ssdl).CreateReader()});
        }

        [Fact]
        public void GetService_invokes_GetService_on_wrapped_provider()
        {
            var service = new object();
            var key = new object();

            var mockProviderServices = new Mock<DbProviderServices>();
            mockProviderServices
                .Setup(s => s.GetService(It.IsAny<Type>(), It.IsAny<object>()))
                .Returns(service);

            Assert.Same(
                service,
                new CachingProviderServices(mockProviderServices.Object, new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object)
                    .GetService(typeof(string), key));

            mockProviderServices.Verify(s => s.GetService(typeof(string), key), Times.Once);
        }

        [Fact]
        public void GetServices_invokes_GetServices_on_wrapped_provider()
        {
            var services = new object[0];
            var key = new object();

            var mockProviderServices = new Mock<DbProviderServices>();
            mockProviderServices
                .Setup(s => s.GetServices(It.IsAny<Type>(), It.IsAny<object>()))
                .Returns(services);

            Assert.Same(
                services,
                new CachingProviderServices(mockProviderServices.Object, new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object)
                    .GetServices(typeof(string), key));

            mockProviderServices.Verify(s => s.GetServices(typeof(string), key), Times.Once);            
        }

        [Fact]
        public void RegisterInfoMessageHandler_invokes_RegisterInfoMessageHandler_on_wrapped_provider()
        {
            Action<string> handler = s => { };

            var mockProviderServices = new Mock<DbProviderServices>();
            mockProviderServices
                .Setup(s => s.RegisterInfoMessageHandler(It.IsAny<DbConnection>(), It.IsAny<Action<string>>()));

            new CachingProviderServices(mockProviderServices.Object, new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object)
                .RegisterInfoMessageHandler(Mock.Of<DbConnection>(), handler);

            mockProviderServices.Verify(s => s.RegisterInfoMessageHandler(It.IsNotNull<DbConnection>(), handler), Times.Once);
        }

        [Fact]
        public void CreateCommandDefinition_creates_valid_CachingCommandDefinition_for_CachingCommand()
        {
            var command = Mock.Of<DbCommand>();
            var mockCommandDefinition = new Mock<DbCommandDefinition>();
            mockCommandDefinition.Setup(d => d.CreateCommand()).Returns(command);
            var commandTreeFacts = new CommandTreeFacts(null, true, true);
            var transactionHandler = new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object;
            var cachingPolicy = Mock.Of<CachingPolicy>();

            var cachingCommand = new CachingCommand(command, commandTreeFacts, transactionHandler, cachingPolicy); 

            var mockProviderServices = new Mock<DbProviderServices>();
            mockProviderServices.Setup(s => s.CreateCommandDefinition(command)).Returns(mockCommandDefinition.Object);

            var newCommandDefinition = 
                new CachingProviderServices(
                    mockProviderServices.Object, new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object)
                        .CreateCommandDefinition(cachingCommand);

            Assert.IsType<CachingCommandDefinition>(newCommandDefinition);
            mockProviderServices.Verify(s => s.CreateCommandDefinition(command), Times.Once);

            var newCommand = (CachingCommand)newCommandDefinition.CreateCommand();
            Assert.Same(command, newCommand.WrappedCommand);
            Assert.Same(commandTreeFacts, newCommand.CommandTreeFacts);
            Assert.Same(transactionHandler, newCommand.CacheTransactionHandler);
            Assert.Same(cachingPolicy, newCommand.CachingPolicy);
        }

        [Fact]
        public void CreateCommandDefinition_creates_valid_DbCommandDefinition_for_DbCommand()
        {
            var command = Mock.Of<DbCommand>();
            var mockCommandDefinition = new Mock<DbCommandDefinition>();
            mockCommandDefinition.Setup(d => d.CreateCommand()).Returns(command);

            var mockProviderServices = new Mock<DbProviderServices>();
            mockProviderServices.Setup(s => s.CreateCommandDefinition(command)).Returns(mockCommandDefinition.Object);

            var newCommandDefinition =
                new CachingProviderServices(
                    mockProviderServices.Object, new Mock<CacheTransactionHandler>(Mock.Of<ICache>()).Object)
                        .CreateCommandDefinition(command);

            Assert.IsNotType<CachingCommandDefinition>(newCommandDefinition);
            mockProviderServices.Verify(s => s.CreateCommandDefinition(command), Times.Once);

            var newCommand = newCommandDefinition.CreateCommand();
            Assert.Same(command, newCommand);
        }
    }
}

namespace EFCache
{
    using System.Collections.Generic;
    using Xunit;

    public class CachedResultsTests : TestBase
    {
        [Fact]
        public void CachedResults_properties_set_correctly()
        {
            var tableMetadata = new ColumnMetadata[0];
            var results = new List<object[]>();

            var cachedResults = new CachedResults(tableMetadata, results, 42);

            Assert.Same(tableMetadata, cachedResults.TableMetadata);
            Assert.Same(results, cachedResults.Results);
            Assert.Equal(42, cachedResults.RecordsAffected);
        }
    }
}

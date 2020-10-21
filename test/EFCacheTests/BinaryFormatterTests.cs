using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;

namespace EFCache
{
    public class BinaryFormatterTests
    {
        [Fact]
        public void CachedResults_binary_formatter_can_serialize()
        {
            var tableMetadata = new ColumnMetadata[]
            {
                new ColumnMetadata("age", "int", typeof(System.Int32))
            };
            
            var cachedResults = new CachedResults(tableMetadata, new List<object[]>{new object[]{105}}, 1);
            var formatter = new BinaryFormatter();

            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, cachedResults);
                stream.Seek(0L, SeekOrigin.Begin);
                var deserialized = (CachedResults)formatter.Deserialize(stream);
                
                Assert.Equal(1, deserialized.RecordsAffected);
                Assert.Equal("age", deserialized.TableMetadata.Single().Name);
                Assert.Equal("int", deserialized.TableMetadata.Single().DataTypeName);
                Assert.Equal(105, deserialized.Results[0][0]);
            }
        }
    }
}
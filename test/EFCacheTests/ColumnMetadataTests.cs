// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace EFCache
{
    using Xunit;

    public class ColumnMetadataTests
    {
        [Fact]
        public void Ctor_sets_properties()
        {
            var columnMetadata = new ColumnMetadata("Name", "Type", typeof(int));

            Assert.Equal("Name", columnMetadata.Name);
            Assert.Equal("Type", columnMetadata.DataTypeName);
            Assert.Same(typeof(int), columnMetadata.DataType);
        }
    }
}

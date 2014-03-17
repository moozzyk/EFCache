
namespace EFCache
{
    using System;

    internal struct ColumnMetadata
    {
        private readonly string _name;
        private readonly string _dataTypeName;
        private readonly Type _dataType;

        public ColumnMetadata(string name, string dataTypeName, Type dataType)
        {
            _name = name;
            _dataTypeName = dataTypeName;
            _dataType = dataType;
        }

        public string Name
        {
            get { return _name; }
        }

        public string DataTypeName
        {
            get { return _dataTypeName; }
        }

        public Type DataType
        {
            get { return _dataType; }
        }
    }
}

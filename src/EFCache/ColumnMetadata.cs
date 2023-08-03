// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.
namespace EFCache
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public struct ColumnMetadata : ISerializable
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

        public ColumnMetadata(SerializationInfo info, StreamingContext context)
        {
            // Reset the property value using the GetValue method.
            _name = (string) info.GetValue("name", typeof(string));
            _dataTypeName = (string) info.GetValue("datatypename", typeof(string));
            _dataType = Type.GetType((string) info.GetValue("datatype", typeof(string)));
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

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("datatypename", _dataTypeName);
            info.AddValue("datatype", DataType.FullName);
            info.AddValue("name", Name);
        }
    }
}
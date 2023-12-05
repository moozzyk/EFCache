// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace EFCache
{
    using System;
    using System.Collections.Generic;

    [Serializable]
    public class CachedResults
    {
        private readonly ColumnMetadata[] _tableMetadata;
        private readonly List<object[]> _results;
        private readonly int _recordsAffected;

        public CachedResults(ColumnMetadata[] tableMetadata, List<object[]> results, int recordsAffected)
        {
            _tableMetadata = tableMetadata;
            _results = results;
            _recordsAffected = recordsAffected;
        }

        public ColumnMetadata[] TableMetadata
        {
            get { return _tableMetadata; }
        }

        public List<object[]> Results
        {
            get { return _results; }
        }

        public int RecordsAffected
        {
            get { return _recordsAffected; }
        }
    }
}

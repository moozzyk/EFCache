using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EFCache
{
    public interface ICachingCommandStrategy
    {
        bool IsCacheable();
        void InvalidateSets(int resultRecordsAffected);
        string CreateKey();
        bool GetCachedDbDataReader(string key, out DbDataReader dbDataReader);
        DbDataReader SetCacheFromDbDataReader(string key, DbDataReader reader);
#if !NET40
        Task<DbDataReader> SetCacheFromDbDataReaderAsync(string key, DbDataReader reader, CancellationToken cancellationToken);
#endif
        bool GetCachedScalarObject(string key, out object cachedObject);
        void SetCacheFromScalarObject(string key, object value);
    }
}

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFCache
{
    public interface ICachingCommandMetadata
    {
        string CommandText { get; }
        DbParameterCollection Parameters { get; }
        DbConnection Connection { get; }
        DbTransaction Transaction { get; }
    }
}

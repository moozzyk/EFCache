using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFCache
{
    public interface ICommandTreeFacts
    {
        bool IsQuery { get; }

        bool UsesNonDeterministicFunctions { get; }

        MetadataWorkspace MetadataWorkspace { get; }
        ReadOnlyCollection<EntitySetBase> AffectedEntitySets { get; }
    }
}

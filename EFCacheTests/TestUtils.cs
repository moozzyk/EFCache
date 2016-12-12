// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace EFCache
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Linq;

    public class TestUtils
    {
        public static ReadOnlyCollection<EntitySetBase> CreateEntitySets(params string[] setNames)
        {
            return CreateEntitySetsEx(setNames, Enumerable.Range(0, setNames.Length).Select(i => "ns").ToArray(),
                new string[setNames.Length]);
        }

        public static ReadOnlyCollection<EntitySetBase> CreateEntitySetsEx(string[] setNames, string[] tableSchemas, string[] tableNames)
        {
            if (setNames == null)
            {
                throw new ArgumentNullException(nameof(setNames));
            }

            if (tableSchemas == null)
            {
                throw new ArgumentNullException(nameof(tableSchemas));
            }

            if (tableNames == null)
            {
                throw new ArgumentNullException(nameof(tableNames));
            }

            if (tableSchemas.Length != tableNames.Length)
            {
                throw new ArgumentException(
                    "The number of table schemas must be the same as the number of set names.", nameof(tableSchemas));
            }

            if (setNames.Length != tableNames.Length)
            {
                throw new ArgumentException(
                    "The number of table names must be the same as the number of set names.", nameof(tableNames));
            }

            var entitySets = new List<EntitySetBase>();

            for (var i = 0; i < setNames.Length; i++)
            {
                var setName = setNames[i];
                var entityType =
                    EntityType.Create(setName + "EntityType", "ns", DataSpace.CSpace,
                    new string[0], new EdmMember[0], null);

                entitySets.Add(EntitySet.Create(setName, tableSchemas[i], tableNames[i], null, entityType, null));
            }

            return entitySets.AsReadOnly();
        }
    }
}

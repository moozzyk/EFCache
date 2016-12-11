// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace EFCache
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Data.Entity.Core.Metadata.Edm;

    public class TestUtils
    {
        public static ReadOnlyCollection<EntitySetBase> CreateEntitySets(params string[] setNames)
        {
            return CreateEntitySetsEx(setNames, new string[setNames.Length]);
        }

        public static ReadOnlyCollection<EntitySetBase> CreateEntitySetsEx(string[] setNames, string[] tableNames)
        {
            if (setNames == null)
            {
                throw new ArgumentNullException(nameof(setNames));
            }

            if (tableNames == null)
            {
                throw new ArgumentNullException(nameof(tableNames));
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

                entitySets.Add(EntitySet.Create(setName, "ns", tableNames[i], null, entityType, null));
            }

            return entitySets.AsReadOnly();
        }
    }
}

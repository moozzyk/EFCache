// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace EFCache
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Core.Objects;

    public static class ObjectContextExtensions
    {
        public static CachingProviderServices GetCachingProviderServices(this ObjectContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var providerInvariantName =
                ((StoreItemCollection) context.MetadataWorkspace.GetItemCollection(DataSpace.SSpace))
                    .ProviderInvariantName;
            return
                DbConfiguration.DependencyResolver.GetService(typeof (DbProviderServices), providerInvariantName) as
                    CachingProviderServices;
        }
    }
}

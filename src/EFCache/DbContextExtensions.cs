// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace EFCache
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;

    public static class DbContextExtensions
    {
        public static CachingProviderServices GetCachingProviderServices(this DbContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            return ((IObjectContextAdapter) context).ObjectContext.GetCachingProviderServices();
        }
    }
}

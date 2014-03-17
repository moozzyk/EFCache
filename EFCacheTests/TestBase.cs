// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace EFCache
{
    using System.Data.Entity;

    public class TestBase
    {
        static TestBase()
        {
            DbConfiguration.SetConfiguration(new Configuration());
        }
    }
}

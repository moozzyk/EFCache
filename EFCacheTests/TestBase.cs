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

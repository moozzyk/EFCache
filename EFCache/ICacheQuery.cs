namespace EFCache
{
    using System.Data.Entity.Core.Metadata.Edm;

    public interface ICacheQuery
    {
        void AddQuery(MetadataWorkspace workspace, string sql);
        bool RemoveQuery(MetadataWorkspace workspace, string sql);
        bool ContainsQuery(MetadataWorkspace workspace, string sql);
    }
}

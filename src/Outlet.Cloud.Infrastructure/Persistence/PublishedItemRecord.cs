namespace Outlet.Cloud.Infrastructure.Persistence;

/// <summary>EF persistence model for a published registry item.</summary>
public sealed class PublishedItemRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ManifestJson { get; set; } = string.Empty;
    public List<PublishedFileRecord> Files { get; set; } = [];
}

/// <summary>EF persistence model for one file of a published item.</summary>
public sealed class PublishedFileRecord
{
    public int Id { get; set; }
    public Guid PublishedItemId { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

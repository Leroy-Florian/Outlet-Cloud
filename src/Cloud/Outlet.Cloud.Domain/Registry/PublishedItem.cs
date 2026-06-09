using Outlet.Cloud.Domain.Organizations;
using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Domain.Registry;

/// <summary>
/// AGGREGATE ROOT — a registry item published into an organization's private registry.
/// The manifest is stored as opaque text (the domain never parses JSON — that is an
/// Infrastructure/Web concern); the files carry the raw content the CLI downloads.
///
/// Invariants: a published item always has a non-empty manifest and at least one file.
/// </summary>
public sealed class PublishedItem : AggregateRoot<PublishedItemId>
{
    public OrganizationId OrganizationId { get; }
    public RegistryItemName Name { get; }
    public string ManifestJson { get; private set; }

    private readonly List<PublishedFile> _files;
    public IReadOnlyList<PublishedFile> Files => _files;

    private PublishedItem(PublishedItemId id, OrganizationId organizationId, RegistryItemName name, string manifestJson, IEnumerable<PublishedFile> files)
        : base(id)
    {
        OrganizationId = organizationId;
        Name = name;
        ManifestJson = manifestJson;
        _files = [.. files];
    }

    public static Result<PublishedItem> Create(
        PublishedItemId id,
        OrganizationId organizationId,
        RegistryItemName name,
        string manifestJson,
        IReadOnlyCollection<PublishedFile> files)
    {
        if (string.IsNullOrWhiteSpace(manifestJson))
            return Result<PublishedItem>.Failure("A published item must have a manifest.");

        if (files.Count == 0)
            return Result<PublishedItem>.Failure("A published item must ship at least one file.");

        return Result<PublishedItem>.Success(new PublishedItem(id, organizationId, name, manifestJson, files));
    }

    /// <summary>Rehydrates a published item from trusted persistence (no validation, no events).</summary>
    public static PublishedItem Restore(
        PublishedItemId id,
        OrganizationId organizationId,
        RegistryItemName name,
        string manifestJson,
        IEnumerable<PublishedFile> files) =>
        new(id, organizationId, name, manifestJson, files);
}

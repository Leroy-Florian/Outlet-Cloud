using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Domain.Analytics;

public sealed class DownloadSnapshot : AggregateRoot<Guid>
{
    private DownloadSnapshot(
        Guid id,
        ProductId productId,
        PackageRegistry registry,
        PackageId packageId,
        long totalDownloads,
        string? latestVersion,
        DateTime capturedAt)
        : base(id)
    {
        ProductId = productId;
        Registry = registry;
        PackageId = packageId;
        TotalDownloads = totalDownloads;
        LatestVersion = latestVersion;
        CapturedAt = capturedAt;
    }

    public ProductId ProductId { get; }

    public PackageRegistry Registry { get; }

    public PackageId PackageId { get; }

    public long TotalDownloads { get; }

    /// <summary>
    /// Latest published version at capture time (best-effort: null when the
    /// registry's version endpoint was unreachable). Trends use version changes
    /// between consecutive snapshots as release markers.
    /// </summary>
    public string? LatestVersion { get; }

    public DateTime CapturedAt { get; }

    public static Result<DownloadSnapshot> Create(
        ProductId productId,
        PackageRegistry registry,
        PackageId packageId,
        long totalDownloads,
        DateTime capturedAt,
        string? latestVersion = null)
    {
        if (totalDownloads < 0)
        {
            return Result.Failure<DownloadSnapshot>(
                "DownloadSnapshot.NegativeCount: A download count cannot be negative.");
        }

        return Result.Success(new DownloadSnapshot(Guid.NewGuid(), productId, registry, packageId, totalDownloads, latestVersion, capturedAt));
    }
}

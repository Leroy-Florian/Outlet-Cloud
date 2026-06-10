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
        DateTime capturedAt)
        : base(id)
    {
        ProductId = productId;
        Registry = registry;
        PackageId = packageId;
        TotalDownloads = totalDownloads;
        CapturedAt = capturedAt;
    }

    public ProductId ProductId { get; }

    public PackageRegistry Registry { get; }

    public PackageId PackageId { get; }

    public long TotalDownloads { get; }

    public DateTime CapturedAt { get; }

    public static Result<DownloadSnapshot> Create(
        ProductId productId,
        PackageRegistry registry,
        PackageId packageId,
        long totalDownloads,
        DateTime capturedAt)
    {
        if (totalDownloads < 0)
        {
            return Result.Failure<DownloadSnapshot>(
                "DownloadSnapshot.NegativeCount: A download count cannot be negative.");
        }

        return Result.Success(new DownloadSnapshot(Guid.NewGuid(), productId, registry, packageId, totalDownloads, capturedAt));
    }
}

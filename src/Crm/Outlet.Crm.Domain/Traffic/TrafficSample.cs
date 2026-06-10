using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Domain.Traffic;

/// <summary>
/// One page view on a product's docs/site. The referrer is normalized into a
/// stable source string ("google", "github", "direct", raw host…) at ingest time
/// so aggregations never re-parse raw referrers.
/// </summary>
public sealed class TrafficSample : AggregateRoot<Guid>
{
    private TrafficSample(
        Guid id,
        ProductId productId,
        string path,
        string referrerSource,
        string? userAgentCategory,
        DateTime occurredAt)
        : base(id)
    {
        ProductId = productId;
        Path = path;
        ReferrerSource = referrerSource;
        UserAgentCategory = userAgentCategory;
        OccurredAt = occurredAt;
    }

    public ProductId ProductId { get; }

    public string Path { get; }

    public string ReferrerSource { get; }

    public string? UserAgentCategory { get; }

    public DateTime OccurredAt { get; }

    public static Result<TrafficSample> Create(
        ProductId productId,
        string path,
        string? referrer,
        string? userAgent,
        DateTime occurredAt)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Result.Failure<TrafficSample>("TrafficSample.PathRequired: A path is required.");
        }

        var normalizedPath = path.Trim();
        if (!normalizedPath.StartsWith('/'))
        {
            normalizedPath = "/" + normalizedPath;
        }

        return Result.Success(new TrafficSample(
            Guid.NewGuid(),
            productId,
            normalizedPath,
            Traffic.ReferrerSource.Normalize(referrer),
            Traffic.UserAgentCategory.Categorize(userAgent),
            occurredAt));
    }
}

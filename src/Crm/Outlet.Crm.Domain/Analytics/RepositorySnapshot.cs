using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Domain.Analytics;

/// <summary>État d'un repository GitHub à un instant donné (issues ouvertes, stars, forks).</summary>
public sealed class RepositorySnapshot : AggregateRoot<Guid>
{
    private RepositorySnapshot(
        Guid id,
        ProductId productId,
        RepositoryName repository,
        int openIssues,
        int stars,
        int forks,
        DateTime capturedAt)
        : base(id)
    {
        ProductId = productId;
        Repository = repository;
        OpenIssues = openIssues;
        Stars = stars;
        Forks = forks;
        CapturedAt = capturedAt;
    }

    public ProductId ProductId { get; }

    public RepositoryName Repository { get; }

    public int OpenIssues { get; }

    public int Stars { get; }

    public int Forks { get; }

    public DateTime CapturedAt { get; }

    public static Result<RepositorySnapshot> Create(
        ProductId productId,
        RepositoryName repository,
        int openIssues,
        int stars,
        int forks,
        DateTime capturedAt)
    {
        if (openIssues < 0 || stars < 0 || forks < 0)
        {
            return Result.Failure<RepositorySnapshot>(
                "RepositorySnapshot.NegativeCount: Repository counters cannot be negative.");
        }

        return Result.Success(new RepositorySnapshot(Guid.NewGuid(), productId, repository, openIssues, stars, forks, capturedAt));
    }
}

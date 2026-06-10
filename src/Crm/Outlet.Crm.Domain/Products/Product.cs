using Outlet.Crm.Domain.Analytics;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Domain.Products;

/// <summary>
/// Produit publié (Outlet, FluxPDF, Accordent…). Un produit regroupe plusieurs
/// packages publiés (NuGet et/ou npm) ; tout le reste du CRM (prospects,
/// paiements, analytics, métriques) est rattaché à un produit.
/// </summary>
public sealed class Product : AggregateRoot<ProductId>
{
    private readonly List<TrackedPackage> _packages = [];
    private readonly List<TrackedRepository> _repositories = [];

    private Product(ProductId id, string name, string? description, DateTime createdAt)
        : base(id)
    {
        Name = name;
        Description = description;
        CreatedAt = createdAt;
    }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public bool IsArchived { get; private set; }

    public DateTime CreatedAt { get; }

    public IReadOnlyList<TrackedPackage> Packages => _packages;

    public IReadOnlyList<TrackedRepository> Repositories => _repositories;

    public static Result<Product> Create(string name, string? description, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Product>(ProductErrors.NameRequired);
        }

        return Result.Success(new Product(ProductId.New(), name.Trim(), description?.Trim(), createdAt));
    }

    public Result Update(string name, string? description)
    {
        if (IsArchived)
        {
            return Result.Failure(ProductErrors.Archived(Id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(ProductErrors.NameRequired);
        }

        Name = name.Trim();
        Description = description?.Trim();
        return Result.Success();
    }

    /// <summary>
    /// Soft archive: the product disappears from the active dashboard but its
    /// history (snapshots, traffic, payments) is preserved.
    /// </summary>
    public Result Archive()
    {
        if (IsArchived)
        {
            return Result.Failure(ProductErrors.AlreadyArchived(Id));
        }

        IsArchived = true;
        return Result.Success();
    }

    public Result TrackPackage(PackageRegistry registry, PackageId packageId)
    {
        if (IsArchived)
        {
            return Result.Failure(ProductErrors.Archived(Id));
        }

        if (IsTracking(registry, packageId))
        {
            return Result.Failure(ProductErrors.PackageAlreadyTracked(registry, packageId));
        }

        _packages.Add(new TrackedPackage(Guid.NewGuid(), registry, packageId));
        return Result.Success();
    }

    public Result UntrackPackage(PackageRegistry registry, PackageId packageId)
    {
        var removed = _packages.RemoveAll(p => p.Registry == registry && p.PackageId == packageId);
        if (removed is 0)
        {
            return Result.Failure(ProductErrors.PackageNotTracked(registry, packageId));
        }

        return Result.Success();
    }

    public bool IsTracking(PackageRegistry registry, PackageId packageId) =>
        _packages.Any(p => p.Registry == registry && p.PackageId == packageId);

    public Result TrackRepository(RepositoryName repository)
    {
        if (IsArchived)
        {
            return Result.Failure(ProductErrors.Archived(Id));
        }

        if (IsTrackingRepository(repository))
        {
            return Result.Failure(ProductErrors.RepositoryAlreadyTracked(repository));
        }

        _repositories.Add(new TrackedRepository(Guid.NewGuid(), repository));
        return Result.Success();
    }

    public Result UntrackRepository(RepositoryName repository)
    {
        var removed = _repositories.RemoveAll(r => r.Repository == repository);
        if (removed is 0)
        {
            return Result.Failure(ProductErrors.RepositoryNotTracked(repository));
        }

        return Result.Success();
    }

    public bool IsTrackingRepository(RepositoryName repository) =>
        _repositories.Any(r => r.Repository == repository);
}

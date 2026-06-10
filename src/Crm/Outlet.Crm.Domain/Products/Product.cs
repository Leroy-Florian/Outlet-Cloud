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

    public string Name { get; }

    public string? Description { get; }

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

    public Result TrackPackage(PackageRegistry registry, PackageId packageId)
    {
        if (IsTracking(registry, packageId))
        {
            return Result.Failure(ProductErrors.PackageAlreadyTracked(registry, packageId));
        }

        _packages.Add(new TrackedPackage(Guid.NewGuid(), registry, packageId));
        return Result.Success();
    }

    public bool IsTracking(PackageRegistry registry, PackageId packageId) =>
        _packages.Any(p => p.Registry == registry && p.PackageId == packageId);

    public Result TrackRepository(RepositoryName repository)
    {
        if (IsTrackingRepository(repository))
        {
            return Result.Failure(ProductErrors.RepositoryAlreadyTracked(repository));
        }

        _repositories.Add(new TrackedRepository(Guid.NewGuid(), repository));
        return Result.Success();
    }

    public bool IsTrackingRepository(RepositoryName repository) =>
        _repositories.Any(r => r.Repository == repository);
}

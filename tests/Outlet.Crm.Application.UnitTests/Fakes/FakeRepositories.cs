using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.ApiMetrics;
using Outlet.Crm.Domain.Organizations;
using Outlet.Crm.Domain.Payments;
using Outlet.Crm.Domain.Products;
using Outlet.Crm.Domain.Prospects;

namespace Outlet.Crm.Application.UnitTests.Fakes;

public sealed class FakeProductRepository : IProductRepository
{
    public List<Product> Items { get; } = [];

    public int UpdateCount { get; private set; }

    public Task<Product?> GetByIdAsync(ProductId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(p => p.Id == id));

    public Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Product>>(Items);

    public Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        Items.Add(product);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        UpdateCount++;
        return Task.CompletedTask;
    }
}

public sealed class FakeOrganizationRepository : IOrganizationRepository
{
    public List<Organization> Items { get; } = [];

    public Task<Organization?> GetByIdAsync(OrganizationId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(o => o.Id == id));

    public Task<IReadOnlyList<Organization>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Organization>>(Items);

    public Task AddAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        Items.Add(organization);
        return Task.CompletedTask;
    }
}

public sealed class FakeProspectRepository : IProspectRepository
{
    public List<Prospect> Items { get; } = [];

    public int UpdateCount { get; private set; }

    public Task<Prospect?> GetByIdAsync(ProspectId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(p => p.Id == id));

    public Task<IReadOnlyList<Prospect>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Prospect>>(Items);

    public Task AddAsync(Prospect prospect, CancellationToken cancellationToken = default)
    {
        Items.Add(prospect);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Prospect prospect, CancellationToken cancellationToken = default)
    {
        UpdateCount++;
        return Task.CompletedTask;
    }
}

public sealed class FakeDownloadSnapshotRepository : IDownloadSnapshotRepository
{
    public List<DownloadSnapshot> Items { get; } = [];

    public Task<IReadOnlyList<DownloadSnapshot>> ListByPackageAsync(
        ProductId productId,
        PackageRegistry registry,
        PackageId packageId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DownloadSnapshot>>(
            [.. Items.Where(s => s.ProductId == productId && s.Registry == registry && s.PackageId == packageId)]);

    public Task AddAsync(DownloadSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        Items.Add(snapshot);
        return Task.CompletedTask;
    }
}

public sealed class FakeRepositorySnapshotRepository : IRepositorySnapshotRepository
{
    public List<RepositorySnapshot> Items { get; } = [];

    public Task<IReadOnlyList<RepositorySnapshot>> ListByRepositoryAsync(
        ProductId productId,
        RepositoryName repository,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RepositorySnapshot>>(
            [.. Items.Where(s => s.ProductId == productId && s.Repository == repository)]);

    public Task AddAsync(RepositorySnapshot snapshot, CancellationToken cancellationToken = default)
    {
        Items.Add(snapshot);
        return Task.CompletedTask;
    }
}

public sealed class FakeApiMetricRepository : IApiMetricRepository
{
    public List<ApiMetricSample> Items { get; } = [];

    public Task<IReadOnlyList<ApiMetricSample>> ListSinceAsync(ProductId productId, DateTime since, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ApiMetricSample>>(
            [.. Items.Where(s => s.ProductId == productId && s.OccurredAt >= since)]);

    public Task AddAsync(ApiMetricSample sample, CancellationToken cancellationToken = default)
    {
        Items.Add(sample);
        return Task.CompletedTask;
    }
}

public sealed class FakePaymentRepository : IPaymentRepository
{
    public List<Payment> Items { get; } = [];

    public int UpdateCount { get; private set; }

    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(p => p.Id == id));

    public Task<IReadOnlyList<Payment>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Payment>>(Items);

    public Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        Items.Add(payment);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        UpdateCount++;
        return Task.CompletedTask;
    }
}

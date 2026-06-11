using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Products;
using Outlet.Crm.Domain.Releases;

namespace Outlet.Crm.Application.UnitTests.Fakes;

public sealed class FakeReleaseRepository : IReleaseRepository
{
    public List<ReleaseRecord> Items { get; } = [];

    public Task<IReadOnlyList<ReleaseRecord>> ListByProductAsync(ProductId productId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ReleaseRecord>>([.. Items.Where(r => r.ProductId == productId)]);

    public Task<bool> ExistsAsync(ProductId productId, RepositoryName repository, string tagName, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(r => r.ProductId == productId && r.Repository == repository && r.TagName == tagName));

    public Task AddAsync(ReleaseRecord release, CancellationToken cancellationToken = default)
    {
        Items.Add(release);
        return Task.CompletedTask;
    }
}

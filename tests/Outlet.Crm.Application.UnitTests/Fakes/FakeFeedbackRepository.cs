using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Feedback;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Application.UnitTests.Fakes;

public sealed class FakeFeedbackRepository : IFeedbackRepository
{
    public List<Domain.Feedback.Feedback> Items { get; } = [];

    public int UpdateCount { get; private set; }

    public Task<Domain.Feedback.Feedback?> GetByIdAsync(FeedbackId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(f => f.Id == id));

    public Task<IReadOnlyList<Domain.Feedback.Feedback>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Domain.Feedback.Feedback>>(Items);

    public Task<IReadOnlyList<Domain.Feedback.Feedback>> ListByProductAsync(ProductId productId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Domain.Feedback.Feedback>>([.. Items.Where(f => f.ProductId == productId)]);

    public Task AddAsync(Domain.Feedback.Feedback feedback, CancellationToken cancellationToken = default)
    {
        Items.Add(feedback);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Domain.Feedback.Feedback feedback, CancellationToken cancellationToken = default)
    {
        UpdateCount++;
        return Task.CompletedTask;
    }
}

using Outlet.Crm.Domain.Feedback;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Application.Ports;

/// <summary>SECONDARY PORT — persistence of <see cref="Domain.Feedback.Feedback"/> aggregates.</summary>
public interface IFeedbackRepository
{
    Task<Domain.Feedback.Feedback?> GetByIdAsync(FeedbackId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Domain.Feedback.Feedback>> ListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Domain.Feedback.Feedback>> ListByProductAsync(ProductId productId, CancellationToken cancellationToken = default);

    Task AddAsync(Domain.Feedback.Feedback feedback, CancellationToken cancellationToken = default);

    Task UpdateAsync(Domain.Feedback.Feedback feedback, CancellationToken cancellationToken = default);
}

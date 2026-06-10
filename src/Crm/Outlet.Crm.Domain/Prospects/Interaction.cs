using Outlet.Kernel.Shared;

namespace Outlet.Crm.Domain.Prospects;

public sealed class Interaction(Guid id, string channel, string notes, DateTime occurredAt) : Entity<Guid>(id)
{
    public string Channel { get; } = channel;

    public string Notes { get; } = notes;

    public DateTime OccurredAt { get; } = occurredAt;
}

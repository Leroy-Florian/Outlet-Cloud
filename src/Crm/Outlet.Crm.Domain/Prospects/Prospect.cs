using Outlet.Crm.Domain.Organizations;
using Outlet.Crm.Domain.Products;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Domain.Prospects;

public sealed class Prospect : AggregateRoot<ProspectId>
{
    private readonly List<Interaction> _interactions = [];

    private Prospect(
        ProspectId id,
        ProductId productId,
        OrganizationId? organizationId,
        string name,
        Email email,
        string? company,
        DateTime createdAt)
        : base(id)
    {
        ProductId = productId;
        OrganizationId = organizationId;
        Name = name;
        Email = email;
        Company = company;
        CreatedAt = createdAt;
        Stage = ProspectStage.New;
    }

    public ProductId ProductId { get; }

    public OrganizationId? OrganizationId { get; }

    public string Name { get; }

    public Email Email { get; }

    public string? Company { get; }

    public ProspectStage Stage { get; private set; }

    public DateTime CreatedAt { get; }

    public IReadOnlyList<Interaction> Interactions => _interactions;

    public static Result<Prospect> Create(
        ProductId productId,
        OrganizationId? organizationId,
        string name,
        Email email,
        string? company,
        DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Prospect>(ProspectErrors.NameRequired);
        }

        return Result.Success(new Prospect(ProspectId.New(), productId, organizationId, name.Trim(), email, company, createdAt));
    }

    public Result Advance(ProspectStage target)
    {
        if (Stage is ProspectStage.Won or ProspectStage.Lost)
        {
            return Result.Failure(ProspectErrors.AlreadyClosed);
        }

        if (target != ProspectStage.Lost && target <= Stage)
        {
            return Result.Failure(ProspectErrors.InvalidTransition);
        }

        Stage = target;
        return Result.Success();
    }

    public void RecordInteraction(string channel, string notes, DateTime occurredAt) =>
        _interactions.Add(new Interaction(Guid.NewGuid(), channel, notes, occurredAt));
}

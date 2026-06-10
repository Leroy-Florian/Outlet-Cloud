using Outlet.Crm.Domain.Organizations;
using Outlet.Crm.Domain.Payments;
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
        Money? estimatedValue,
        DateTime createdAt)
        : base(id)
    {
        ProductId = productId;
        OrganizationId = organizationId;
        Name = name;
        Email = email;
        Company = company;
        EstimatedValue = estimatedValue;
        CreatedAt = createdAt;
        Stage = ProspectStage.New;
    }

    public ProductId ProductId { get; }

    public OrganizationId? OrganizationId { get; }

    public string Name { get; }

    public Email Email { get; }

    public string? Company { get; private set; }

    /// <summary>Expected deal value; null until the prospect is qualified.</summary>
    public Money? EstimatedValue { get; private set; }

    public ProspectStage Stage { get; private set; }

    /// <summary>Why the deal was lost; only set via <see cref="Lose"/>.</summary>
    public string? LossReason { get; private set; }

    public DateTime CreatedAt { get; }

    public IReadOnlyList<Interaction> Interactions => _interactions;

    public static Result<Prospect> Create(
        ProductId productId,
        OrganizationId? organizationId,
        string name,
        Email email,
        string? company,
        Money? estimatedValue,
        DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Prospect>(ProspectErrors.NameRequired);
        }

        return Result.Success(new Prospect(
            ProspectId.New(), productId, organizationId, name.Trim(), email, company, estimatedValue, createdAt));
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

    /// <summary>
    /// PATCH semantics for the qualification fields: the estimated value and the
    /// company are both replaced (null clears). Closed prospects are immutable.
    /// </summary>
    public Result UpdateDetails(Money? estimatedValue, string? company)
    {
        if (Stage is ProspectStage.Won or ProspectStage.Lost)
        {
            return Result.Failure(ProspectErrors.AlreadyClosed);
        }

        EstimatedValue = estimatedValue;
        Company = string.IsNullOrWhiteSpace(company) ? null : company.Trim();
        return Result.Success();
    }

    /// <summary>Closes the prospect as Lost with a mandatory reason.</summary>
    public Result Lose(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(ProspectErrors.LossReasonRequired);
        }

        var advanced = Advance(ProspectStage.Lost);
        if (advanced.IsFailure)
        {
            return advanced;
        }

        LossReason = reason.Trim();
        return Result.Success();
    }

    public void RecordInteraction(string channel, string notes, DateTime occurredAt) =>
        _interactions.Add(new Interaction(Guid.NewGuid(), channel, notes, occurredAt));
}

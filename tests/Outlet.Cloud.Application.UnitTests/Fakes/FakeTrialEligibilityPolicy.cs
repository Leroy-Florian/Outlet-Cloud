using Outlet.Cloud.Application.Ports;
using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Application.UnitTests.Fakes;

/// <summary>Hand-written policy fake: eligible by default, configurable to reject.</summary>
public sealed class FakeTrialEligibilityPolicy : ITrialEligibilityPolicy
{
    private readonly string? _rejection;

    private FakeTrialEligibilityPolicy(string? rejection) => _rejection = rejection;

    public static FakeTrialEligibilityPolicy Eligible() => new(null);

    public static FakeTrialEligibilityPolicy Rejecting(string reason) => new(reason);

    public Task<Result> EnsureEligibleAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(_rejection is null ? Result.Success() : Result.Failure(_rejection));
}

using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Application.Ports;

/// <summary>
/// SECONDARY PORT — anti-abuse gate for starting a trial. Deliberately a POLICY, not a
/// constant: the frictionless (no-card) model invites abuse, but over-blocking kills early
/// adoption. The default adapter runs in SOFT enforcement (verified email, disposable-domain
/// block, one trial per domain, sign-up rate limit); the key (email vs domain vs fingerprint)
/// is tightened later as the ICP becomes known — the subscription state machine never changes.
/// </summary>
public interface ITrialEligibilityPolicy
{
    /// <summary>
    /// Succeeds when <paramref name="email"/> may start a trial, otherwise carries a
    /// user-facing reason. Implementations must not throw on a "not eligible" outcome.
    /// </summary>
    Task<Result> EnsureEligibleAsync(string email, CancellationToken cancellationToken = default);
}

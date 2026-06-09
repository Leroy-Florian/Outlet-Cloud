using Outlet.Cloud.Application.Ports;
using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Infrastructure.TrialEligibility;

/// <summary>
/// SECONDARY ADAPTER — default anti-abuse policy in SOFT enforcement. It rejects only the
/// obviously-bad (malformed e-mail, known disposable domains) and lets everything else through:
/// frictionless adoption first, observe before blocking hard. The eligibility KEY (email vs
/// domain vs fingerprint) and harder limits (one trial per domain, rate limiting) are tightened
/// here — and only here — as the ICP becomes known. The subscription state machine never moves.
/// </summary>
public sealed class DefaultTrialEligibilityPolicy : ITrialEligibilityPolicy
{
    // A starter list; sourced from a maintained set in a real deployment.
    private static readonly HashSet<string> DisposableDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "mailinator.com",
        "10minutemail.com",
        "guerrillamail.com",
        "tempmail.com",
        "yopmail.com",
    };

    public Task<Result> EnsureEligibleAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Task.FromResult(Result.Failure("An e-mail address is required to start a trial."));

        var atIndex = email.LastIndexOf('@');
        if (atIndex <= 0 || atIndex == email.Length - 1)
            return Task.FromResult(Result.Failure("Enter a valid e-mail address to start a trial."));

        var domain = email[(atIndex + 1)..];
        if (DisposableDomains.Contains(domain))
            return Task.FromResult(Result.Failure("Disposable e-mail addresses are not eligible for a trial."));

        return Task.FromResult(Result.Success());
    }
}

namespace Outlet.Crm.Domain.Analytics;

/// <summary>
/// Raw signals feeding the health score. Every nullable input means "no data":
/// the corresponding component falls back to <see cref="HealthScore.NeutralScore"/>
/// instead of unfairly punishing (or rewarding) a package we know nothing about.
/// </summary>
public sealed record HealthInputs(
    int? DaysSinceLatestRelease,
    decimal? DownloadsPercentChange,
    decimal? OpenIssuesGrowthPercent,
    decimal? StarsGrowthPercent,
    int RecentCaptureFailures);

/// <summary>Per-component sub-scores (each 0–100), the weighted total and its French label.</summary>
public sealed record ProductHealth(
    HealthInputs Inputs,
    int ReleaseFreshnessScore,
    int DownloadTrendScore,
    int RepoActivityScore,
    int SnapshotReliabilityScore,
    int Total,
    string Label);

/// <summary>
/// Pure 0–100 health score of a product, v1. Weights and curves are deliberate
/// constants (no per-product tuning yet):
/// <list type="bullet">
/// <item>RELEASE FRESHNESS (25 %) — 100 up to <see cref="FreshDays"/> days since the latest
/// observed release, then linear decay down to 0 at <see cref="StaleDays"/> days. Release
/// dates are approximated by version-change markers between snapshots (registries do not
/// expose stored publish dates in v1); no marker → neutral.</item>
/// <item>DOWNLOAD TREND (35 %) — the 30-days-vs-previous-30-days percent change mapped
/// linearly: −50 % → 0, 0 % → 50, +50 % → 100. No baseline → neutral.</item>
/// <item>REPO ACTIVITY (25 %) — starts neutral; stars growth is a bonus
/// (<see cref="StarsBonusPerPercent"/> points per %), rising open issues a penalty
/// (<see cref="IssuesPenaltyPerPercent"/> point per %). Fewer than two snapshots → neutral.</item>
/// <item>SNAPSHOT RELIABILITY (15 %) — 100 minus <see cref="ReliabilityPenaltyPerFailure"/>
/// points per capture-failure alert over the last <see cref="ReliabilityWindowDays"/> days.</item>
/// </list>
/// </summary>
public static class HealthScore
{
    public const decimal ReleaseFreshnessWeight = 0.25m;
    public const decimal DownloadTrendWeight = 0.35m;
    public const decimal RepoActivityWeight = 0.25m;
    public const decimal SnapshotReliabilityWeight = 0.15m;

    /// <summary>Component score used whenever a signal has no usable data.</summary>
    public const int NeutralScore = 50;

    /// <summary>A release younger than this many days scores a full 100.</summary>
    public const int FreshDays = 30;

    /// <summary>A release older than this many days scores 0.</summary>
    public const int StaleDays = 365;

    /// <summary>Trend points gained/lost per percent of downloads change (±50 % saturates).</summary>
    public const decimal TrendPointsPerPercent = 1m;

    /// <summary>Bonus points per percent of stars growth over the comparison window.</summary>
    public const decimal StarsBonusPerPercent = 2m;

    /// <summary>Penalty points per percent of open-issues growth over the comparison window.</summary>
    public const decimal IssuesPenaltyPerPercent = 1m;

    /// <summary>Points removed per snapshot-capture failure inside the reliability window.</summary>
    public const int ReliabilityPenaltyPerFailure = 25;

    /// <summary>Trailing window (days) in which capture failures hurt reliability.</summary>
    public const int ReliabilityWindowDays = 7;

    public const int ExcellentThreshold = 80;
    public const int GoodThreshold = 60;
    public const int WatchThreshold = 40;

    public const string ExcellentLabel = "Excellent";
    public const string GoodLabel = "Bon";
    public const string WatchLabel = "À surveiller";
    public const string StrugglingLabel = "En difficulté";

    public static ProductHealth Compute(HealthInputs inputs)
    {
        var freshness = ReleaseFreshnessScore(inputs.DaysSinceLatestRelease);
        var trend = DownloadTrendScore(inputs.DownloadsPercentChange);
        var activity = RepoActivityScore(inputs.OpenIssuesGrowthPercent, inputs.StarsGrowthPercent);
        var reliability = SnapshotReliabilityScore(inputs.RecentCaptureFailures);

        var total = (int)Math.Round(
            (freshness * ReleaseFreshnessWeight)
            + (trend * DownloadTrendWeight)
            + (activity * RepoActivityWeight)
            + (reliability * SnapshotReliabilityWeight),
            MidpointRounding.AwayFromZero);

        return new ProductHealth(inputs, freshness, trend, activity, reliability, total, Label(total));
    }

    public static int ReleaseFreshnessScore(int? daysSinceLatestRelease)
    {
        if (daysSinceLatestRelease is not { } days)
        {
            return NeutralScore;
        }

        if (days <= FreshDays)
        {
            return 100;
        }

        if (days >= StaleDays)
        {
            return 0;
        }

        return (int)Math.Round(100m * (StaleDays - days) / (StaleDays - FreshDays), MidpointRounding.AwayFromZero);
    }

    public static int DownloadTrendScore(decimal? percentChange)
    {
        if (percentChange is not { } change)
        {
            return NeutralScore;
        }

        return Clamp(NeutralScore + (change * TrendPointsPerPercent));
    }

    public static int RepoActivityScore(decimal? openIssuesGrowthPercent, decimal? starsGrowthPercent)
    {
        if (openIssuesGrowthPercent is null && starsGrowthPercent is null)
        {
            return NeutralScore;
        }

        var bonus = (starsGrowthPercent ?? 0m) * StarsBonusPerPercent;
        var penalty = Math.Max(openIssuesGrowthPercent ?? 0m, 0m) * IssuesPenaltyPerPercent;

        return Clamp(NeutralScore + bonus - penalty);
    }

    public static int SnapshotReliabilityScore(int recentCaptureFailures) =>
        Clamp(100 - (recentCaptureFailures * (decimal)ReliabilityPenaltyPerFailure));

    public static string Label(int total) =>
        total >= ExcellentThreshold ? ExcellentLabel
        : total >= GoodThreshold ? GoodLabel
        : total >= WatchThreshold ? WatchLabel
        : StrugglingLabel;

    /// <summary>
    /// Best-effort release marker: the capture time of the first snapshot whose
    /// latest version differs from the previous snapshot's (both known). Returns
    /// null when no version change was ever observed — the real publish date is
    /// then unknowable from stored data and freshness stays neutral.
    /// </summary>
    public static DateTime? LatestObservedRelease(IEnumerable<DownloadSnapshot> snapshots)
    {
        DateTime? latest = null;

        foreach (var group in snapshots.GroupBy(s => (s.Registry, s.PackageId.Value)))
        {
            List<DownloadSnapshot> ordered = [.. group.OrderBy(s => s.CapturedAt)];

            for (var i = ordered.Count - 1; i >= 1; i--)
            {
                var current = ordered[i].LatestVersion;
                var previous = ordered[i - 1].LatestVersion;

                if (current is not null && previous is not null && !string.Equals(current, previous, StringComparison.Ordinal))
                {
                    if (latest is null || ordered[i].CapturedAt > latest)
                    {
                        latest = ordered[i].CapturedAt;
                    }

                    break;
                }
            }
        }

        return latest;
    }

    /// <summary>
    /// Percent growth of a repository counter between the baseline and the latest
    /// snapshot. A zero baseline has no meaningful percentage: returns 100 when the
    /// counter appeared from nothing, 0 when it stayed at zero.
    /// </summary>
    public static decimal GrowthPercent(int baseline, int latest)
    {
        if (baseline is 0)
        {
            return latest > 0 ? 100m : 0m;
        }

        return Math.Round((latest - baseline) * 100m / baseline, 1);
    }

    private static int Clamp(decimal score) =>
        (int)Math.Round(Math.Clamp(score, 0m, 100m), MidpointRounding.AwayFromZero);
}

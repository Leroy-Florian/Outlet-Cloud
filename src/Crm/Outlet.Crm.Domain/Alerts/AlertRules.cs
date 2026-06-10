namespace Outlet.Crm.Domain.Alerts;

public sealed record AlertSignal(AlertType Type, string Message);

/// <summary>
/// Pure alert evaluation rules, v1. Thresholds are deliberate constants
/// (no per-product configuration yet):
/// <list type="bullet">
/// <item>SPIKE — today's new downloads &gt; <see cref="SpikeFactor"/> × the average of the
/// previous 7 days AND today &gt; <see cref="MinimumSpikeDownloads"/> absolute (tiny packages
/// going 2 → 5 downloads are noise, not a spike).</item>
/// <item>DROP — today's new downloads &lt; <see cref="DropFactor"/> × that average AND the
/// average itself &gt; <see cref="MinimumDropBaseline"/> (a drop only means something against
/// a real baseline).</item>
/// <item>STARS MILESTONE — the latest star count crossed a power of 10 (10, 100, 1000…)
/// or a multiple of <see cref="StarsMilestoneStep"/> since the previous snapshot.</item>
/// </list>
/// </summary>
public static class AlertRules
{
    /// <summary>Today must exceed twice the trailing 7-day average to count as a spike.</summary>
    public const decimal SpikeFactor = 2m;

    /// <summary>Today must fall below half the trailing 7-day average to count as a drop.</summary>
    public const decimal DropFactor = 0.5m;

    /// <summary>Absolute floor for spikes: today's downloads must exceed this.</summary>
    public const long MinimumSpikeDownloads = 50;

    /// <summary>Baseline floor for drops: the 7-day average must exceed this.</summary>
    public const long MinimumDropBaseline = 50;

    /// <summary>Star milestones are powers of 10 and multiples of this step.</summary>
    public const int StarsMilestoneStep = 100;

    /// <summary>
    /// Compares today's new downloads against the average of the previous 7 days.
    /// Returns a spike or drop signal, or null when downloads are within normal range.
    /// </summary>
    public static AlertSignal? EvaluateDownloads(string package, long todayDownloads, decimal previous7DayAverage)
    {
        if (todayDownloads > previous7DayAverage * SpikeFactor && todayDownloads > MinimumSpikeDownloads)
        {
            return new AlertSignal(
                AlertType.DownloadsSpike,
                $"Downloads spike on {package}: {todayDownloads} today vs {Math.Round(previous7DayAverage, 1)} avg over the previous 7 days.");
        }

        if (todayDownloads < previous7DayAverage * DropFactor && previous7DayAverage > MinimumDropBaseline)
        {
            return new AlertSignal(
                AlertType.DownloadsDrop,
                $"Downloads drop on {package}: {todayDownloads} today vs {Math.Round(previous7DayAverage, 1)} avg over the previous 7 days.");
        }

        return null;
    }

    /// <summary>
    /// Detects a star milestone crossed between two consecutive snapshots.
    /// Returns null when no milestone was crossed (or stars went down).
    /// </summary>
    public static AlertSignal? EvaluateStars(string repository, int previousStars, int latestStars)
    {
        var milestone = CrossedMilestone(previousStars, latestStars);
        if (milestone is null)
        {
            return null;
        }

        return new AlertSignal(
            AlertType.StarsMilestone,
            $"{repository} crossed {milestone} stars (now {latestStars}).");
    }

    /// <summary>
    /// The largest milestone (power of 10 ≥ 10, or multiple of <see cref="StarsMilestoneStep"/>)
    /// strictly above <paramref name="previousStars"/> and at most <paramref name="latestStars"/>.
    /// </summary>
    public static long? CrossedMilestone(int previousStars, int latestStars)
    {
        if (latestStars <= previousStars)
        {
            return null;
        }

        long? best = null;

        var hundreds = latestStars / (long)StarsMilestoneStep * StarsMilestoneStep;
        if (hundreds >= StarsMilestoneStep && hundreds > previousStars)
        {
            best = hundreds;
        }

        for (long power = 10; power <= latestStars; power *= 10)
        {
            if (power > previousStars && (best is null || power > best))
            {
                best = power;
            }
        }

        return best;
    }
}

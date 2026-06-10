namespace Outlet.Crm.Domain.Prospects;

public sealed record ProspectStageStats(
    ProspectStage Stage,
    int Count,
    decimal TotalEstimatedValue,
    decimal? ConversionRateToNext);

public sealed record ProspectPipelineReport(
    int TotalProspects,
    decimal TotalEstimatedValue,
    IReadOnlyList<ProspectStageStats> Stages);

/// <summary>
/// Aggregates the prospect list into per-stage pipeline statistics.
///
/// Conversion rates are a documented APPROXIMATION from current data only (the
/// pipeline keeps no stage history): a prospect is counted as having "ever
/// reached" stage S when its current stage is at or beyond S in the forward
/// order New → Contacted → Qualified → Won. Lost prospects only count towards
/// New (we do not know which stage they dropped out of). The rate for stage S
/// is everReached(next) / everReached(S), null when the denominator is zero,
/// rounded to three decimals.
///
/// Estimated values are summed on their raw amounts: the CRM operates in a
/// single currency, so no FX conversion is attempted.
/// </summary>
public static class ProspectPipelineStats
{
    private static readonly ProspectStage[] ForwardStages =
        [ProspectStage.New, ProspectStage.Contacted, ProspectStage.Qualified, ProspectStage.Won];

    public static ProspectPipelineReport Compute(IEnumerable<Prospect> prospects)
    {
        List<Prospect> all = [.. prospects];

        var everReached = new Dictionary<ProspectStage, int>();
        foreach (var stage in ForwardStages)
        {
            everReached[stage] = all.Count(p =>
                p.Stage == ProspectStage.Lost ? stage == ProspectStage.New : p.Stage >= stage);
        }

        List<ProspectStageStats> stages = [];
        foreach (var stage in Enum.GetValues<ProspectStage>())
        {
            List<Prospect> inStage = [.. all.Where(p => p.Stage == stage)];

            decimal? conversion = null;
            var forwardIndex = Array.IndexOf(ForwardStages, stage);
            if (forwardIndex >= 0 && forwardIndex < ForwardStages.Length - 1 && everReached[stage] > 0)
            {
                conversion = Math.Round((decimal)everReached[ForwardStages[forwardIndex + 1]] / everReached[stage], 3);
            }

            stages.Add(new ProspectStageStats(
                stage,
                inStage.Count,
                inStage.Sum(p => p.EstimatedValue?.Amount ?? 0m),
                conversion));
        }

        return new ProspectPipelineReport(
            all.Count,
            stages.Sum(s => s.TotalEstimatedValue),
            stages);
    }
}

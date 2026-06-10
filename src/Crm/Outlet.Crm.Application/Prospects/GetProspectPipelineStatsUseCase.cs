using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Prospects;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Application.Prospects;

public sealed record GetProspectPipelineStatsQuery;

/// <summary>
/// Pipeline view: per-stage counts and total estimated value, plus the
/// approximate stage-to-stage conversion rates documented on
/// <see cref="ProspectPipelineStats"/>.
/// </summary>
public sealed class GetProspectPipelineStatsUseCase(IProspectRepository prospects)
    : IUseCase<GetProspectPipelineStatsQuery, ProspectPipelineReport>
{
    public async Task<Result<ProspectPipelineReport>> HandleAsync(
        GetProspectPipelineStatsQuery command,
        CancellationToken cancellationToken = default)
    {
        var all = await prospects.ListAsync(cancellationToken);
        return Result.Success(ProspectPipelineStats.Compute(all));
    }
}

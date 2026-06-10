using System.Text.Json;
using Outlet.Crm.Domain.Analytics;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Infrastructure.PackageStats;

/// <summary>
/// Adapter over the npm downloads API. npm n'expose pas de cumul total :
/// on capture le volume glissant des 30 derniers jours (point/last-month).
/// </summary>
public sealed class NpmStatsHttpClient(HttpClient httpClient)
{
    public async Task<Result<long>> GetTotalDownloadsAsync(PackageId packageId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            $"downloads/point/last-month/{Uri.EscapeDataString(packageId.Value)}",
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Result.Failure<long>(
                $"NpmStats.PackageNotFound: Package '{packageId.Value}' was not found on npm.");
        }

        if (!response.IsSuccessStatusCode)
        {
            return Result.Failure<long>(
                $"NpmStats.HttpError: npm downloads API answered {(int)response.StatusCode} for '{packageId.Value}'.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        return Result.Success(document.RootElement.GetProperty("downloads").GetInt64());
    }
}

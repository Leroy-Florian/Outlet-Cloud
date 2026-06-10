using System.Text.Json;
using Outlet.Crm.Domain.Analytics;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Infrastructure.PackageStats;

/// <summary>Adapter over the NuGet azuresearch query API (cumulative total downloads).</summary>
public sealed class NuGetStatsHttpClient(HttpClient httpClient)
{
    public async Task<Result<long>> GetTotalDownloadsAsync(PackageId packageId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            $"query?q=packageid:{Uri.EscapeDataString(packageId.Value)}&prerelease=true",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Result.Failure<long>(
                $"NuGetStats.HttpError: NuGet search API answered {(int)response.StatusCode} for '{packageId.Value}'.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        foreach (var item in document.RootElement.GetProperty("data").EnumerateArray())
        {
            if (string.Equals(item.GetProperty("id").GetString(), packageId.Value, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Success(item.GetProperty("totalDownloads").GetInt64());
            }
        }

        return Result.Failure<long>(
            $"NuGetStats.PackageNotFound: Package '{packageId.Value}' was not found on NuGet.org.");
    }
}

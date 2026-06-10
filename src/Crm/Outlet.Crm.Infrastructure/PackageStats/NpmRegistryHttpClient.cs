using System.Text.Json;
using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Analytics;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Infrastructure.PackageStats;

/// <summary>
/// Adapter over the npm registry metadata document (registry.npmjs.org/{pkg}):
/// latest version from dist-tags.latest, publication date and version count from
/// the time map (whose "created"/"modified" entries are not versions).
/// </summary>
public sealed class NpmRegistryHttpClient(HttpClient httpClient)
{
    public async Task<Result<PackageVersionInfo>> GetVersionsAsync(PackageId packageId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(Uri.EscapeDataString(packageId.Value), cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Result.Failure<PackageVersionInfo>(
                $"NpmRegistry.PackageNotFound: Package '{packageId.Value}' was not found on npm.");
        }

        if (!response.IsSuccessStatusCode)
        {
            return Result.Failure<PackageVersionInfo>(
                $"NpmRegistry.HttpError: npm registry answered {(int)response.StatusCode} for '{packageId.Value}'.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("dist-tags", out var distTags) ||
            distTags.GetProperty("latest").GetString() is not { } latest)
        {
            return Result.Failure<PackageVersionInfo>(
                $"NpmRegistry.NoLatestTag: Package '{packageId.Value}' has no dist-tags.latest.");
        }

        DateTime? publishedAt = null;
        var versionCount = 0;
        if (document.RootElement.TryGetProperty("time", out var time))
        {
            foreach (var entry in time.EnumerateObject())
            {
                if (entry.Name is "created" or "modified")
                {
                    continue;
                }

                versionCount++;
                if (entry.Name == latest)
                {
                    publishedAt = entry.Value.GetDateTime();
                }
            }
        }

        return Result.Success(new PackageVersionInfo(latest, publishedAt, versionCount));
    }
}

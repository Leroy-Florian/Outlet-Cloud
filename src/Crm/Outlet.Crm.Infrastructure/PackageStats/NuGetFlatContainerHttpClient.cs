using System.Text.Json;
using Outlet.Crm.Application.Ports;
using Outlet.Crm.Domain.Analytics;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Infrastructure.PackageStats;

/// <summary>
/// Adapter over the NuGet flat-container version index
/// (api.nuget.org/v3-flatcontainer/{id}/index.json): the versions array is
/// ordered ascending, the latest is the last entry. The flat container exposes
/// no publication dates, so <c>PublishedAt</c> stays null for NuGet.
/// </summary>
public sealed class NuGetFlatContainerHttpClient(HttpClient httpClient)
{
    public async Task<Result<PackageVersionInfo>> GetVersionsAsync(PackageId packageId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(
            $"{Uri.EscapeDataString(packageId.Value)}/index.json",
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Result.Failure<PackageVersionInfo>(
                $"NuGetVersions.PackageNotFound: Package '{packageId.Value}' was not found on NuGet.org.");
        }

        if (!response.IsSuccessStatusCode)
        {
            return Result.Failure<PackageVersionInfo>(
                $"NuGetVersions.HttpError: NuGet flat-container answered {(int)response.StatusCode} for '{packageId.Value}'.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var versions = document.RootElement.GetProperty("versions");
        var count = versions.GetArrayLength();
        if (count is 0)
        {
            return Result.Failure<PackageVersionInfo>(
                $"NuGetVersions.NoVersions: Package '{packageId.Value}' has no published versions.");
        }

        return Result.Success(new PackageVersionInfo(versions[count - 1].GetString()!, null, count));
    }
}

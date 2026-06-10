namespace Outlet.Crm.Domain.Traffic;

/// <summary>
/// Coarse user-agent bucketing — enough for a dashboard split between humans
/// and automated traffic without shipping a UA-parsing dependency.
/// </summary>
public static class UserAgentCategory
{
    public const string Bot = "bot";

    public const string Browser = "browser";

    private static readonly string[] BotMarkers = ["bot", "crawler", "spider", "curl", "wget", "python-requests"];

    public static string? Categorize(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return null;
        }

        return BotMarkers.Any(marker => userAgent.Contains(marker, StringComparison.OrdinalIgnoreCase))
            ? Bot
            : Browser;
    }
}

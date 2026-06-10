namespace Outlet.Crm.Domain.Traffic;

/// <summary>
/// Normalizes a raw referrer (full URL or bare host) into a stable source string:
/// blank → "direct"; known hosts map to friendly names ("google", "github"…);
/// anything else keeps its lowercased host with any "www." prefix stripped.
/// </summary>
public static class ReferrerSource
{
    public const string Direct = "direct";

    public static string Normalize(string? referrer)
    {
        if (string.IsNullOrWhiteSpace(referrer))
        {
            return Direct;
        }

        var trimmed = referrer.Trim();
        var host = Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) && uri.Host.Length > 0
            ? uri.Host
            : trimmed;

        host = host.ToLowerInvariant();
        if (host.StartsWith("www.", StringComparison.Ordinal))
        {
            host = host[4..];
        }

        return MapKnownHost(host);
    }

    private static string MapKnownHost(string host)
    {
        if (host.StartsWith("google.", StringComparison.Ordinal))
        {
            return "google";
        }

        return host switch
        {
            "github.com" => "github",
            "bing.com" => "bing",
            "duckduckgo.com" => "duckduckgo",
            "news.ycombinator.com" => "hackernews",
            "reddit.com" or "old.reddit.com" => "reddit",
            "t.co" or "x.com" or "twitter.com" => "twitter",
            "linkedin.com" or "lnkd.in" => "linkedin",
            _ => host,
        };
    }
}

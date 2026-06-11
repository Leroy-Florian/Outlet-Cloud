using System.Net.Http.Json;

namespace Outlet.Cloud.Web.Composition;

/// <summary>
/// HOST-LEVEL BRIDGE — forwards end-user feedback from Outlet Cloud to the CRM host
/// over HTTP/JSON (the only allowed channel between hosts; bounded contexts stay isolated).
/// Disabled when <c>Crm:BaseUrl</c> is not configured.
/// </summary>
public sealed class CrmFeedbackForwarder(HttpClient httpClient, IConfiguration configuration, ILogger<CrmFeedbackForwarder> logger)
{
    public bool IsEnabled =>
        !string.IsNullOrWhiteSpace(configuration["Crm:BaseUrl"])
        && Guid.TryParse(configuration["Crm:FeedbackProductId"], out _);

    /// <summary>Relays a feedback to the CRM ingestion endpoint. Returns false when relaying failed.</summary>
    public async Task<bool> ForwardAsync(string category, string message, string? reporterEmail, CancellationToken cancellationToken)
    {
        var payload = new
        {
            productId = Guid.Parse(configuration["Crm:FeedbackProductId"]!),
            category,
            message,
            reporterEmail,
            sourceApp = "outlet-cloud",
        };

        try
        {
            var response = await httpClient.PostAsJsonAsync(
                new Uri(new Uri(configuration["Crm:BaseUrl"]!), "/api/feedback"), payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("CRM feedback relay answered {StatusCode}.", (int)response.StatusCode);
                return false;
            }

            return true;
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "CRM feedback relay unreachable.");
            return false;
        }
    }
}

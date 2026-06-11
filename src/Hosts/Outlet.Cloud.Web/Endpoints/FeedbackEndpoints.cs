using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Outlet.Cloud.Web.Composition;
using Outlet.Identity.Infrastructure.Persistence;

namespace Outlet.Cloud.Web.Endpoints;

/// <summary>
/// End-user feedback from the Outlet Cloud UI/CLI, relayed to the CRM host over HTTP
/// (see <see cref="CrmFeedbackForwarder"/>). Requires an authenticated session so the
/// reporter email is trustworthy; returns 503 when the CRM relay is not configured.
/// </summary>
public static class FeedbackEndpoints
{
    private static readonly string[] Categories = ["Bug", "FeatureRequest", "Question", "Other"];

    public static void MapOutletFeedback(this WebApplication app)
    {
        app.MapPost("/feedback", async (
            FeedbackRequest request,
            ClaimsPrincipal principal,
            UserManager<OutletIdentityUser> users,
            CrmFeedbackForwarder forwarder,
            CancellationToken cancellationToken) =>
        {
            var user = await users.GetUserAsync(principal);
            if (user is null)
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Message))
                return Results.BadRequest(new { error = "Feedback.MessageRequired: message must not be blank." });

            var category = Categories.FirstOrDefault(c => string.Equals(c, request.Category, StringComparison.OrdinalIgnoreCase)) ?? "Other";

            if (!forwarder.IsEnabled)
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);

            var forwarded = await forwarder.ForwardAsync(category, request.Message.Trim(), user.Email, cancellationToken);
            return forwarded
                ? Results.Accepted()
                : Results.StatusCode(StatusCodes.Status502BadGateway);
        }).RequireAuthorization();
    }
}

public sealed record FeedbackRequest(string? Category, string Message);

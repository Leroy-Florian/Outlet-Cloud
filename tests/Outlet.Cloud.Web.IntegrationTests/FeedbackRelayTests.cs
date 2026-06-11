using System.Net;
using System.Net.Http.Json;

namespace Outlet.Cloud.Web.IntegrationTests;

public sealed class FeedbackRelayTests
{
    [Fact]
    public async Task Should_RejectFeedback_When_NotAuthenticated()
    {
        using var factory = new OutletCloudAppFactory();
        var client = factory.Migrated().CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/feedback", new { category = "Bug", message = "outlet add plante" });

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Redirect, HttpStatusCode.Found);
    }

    [Fact]
    public async Task Should_AnswerServiceUnavailable_When_CrmRelayIsNotConfigured()
    {
        using var factory = new OutletCloudAppFactory();
        var client = factory.Migrated().CreateClient();

        await client.PostAsJsonAsync(
            "/auth/register", new { email = "carol@acme.test", password = "Str0ng!pwd", displayName = "Carol" });

        var response = await client.PostAsJsonAsync("/feedback", new { category = "Bug", message = "outlet add plante" });

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Should_RejectFeedback_When_MessageIsBlank()
    {
        using var factory = new OutletCloudAppFactory();
        var client = factory.Migrated().CreateClient();

        await client.PostAsJsonAsync(
            "/auth/register", new { email = "dave@acme.test", password = "Str0ng!pwd", displayName = "Dave" });

        var response = await client.PostAsJsonAsync("/feedback", new { category = "Bug", message = "  " });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

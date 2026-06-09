using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Outlet.Cloud.Web.IntegrationTests;

public sealed class PaywallAndResetTests
{
    [Fact]
    public async Task Should_GateManagementUi_OnSubscriptionStatus()
    {
        using var factory = new OutletCloudAppFactory().Migrated();
        var client = factory.CreateClient();

        (await client.PostAsJsonAsync("/auth/register", new { email = "dev@acme.test", password = "Str0ng!pwd", displayName = "Dev" }))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        // Frictionless trial: full Pro access immediately, no card.
        (await client.PostAsJsonAsync("/organizations", new { slug = "acme", name = "Acme" }))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        // Convert (mock checkout) keeps access; cancel suspends -> management goes read-only (402).
        (await client.PostAsync("/billing/subscribe", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsync("/billing/cancel", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        (await client.PostAsJsonAsync("/organizations", new { slug = "beta", name = "Beta" }))
            .StatusCode.Should().Be(HttpStatusCode.PaymentRequired);
    }

    [Fact]
    public async Task Should_ResetPassword_AndLoginWithNewPassword()
    {
        using var factory = new OutletCloudAppFactory().Migrated();
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/auth/register", new { email = "carol@acme.test", password = "Old!pass1", displayName = "Carol" });

        var forgotResponse = await client.PostAsJsonAsync("/auth/forgot-password", new { email = "carol@acme.test" });
        var forgot = await forgotResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = forgot.GetProperty("token").GetString()!;

        (await client.PostAsJsonAsync("/auth/reset-password", new { email = "carol@acme.test", token, newPassword = "N3w!pass2" }))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var fresh = factory.CreateClient();
        (await fresh.PostAsJsonAsync("/auth/login", new { email = "carol@acme.test", password = "N3w!pass2" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await fresh.PostAsJsonAsync("/auth/login", new { email = "carol@acme.test", password = "Old!pass1" }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

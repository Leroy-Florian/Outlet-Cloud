using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Outlet.Cloud.Application.Ports;
using Outlet.Cloud.Domain.Subscriptions;

namespace Outlet.Cloud.Web.IntegrationTests;

/// <summary>
/// Public registries: an org may open its registry to anonymous pulls; publishing and
/// the owner-plan gates (Suspended readable, Expired blocked) stay exactly as before.
/// </summary>
public sealed class PublicRegistryTests
{
    [Fact]
    public async Task Should_ServeRegistryAnonymously_When_OrganizationIsPublic()
    {
        using var factory = new OutletCloudAppFactory().Migrated();
        var owner = factory.CreateClient();
        await Register(owner, "owner@acme.test");
        var orgId = await CreateOrg(owner, "acme");
        await Publish(owner, orgId, "email-smtp", "SmtpEmailSender.cs", "public sealed class SmtpEmailSender {}");

        (await SetVisibility(owner, orgId, "Public")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var anonymous = factory.CreateClient();
        var index = await anonymous.GetAsync("/organizations/acme/registry.json");
        index.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = (await index.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items");
        items.EnumerateArray().Select(i => i.GetProperty("name").GetString()).Should().Contain("email-smtp");

        var file = await anonymous.GetAsync("/organizations/acme/email-smtp/SmtpEmailSender.cs");
        file.StatusCode.Should().Be(HttpStatusCode.OK);
        (await file.Content.ReadAsStringAsync()).Should().Contain("SmtpEmailSender");
    }

    [Fact]
    public async Task Should_RejectAnonymousPull_When_OrganizationIsPrivate()
    {
        using var factory = new OutletCloudAppFactory().Migrated();
        var owner = factory.CreateClient();
        await Register(owner, "owner@acme.test");
        var orgId = await CreateOrg(owner, "acme");
        await Publish(owner, orgId, "email-smtp", "x.cs", "// x");

        var anonymous = factory.CreateClient();

        (await anonymous.GetAsync("/organizations/acme/registry.json"))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await anonymous.GetAsync("/organizations/acme/email-smtp/x.cs"))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Should_ForbidVisibilityChange_When_CallerIsAPlainMember()
    {
        using var factory = new OutletCloudAppFactory().Migrated();
        var owner = factory.CreateClient();
        await Register(owner, "owner@acme.test");
        var orgId = await CreateOrg(owner, "acme");

        var member = factory.CreateClient();
        await Register(member, "bob@acme.test");
        await owner.PostAsJsonAsync($"/organizations/{orgId}/members", new { email = "bob@acme.test", role = "Member" });

        (await SetVisibility(member, orgId, "Public")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Should_AllowVisibilityChange_When_CallerIsAdmin()
    {
        using var factory = new OutletCloudAppFactory().Migrated();
        var owner = factory.CreateClient();
        await Register(owner, "owner@acme.test");
        var orgId = await CreateOrg(owner, "acme");

        var admin = factory.CreateClient();
        await Register(admin, "ann@acme.test");
        await owner.PostAsJsonAsync($"/organizations/{orgId}/members", new { email = "ann@acme.test", role = "Admin" });

        (await SetVisibility(admin, orgId, "Public")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = await (await owner.GetAsync($"/organizations/{orgId}")).Content.ReadFromJsonAsync<JsonElement>();
        detail.GetProperty("registryVisibility").GetString().Should().Be("Public");
    }

    [Fact]
    public async Task Should_KeepPublicRegistryReadable_When_OwnerIsSuspended()
    {
        using var factory = new OutletCloudAppFactory().Migrated();
        var owner = factory.CreateClient();
        await Register(owner, "owner@acme.test");
        var orgId = await CreateOrg(owner, "acme");
        await Publish(owner, orgId, "email-smtp", "x.cs", "// x");
        await SetVisibility(owner, orgId, "Public");

        (await owner.PostAsync("/billing/cancel", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        var anonymous = factory.CreateClient();
        (await anonymous.GetAsync("/organizations/acme/registry.json"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await anonymous.GetAsync("/organizations/acme/email-smtp/x.cs"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Should_BlockPublicRegistry_When_OwnerIsExpired()
    {
        using var factory = new OutletCloudAppFactory().Migrated();
        var owner = factory.CreateClient();
        var ownerId = await Register(owner, "owner@acme.test");
        var orgId = await CreateOrg(owner, "acme");
        await Publish(owner, orgId, "email-smtp", "x.cs", "// x");
        await SetVisibility(owner, orgId, "Public");

        (await owner.PostAsync("/billing/cancel", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        await PurgeSubscription(factory, ownerId);

        var anonymous = factory.CreateClient();
        (await anonymous.GetAsync("/organizations/acme/registry.json"))
            .StatusCode.Should().Be(HttpStatusCode.PaymentRequired);
        (await anonymous.GetAsync("/organizations/acme/email-smtp/x.cs"))
            .StatusCode.Should().Be(HttpStatusCode.PaymentRequired);
    }

    /// <summary>Drives the retention timeline server-side: Suspended → Expired (purged).</summary>
    private static async Task PurgeSubscription(OutletCloudAppFactory factory, Guid accountId)
    {
        using var scope = factory.Services.CreateScope();
        var subscriptions = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
        var subscription = await subscriptions.GetByAccountAsync(AccountId.From(accountId));
        subscription!.Purge().IsSuccess.Should().BeTrue();
        await subscriptions.UpdateAsync(subscription);
    }

    private static async Task<Guid> Register(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/auth/register",
            new { email, password = "Str0ng!pwd", displayName = email.Split('@')[0] });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var userId = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("userId").GetGuid();
        (await client.PostAsync("/billing/subscribe", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        return userId;
    }

    private static async Task<Guid> CreateOrg(HttpClient client, string slug)
    {
        var response = await client.PostAsJsonAsync("/organizations", new { slug, name = slug });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("organizationId").GetGuid();
    }

    private static Task<HttpResponseMessage> SetVisibility(HttpClient client, Guid orgId, string visibility) =>
        client.PutAsJsonAsync($"/organizations/{orgId}/registry/visibility", new { visibility });

    private static Task<HttpResponseMessage> Publish(HttpClient client, Guid orgId, string name, string path, string content) =>
        client.PostAsJsonAsync($"/organizations/{orgId}/registry/items", new
        {
            name,
            manifest = new
            {
                name,
                type = "outlet:adapter",
                concern = "email",
                targetFrameworks = new[] { "net10.0" },
                files = new[] { new { path, target = "adapter" } },
            },
            files = new[] { new { path, content } },
        });
}

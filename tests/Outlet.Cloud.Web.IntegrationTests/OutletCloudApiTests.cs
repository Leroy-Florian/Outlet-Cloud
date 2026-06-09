using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Outlet.Cloud.Web.IntegrationTests;

/// <summary>End-to-end management flows over the real host (cookie session + bearer registry).</summary>
public sealed class OutletCloudApiTests
{
    [Fact]
    public async Task Should_CreateOrg_IssueToken_AccessRegistry()
    {
        using var factory = new OutletCloudAppFactory().Migrated();
        var owner = factory.CreateClient();

        await Register(owner, "owner@acme.test");
        var orgId = await CreateOrg(owner, "acme");
        var (_, secret) = await IssueToken(owner, orgId);

        (await Registry(factory.CreateClient(), "acme", secret)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Should_Reject_Registry_When_TokenIsInvalid()
    {
        using var factory = new OutletCloudAppFactory().Migrated();

        var response = await Registry(factory.CreateClient(), "acme", "outlet_pat_garbage");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Should_Forbid_Member_FromManagingMembers()
    {
        using var factory = new OutletCloudAppFactory().Migrated();
        var owner = factory.CreateClient();
        await Register(owner, "owner@acme.test");
        var orgId = await CreateOrg(owner, "acme");

        var member = factory.CreateClient();
        await Register(member, "bob@acme.test");
        (await AddMember(owner, orgId, "bob@acme.test", "Member")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var attempt = await AddMember(member, orgId, "carol@acme.test", "Member");

        attempt.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Should_Reject_ChangingOwnRole()
    {
        using var factory = new OutletCloudAppFactory().Migrated();
        var owner = factory.CreateClient();
        var ownerId = await Register(owner, "owner@acme.test");
        var orgId = await CreateOrg(owner, "acme");

        var response = await owner.PutAsJsonAsync($"/organizations/{orgId}/members/{ownerId}", new { role = "Admin" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Should_RevokeMemberTokens_When_Removed()
    {
        using var factory = new OutletCloudAppFactory().Migrated();
        var owner = factory.CreateClient();
        await Register(owner, "owner@acme.test");
        var orgId = await CreateOrg(owner, "acme");

        var member = factory.CreateClient();
        var bobId = await Register(member, "bob@acme.test");
        await AddMember(owner, orgId, "bob@acme.test", "Member");

        var (_, secret) = await IssueToken(member, orgId);
        (await Registry(factory.CreateClient(), "acme", secret)).StatusCode.Should().Be(HttpStatusCode.OK);

        (await owner.DeleteAsync($"/organizations/{orgId}/members/{bobId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await Registry(factory.CreateClient(), "acme", secret)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Should_AddMemberByEmail_AndListThem()
    {
        using var factory = new OutletCloudAppFactory().Migrated();
        var owner = factory.CreateClient();
        await Register(owner, "owner@acme.test");
        var orgId = await CreateOrg(owner, "acme");

        var member = factory.CreateClient();
        await Register(member, "bob@acme.test");
        await AddMember(owner, orgId, "bob@acme.test", "Admin");

        var detail = await (await owner.GetAsync($"/organizations/{orgId}")).Content.ReadFromJsonAsync<JsonElement>();
        var emails = detail.GetProperty("members").EnumerateArray().Select(m => m.GetProperty("email").GetString());
        emails.Should().Contain("bob@acme.test");
    }

    private static async Task<Guid> Register(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/auth/register",
            new { email, password = "Str0ng!pwd", displayName = email.Split('@')[0] });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var userId = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("userId").GetGuid();
        // The web UI is Pro-only; subscribe so the management endpoints are reachable.
        (await client.PostAsync("/billing/subscribe", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        return userId;
    }

    private static async Task<Guid> CreateOrg(HttpClient client, string slug)
    {
        var response = await client.PostAsJsonAsync("/organizations", new { slug, name = slug });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("organizationId").GetGuid();
    }

    private static Task<HttpResponseMessage> AddMember(HttpClient client, Guid orgId, string email, string role) =>
        client.PostAsJsonAsync($"/organizations/{orgId}/members", new { email, role });

    private static async Task<(Guid TokenId, string Secret)> IssueToken(HttpClient client, Guid orgId, string name = "ci")
    {
        var response = await client.PostAsJsonAsync($"/organizations/{orgId}/tokens", new { name });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (body.GetProperty("tokenId").GetGuid(), body.GetProperty("secret").GetString()!);
    }

    private static async Task<HttpResponseMessage> Registry(HttpClient client, string slug, string secret)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/organizations/{slug}/registry.json");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);
        return await client.SendAsync(request);
    }
}

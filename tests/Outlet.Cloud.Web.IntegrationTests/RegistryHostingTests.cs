using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Outlet.Cloud.Web.IntegrationTests;

/// <summary>The private-registry loop: an org publishes an item, then a PAT-scoped client pulls it.</summary>
public sealed class RegistryHostingTests
{
    [Fact]
    public async Task Should_PublishItem_AndServeItToAScopedToken()
    {
        using var factory = new OutletCloudAppFactory().Migrated();
        var owner = factory.CreateClient();
        await Register(owner, "owner@acme.test");
        var orgId = await CreateOrg(owner, "acme");

        (await Publish(owner, orgId, "email-smtp", "SmtpEmailSender.cs", "public sealed class SmtpEmailSender {}"))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var (_, secret) = await IssueToken(owner, orgId);
        var puller = factory.CreateClient();

        var index = await BearerGet(puller, "/organizations/acme/registry.json", secret);
        index.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = (await index.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items");
        items.EnumerateArray().Select(i => i.GetProperty("name").GetString()).Should().Contain("email-smtp");

        var file = await BearerGet(puller, "/organizations/acme/email-smtp/SmtpEmailSender.cs", secret);
        file.StatusCode.Should().Be(HttpStatusCode.OK);
        (await file.Content.ReadAsStringAsync()).Should().Contain("SmtpEmailSender");
    }

    [Fact]
    public async Task Should_Forbid_Member_FromPublishing()
    {
        using var factory = new OutletCloudAppFactory().Migrated();
        var owner = factory.CreateClient();
        await Register(owner, "owner@acme.test");
        var orgId = await CreateOrg(owner, "acme");

        var member = factory.CreateClient();
        await Register(member, "bob@acme.test");
        await owner.PostAsJsonAsync($"/organizations/{orgId}/members", new { email = "bob@acme.test", role = "Member" });

        var response = await Publish(member, orgId, "email-smtp", "x.cs", "// x");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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

    private static async Task<(Guid TokenId, string Secret)> IssueToken(HttpClient client, Guid orgId, string name = "ci")
    {
        var response = await client.PostAsJsonAsync($"/organizations/{orgId}/tokens", new { name });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (body.GetProperty("tokenId").GetGuid(), body.GetProperty("secret").GetString()!);
    }

    private static async Task<HttpResponseMessage> BearerGet(HttpClient client, string path, string secret)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);
        return await client.SendAsync(request);
    }
}

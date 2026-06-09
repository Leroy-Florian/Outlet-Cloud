using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Outlet.Cloud.Web.IntegrationTests;

public sealed class AuthApiTests
{
    [Fact]
    public async Task Should_RegisterThenLogin_WithPassword()
    {
        using var factory = new OutletCloudAppFactory();
        var client = factory.Migrated().CreateClient();

        var register = await client.PostAsJsonAsync(
            "/auth/register", new { email = "alice@acme.test", password = "Str0ng!pwd", displayName = "Alice" });
        register.StatusCode.Should().Be(HttpStatusCode.Created);

        var login = await client.PostAsJsonAsync(
            "/auth/login", new { email = "alice@acme.test", password = "Str0ng!pwd" });
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("displayName").GetString().Should().Be("Alice");
    }

    [Fact]
    public async Task Should_Reject_When_PasswordIsWrong()
    {
        using var factory = new OutletCloudAppFactory();
        var client = factory.Migrated().CreateClient();

        await client.PostAsJsonAsync(
            "/auth/register", new { email = "bob@acme.test", password = "Str0ng!pwd", displayName = "Bob" });

        var login = await client.PostAsJsonAsync(
            "/auth/login", new { email = "bob@acme.test", password = "wrong" });

        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

using Outlet.Identity.Domain.Users;
using Outlet.Identity.Infrastructure.Persistence;

namespace Outlet.Identity.Infrastructure.UnitTests;

public sealed class EfUserRepositoryTests : IdentityDataContextFixture
{
    [Fact]
    public async Task Should_RoundTrip_User()
    {
        var user = User.Create(UserId.From(Guid.NewGuid()), EmailAddress.From("alice@example.com"), "Alice").Value!;
        await new EfUserRepository(NewContext()).AddAsync(user);

        var loaded = await new EfUserRepository(NewContext()).GetByIdAsync(user.Id);

        loaded.Should().NotBeNull();
        loaded!.Email.Value.Should().Be("alice@example.com");
        loaded.DisplayName.Should().Be("Alice");
    }

    [Fact]
    public async Task Should_ReportEmailExists_CaseInsensitively()
    {
        var user = User.Create(UserId.From(Guid.NewGuid()), EmailAddress.From("alice@example.com"), "Alice").Value!;
        await new EfUserRepository(NewContext()).AddAsync(user);

        var exists = await new EfUserRepository(NewContext()).ExistsWithEmailAsync(EmailAddress.From("ALICE@example.com"));

        exists.Should().BeTrue();
    }
}

using Outlet.Identity.Application.UnitTests.Fakes;
using Outlet.Identity.Application.Users;
using Outlet.Identity.Domain.Users;

namespace Outlet.Identity.Application.UnitTests.Users;

public sealed class RegisterUserUseCaseTests
{
    private readonly FakeUserRepository _users = new();

    [Fact]
    public async Task Should_RegisterAndPersist_When_EmailIsFree()
    {
        var useCase = new RegisterUserUseCase(_users);

        var result = await useCase.HandleAsync(new RegisterUserCommand(Guid.NewGuid(), "alice@example.com", "Alice"));

        result.IsSuccess.Should().BeTrue();
        (await _users.GetByIdAsync(UserId.From(result.Value))).Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Fail_When_EmailAlreadyExists()
    {
        _users.Seed(User.Create(UserId.From(Guid.NewGuid()), EmailAddress.From("alice@example.com"), "Alice").Value!);
        var useCase = new RegisterUserUseCase(_users);

        var result = await useCase.HandleAsync(new RegisterUserCommand(Guid.NewGuid(), "Alice@Example.com", "Alice II"));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_EmailIsInvalid()
    {
        var useCase = new RegisterUserUseCase(_users);

        var result = await useCase.HandleAsync(new RegisterUserCommand(Guid.NewGuid(), "not-an-email", "Alice"));

        result.IsFailure.Should().BeTrue();
    }
}

using Outlet.Crm.Application.Organizations;
using Outlet.Crm.Application.UnitTests.Fakes;
using Outlet.Crm.Domain.Organizations;

namespace Outlet.Crm.Application.UnitTests.Organizations;

public sealed class CreateOrganizationUseCaseTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeOrganizationRepository _repository = new();

    private CreateOrganizationUseCase UseCase => new(_repository, new FixedClock(Now));

    [Fact]
    public async Task Should_PersistOrganization_When_CommandIsValid()
    {
        var result = await UseCase.HandleAsync(
            new CreateOrganizationCommand("Acme Corp", "https://acme.example"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var stored = _repository.Items.Should().ContainSingle().Subject;
        stored.Id.Should().Be(result.Value);
        stored.Name.Should().Be("Acme Corp");
    }

    [Fact]
    public async Task Should_Fail_When_NameIsBlank()
    {
        var result = await UseCase.HandleAsync(new CreateOrganizationCommand("  ", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.NameRequired);
        _repository.Items.Should().BeEmpty();
    }
}

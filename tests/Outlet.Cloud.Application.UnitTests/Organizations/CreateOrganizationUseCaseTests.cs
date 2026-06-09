using Outlet.Cloud.Application.Organizations;
using Outlet.Cloud.Application.UnitTests.Fakes;
using Outlet.Cloud.Domain.Organizations;

namespace Outlet.Cloud.Application.UnitTests.Organizations;

public sealed class CreateOrganizationUseCaseTests
{
    private readonly FakeOrganizationRepository _organizations = new();

    private CreateOrganizationUseCase NewUseCase() => new(_organizations);

    [Fact]
    public async Task Should_CreateAndPersist_When_SlugIsFree()
    {
        var result = await NewUseCase().HandleAsync(
            new CreateOrganizationCommand(Guid.NewGuid(), "acme", "Acme Corp", Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        (await _organizations.GetByIdAsync(OrganizationId.From(result.Value))).Should().NotBeNull();
    }

    [Fact]
    public async Task Should_SeedOwnerMembership_When_Created()
    {
        var ownerId = Guid.NewGuid();

        var result = await NewUseCase().HandleAsync(
            new CreateOrganizationCommand(Guid.NewGuid(), "acme", "Acme Corp", ownerId));

        var organization = await _organizations.GetByIdAsync(OrganizationId.From(result.Value));
        organization!.Memberships.Should().ContainSingle(m =>
            m.Id == MemberUserId.From(ownerId) && m.Role == OrganizationRole.Owner);
    }

    [Fact]
    public async Task Should_Fail_When_SlugAlreadyExists()
    {
        await NewUseCase().HandleAsync(new CreateOrganizationCommand(Guid.NewGuid(), "acme", "Acme Corp", Guid.NewGuid()));

        var result = await NewUseCase().HandleAsync(
            new CreateOrganizationCommand(Guid.NewGuid(), "ACME", "Acme Two", Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_SlugIsInvalid()
    {
        var result = await NewUseCase().HandleAsync(
            new CreateOrganizationCommand(Guid.NewGuid(), "Not A Slug", "Acme", Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
    }
}

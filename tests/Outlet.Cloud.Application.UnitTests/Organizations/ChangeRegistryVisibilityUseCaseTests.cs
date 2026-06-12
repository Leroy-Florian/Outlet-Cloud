using Outlet.Cloud.Application.Organizations;
using Outlet.Cloud.Application.UnitTests.Fakes;
using Outlet.Cloud.Domain.Organizations;

namespace Outlet.Cloud.Application.UnitTests.Organizations;

public sealed class ChangeRegistryVisibilityUseCaseTests
{
    private readonly FakeOrganizationRepository _organizations = new();
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _ownerId = Guid.NewGuid();

    private ChangeRegistryVisibilityUseCase NewUseCase() => new(_organizations);

    private Organization SeedOrg()
    {
        var org = Organization.Create(
            OrganizationId.From(_orgId),
            OrganizationSlug.From("acme"),
            OrganizationName.From("Acme Corp"),
            MemberUserId.From(_ownerId)).Value!;
        _organizations.Seed(org);
        return org;
    }

    private Guid SeedMember(Organization org, OrganizationRole role)
    {
        var userId = Guid.NewGuid();
        org.AddMember(MemberUserId.From(userId), role);
        return userId;
    }

    [Fact]
    public async Task Should_MakeRegistryPublic_When_ActorIsOwner()
    {
        SeedOrg();

        var result = await NewUseCase().HandleAsync(
            new ChangeRegistryVisibilityCommand(_orgId, _ownerId, RegistryVisibility.Public));

        result.IsSuccess.Should().BeTrue();
        var org = await _organizations.GetByIdAsync(OrganizationId.From(_orgId));
        org!.RegistryVisibility.Should().Be(RegistryVisibility.Public);
        _organizations.UpdateCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_MakeRegistryPublic_When_ActorIsAdmin()
    {
        var org = SeedOrg();
        var adminId = SeedMember(org, OrganizationRole.Admin);

        var result = await NewUseCase().HandleAsync(
            new ChangeRegistryVisibilityCommand(_orgId, adminId, RegistryVisibility.Public));

        result.IsSuccess.Should().BeTrue();
        org.RegistryVisibility.Should().Be(RegistryVisibility.Public);
    }

    [Fact]
    public async Task Should_Fail_When_ActorIsAPlainMember()
    {
        var org = SeedOrg();
        var memberId = SeedMember(org, OrganizationRole.Member);

        var result = await NewUseCase().HandleAsync(
            new ChangeRegistryVisibilityCommand(_orgId, memberId, RegistryVisibility.Public));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Only owners and admins can change registry visibility.");
        org.RegistryVisibility.Should().Be(RegistryVisibility.Private);
    }

    [Fact]
    public async Task Should_Fail_When_ActorIsNotAMember()
    {
        var org = SeedOrg();

        var result = await NewUseCase().HandleAsync(
            new ChangeRegistryVisibilityCommand(_orgId, Guid.NewGuid(), RegistryVisibility.Public));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Only owners and admins can change registry visibility.");
        org.RegistryVisibility.Should().Be(RegistryVisibility.Private);
    }

    [Fact]
    public async Task Should_Fail_When_OrganizationDoesNotExist()
    {
        var result = await NewUseCase().HandleAsync(
            new ChangeRegistryVisibilityCommand(_orgId, _ownerId, RegistryVisibility.Public));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be($"Organization '{_orgId}' was not found.");
    }

    [Fact]
    public async Task Should_Fail_When_OrganizationIdIsEmpty()
    {
        var result = await NewUseCase().HandleAsync(
            new ChangeRegistryVisibilityCommand(Guid.Empty, _ownerId, RegistryVisibility.Public));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Organization id is invalid.");
    }

    [Fact]
    public async Task Should_Fail_When_ActorUserIdIsEmpty()
    {
        SeedOrg();

        var result = await NewUseCase().HandleAsync(
            new ChangeRegistryVisibilityCommand(_orgId, Guid.Empty, RegistryVisibility.Public));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("User id is invalid.");
    }

    [Fact]
    public async Task Should_PersistTheOrganization_When_VisibilityIsAlreadyInForce()
    {
        SeedOrg();
        await NewUseCase().HandleAsync(new ChangeRegistryVisibilityCommand(_orgId, _ownerId, RegistryVisibility.Public));

        var result = await NewUseCase().HandleAsync(
            new ChangeRegistryVisibilityCommand(_orgId, _ownerId, RegistryVisibility.Public));

        result.IsSuccess.Should().BeTrue();
        var org = await _organizations.GetByIdAsync(OrganizationId.From(_orgId));
        org!.RegistryVisibility.Should().Be(RegistryVisibility.Public);
        org.DomainEvents.OfType<RegistryVisibilityChangedEvent>().Should().ContainSingle();
        _organizations.UpdateCount.Should().Be(2);
    }
}

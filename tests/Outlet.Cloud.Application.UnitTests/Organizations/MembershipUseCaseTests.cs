using Outlet.Cloud.Application.Organizations;
using Outlet.Cloud.Application.UnitTests.Fakes;
using Outlet.Cloud.Domain.Organizations;

namespace Outlet.Cloud.Application.UnitTests.Organizations;

public sealed class MembershipUseCaseTests
{
    private readonly FakeOrganizationRepository _organizations = new();
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _ownerId = Guid.NewGuid();

    private void SeedOrg() =>
        _organizations.Seed(Organization.Create(
            OrganizationId.From(_orgId),
            OrganizationSlug.From("acme"),
            OrganizationName.From("Acme Corp"),
            MemberUserId.From(_ownerId)).Value!);

    [Fact]
    public async Task Should_AddMember_When_OrganizationExists()
    {
        SeedOrg();
        var newMember = Guid.NewGuid();

        var result = await new AddMemberUseCase(_organizations).HandleAsync(
            new AddMemberCommand(_orgId, newMember, OrganizationRole.Member));

        result.IsSuccess.Should().BeTrue();
        var org = await _organizations.GetByIdAsync(OrganizationId.From(_orgId));
        org!.Memberships.Should().HaveCount(2);
    }

    [Fact]
    public async Task Should_Fail_When_OrganizationDoesNotExist()
    {
        var result = await new AddMemberUseCase(_organizations).HandleAsync(
            new AddMemberCommand(_orgId, Guid.NewGuid(), OrganizationRole.Member));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_DemotingTheLastOwner()
    {
        SeedOrg();

        var result = await new ChangeMemberRoleUseCase(_organizations).HandleAsync(
            new ChangeMemberRoleCommand(_orgId, _ownerId, OrganizationRole.Admin));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_RemovingTheLastOwner()
    {
        SeedOrg();

        var result = await new RemoveMemberUseCase(_organizations).HandleAsync(
            new RemoveMemberCommand(_orgId, _ownerId));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_RemoveMember_When_NotTheLastOwner()
    {
        SeedOrg();
        var member = Guid.NewGuid();
        await new AddMemberUseCase(_organizations).HandleAsync(new AddMemberCommand(_orgId, member, OrganizationRole.Member));

        var result = await new RemoveMemberUseCase(_organizations).HandleAsync(new RemoveMemberCommand(_orgId, member));

        result.IsSuccess.Should().BeTrue();
        var org = await _organizations.GetByIdAsync(OrganizationId.From(_orgId));
        org!.Memberships.Should().ContainSingle();
        _organizations.UpdateCount.Should().Be(2);
    }

    [Fact]
    public async Task Should_ChangeRole_When_MemberExists()
    {
        SeedOrg();
        var member = Guid.NewGuid();
        await new AddMemberUseCase(_organizations).HandleAsync(new AddMemberCommand(_orgId, member, OrganizationRole.Member));

        var result = await new ChangeMemberRoleUseCase(_organizations).HandleAsync(
            new ChangeMemberRoleCommand(_orgId, member, OrganizationRole.Admin));

        result.IsSuccess.Should().BeTrue();
        var org = await _organizations.GetByIdAsync(OrganizationId.From(_orgId));
        org!.Memberships.Should().Contain(m => m.Id == MemberUserId.From(member) && m.Role == OrganizationRole.Admin);
        _organizations.UpdateCount.Should().Be(2);
    }

    [Fact]
    public async Task Should_Fail_When_AddingWithAnEmptyOrganizationId()
    {
        var result = await new AddMemberUseCase(_organizations).HandleAsync(
            new AddMemberCommand(Guid.Empty, Guid.NewGuid(), OrganizationRole.Member));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Organization id is invalid.");
    }

    [Fact]
    public async Task Should_Fail_When_AddingWithAnEmptyUserId()
    {
        SeedOrg();

        var result = await new AddMemberUseCase(_organizations).HandleAsync(
            new AddMemberCommand(_orgId, Guid.Empty, OrganizationRole.Member));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("User id is invalid.");
    }

    [Fact]
    public async Task Should_Fail_When_ChangingRoleWithAnEmptyOrganizationId()
    {
        var result = await new ChangeMemberRoleUseCase(_organizations).HandleAsync(
            new ChangeMemberRoleCommand(Guid.Empty, Guid.NewGuid(), OrganizationRole.Admin));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Organization id is invalid.");
    }

    [Fact]
    public async Task Should_Fail_When_ChangingRoleWithAnEmptyUserId()
    {
        SeedOrg();

        var result = await new ChangeMemberRoleUseCase(_organizations).HandleAsync(
            new ChangeMemberRoleCommand(_orgId, Guid.Empty, OrganizationRole.Admin));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("User id is invalid.");
    }

    [Fact]
    public async Task Should_Fail_When_ChangingRoleInAMissingOrganization()
    {
        var result = await new ChangeMemberRoleUseCase(_organizations).HandleAsync(
            new ChangeMemberRoleCommand(_orgId, Guid.NewGuid(), OrganizationRole.Admin));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("was not found");
    }

    [Fact]
    public async Task Should_Fail_When_RemovingWithAnEmptyOrganizationId()
    {
        var result = await new RemoveMemberUseCase(_organizations).HandleAsync(
            new RemoveMemberCommand(Guid.Empty, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Organization id is invalid.");
    }

    [Fact]
    public async Task Should_Fail_When_RemovingWithAnEmptyUserId()
    {
        SeedOrg();

        var result = await new RemoveMemberUseCase(_organizations).HandleAsync(
            new RemoveMemberCommand(_orgId, Guid.Empty));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("User id is invalid.");
    }

    [Fact]
    public async Task Should_Fail_When_RemovingFromAMissingOrganization()
    {
        var result = await new RemoveMemberUseCase(_organizations).HandleAsync(
            new RemoveMemberCommand(_orgId, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("was not found");
    }
}

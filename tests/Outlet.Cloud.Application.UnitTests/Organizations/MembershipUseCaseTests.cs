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
    }
}

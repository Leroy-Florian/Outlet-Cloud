using Outlet.Cloud.Domain.Organizations;

namespace Outlet.Cloud.Domain.UnitTests.Organizations;

public sealed class OrganizationTests
{
    private static readonly OrganizationId AnyId = OrganizationId.From(Guid.NewGuid());
    private static readonly OrganizationSlug AnySlug = OrganizationSlug.From("acme");
    private static readonly OrganizationName AnyName = OrganizationName.From("Acme Corp");
    private static readonly MemberUserId Owner = MemberUserId.From(Guid.NewGuid());
    private static readonly MemberUserId Other = MemberUserId.From(Guid.NewGuid());

    private static Organization NewOrg() =>
        Organization.Create(AnyId, AnySlug, AnyName, Owner).Value!;

    [Fact]
    public void Should_StartWithOwnerMembership_When_Created()
    {
        var org = NewOrg();

        org.Memberships.Should().ContainSingle();
        org.Memberships[0].Id.Should().Be(Owner);
        org.Memberships[0].Role.Should().Be(OrganizationRole.Owner);
        org.DomainEvents.Should().ContainSingle(e => e is OrganizationCreatedEvent);
    }

    [Fact]
    public void Should_AddMember_When_UserIsNotYetAMember()
    {
        var org = NewOrg();

        var result = org.AddMember(Other, OrganizationRole.Member);

        result.IsSuccess.Should().BeTrue();
        org.Memberships.Should().HaveCount(2);
        org.DomainEvents.Should().Contain(e => e is MemberAddedEvent);
    }

    [Fact]
    public void Should_Fail_When_AddingAnExistingMember()
    {
        var org = NewOrg();
        org.AddMember(Other, OrganizationRole.Member);

        var result = org.AddMember(Other, OrganizationRole.Admin);

        result.IsFailure.Should().BeTrue();
        org.Memberships.Should().HaveCount(2);
    }

    [Fact]
    public void Should_Fail_When_RemovingTheLastOwner()
    {
        var org = NewOrg();

        var result = org.RemoveMember(Owner);

        result.IsFailure.Should().BeTrue();
        org.Memberships.Should().ContainSingle();
    }

    [Fact]
    public void Should_Fail_When_DemotingTheLastOwner()
    {
        var org = NewOrg();

        var result = org.ChangeRole(Owner, OrganizationRole.Admin);

        result.IsFailure.Should().BeTrue();
        org.Memberships[0].Role.Should().Be(OrganizationRole.Owner);
    }

    [Fact]
    public void Should_AllowDemotingAnOwner_When_AnotherOwnerRemains()
    {
        var org = NewOrg();
        org.AddMember(Other, OrganizationRole.Owner);

        var result = org.ChangeRole(Owner, OrganizationRole.Admin);

        result.IsSuccess.Should().BeTrue();
        org.DomainEvents.Should().Contain(e => e is MemberRoleChangedEvent);
    }

    [Fact]
    public void Should_RemoveMember_When_NotTheLastOwner()
    {
        var org = NewOrg();
        org.AddMember(Other, OrganizationRole.Member);

        var result = org.RemoveMember(Other);

        result.IsSuccess.Should().BeTrue();
        org.Memberships.Should().ContainSingle();
        org.DomainEvents.Should().Contain(e => e is MemberRemovedEvent);
    }

    [Fact]
    public void Should_Fail_When_ChangingRoleOfNonMember()
    {
        var org = NewOrg();

        var result = org.ChangeRole(Other, OrganizationRole.Admin);

        result.IsFailure.Should().BeTrue();
    }
}

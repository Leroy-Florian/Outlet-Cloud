using Outlet.Cloud.Domain.Organizations;

namespace Outlet.Cloud.Domain.UnitTests.Organizations;

public sealed class OrganizationValueObjectTests
{
    [Fact]
    public void Should_Throw_When_OrganizationIdIsEmpty()
    {
        var act = () => OrganizationId.From(Guid.Empty);

        act.Should().Throw<ArgumentException>().WithMessage("OrganizationId cannot be empty.*");
    }

    [Fact]
    public void Should_BeEqualByValue_When_OrganizationIdsShareTheSameGuid()
    {
        var guid = Guid.NewGuid();

        OrganizationId.From(guid).Should().Be(OrganizationId.From(guid));
        OrganizationId.From(guid).ToString().Should().Be(guid.ToString());
    }

    [Fact]
    public void Should_Throw_When_MemberUserIdIsEmpty()
    {
        var act = () => MemberUserId.From(Guid.Empty);

        act.Should().Throw<ArgumentException>().WithMessage("MemberUserId cannot be empty.*");
    }

    [Fact]
    public void Should_BeEqualByValue_When_MemberUserIdsShareTheSameGuid()
    {
        var guid = Guid.NewGuid();

        MemberUserId.From(guid).Should().Be(MemberUserId.From(guid));
        MemberUserId.From(guid).ToString().Should().Be(guid.ToString());
    }

    [Fact]
    public void Should_TrimName_When_OrganizationNameIsCreated()
    {
        var name = OrganizationName.From("  Acme Corp ");

        name.Value.Should().Be("Acme Corp");
        name.ToString().Should().Be("Acme Corp");
        name.Should().Be(OrganizationName.From("Acme Corp"));
    }

    [Fact]
    public void Should_Throw_When_OrganizationNameIsBlank()
    {
        var act = () => OrganizationName.From("   ");

        act.Should().Throw<ArgumentException>().WithMessage("OrganizationName cannot be empty.*");
    }

    [Fact]
    public void Should_Throw_When_OrganizationNameExceedsMaxLength()
    {
        var act = () => OrganizationName.From(new string('a', OrganizationName.MaxLength + 1));

        act.Should().Throw<ArgumentException>().WithMessage($"OrganizationName must be at most {OrganizationName.MaxLength} characters.*");
    }

    [Fact]
    public void Should_AcceptName_When_ExactlyAtMaxLength()
    {
        var name = OrganizationName.From(new string('a', OrganizationName.MaxLength));

        name.Value.Should().HaveLength(OrganizationName.MaxLength);
    }
}

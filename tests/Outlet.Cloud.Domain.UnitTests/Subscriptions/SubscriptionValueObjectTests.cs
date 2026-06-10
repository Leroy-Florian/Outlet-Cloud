using Outlet.Cloud.Domain.Subscriptions;

namespace Outlet.Cloud.Domain.UnitTests.Subscriptions;

public sealed class SubscriptionValueObjectTests
{
    [Fact]
    public void Should_Throw_When_AccountIdIsEmpty()
    {
        var act = () => AccountId.From(Guid.Empty);

        act.Should().Throw<ArgumentException>().WithMessage("AccountId cannot be empty.*");
    }

    [Fact]
    public void Should_BeEqualByValue_When_AccountIdsShareTheSameGuid()
    {
        var guid = Guid.NewGuid();

        AccountId.From(guid).Should().Be(AccountId.From(guid));
        AccountId.From(guid).GetHashCode().Should().Be(AccountId.From(guid).GetHashCode());
        AccountId.From(guid).ToString().Should().Be(guid.ToString());
    }

    [Fact]
    public void Should_Throw_When_SubscriptionIdIsEmpty()
    {
        var act = () => SubscriptionId.From(Guid.Empty);

        act.Should().Throw<ArgumentException>().WithMessage("SubscriptionId cannot be empty.*");
    }

    [Fact]
    public void Should_BeEqualByValue_When_SubscriptionIdsShareTheSameGuid()
    {
        var guid = Guid.NewGuid();

        SubscriptionId.From(guid).Should().Be(SubscriptionId.From(guid));
        SubscriptionId.From(guid).GetHashCode().Should().Be(SubscriptionId.From(guid).GetHashCode());
        SubscriptionId.From(guid).ToString().Should().Be(guid.ToString());
    }

    [Fact]
    public void Should_GrantNothing_When_PlanIsUnknown()
    {
        var entitlements = Entitlements.For((PlanTier)999);

        entitlements.Should().Be(Entitlements.None);
        entitlements.CanPublishPrivateItems.Should().BeFalse();
        entitlements.CanReadPrivateRegistry.Should().BeFalse();
        entitlements.MaxPrivateItems.Should().Be(0);
        entitlements.Analytics.Should().BeFalse();
    }

    [Fact]
    public void Should_BeEqualByDates_When_TrialPeriodsCoverTheSameWindow()
    {
        var start = new DateOnly(2026, 6, 1);

        TrialPeriod.Of(start, 14).Should().Be(TrialPeriod.Between(start, start.AddDays(14)));
        TrialPeriod.Of(start, 14).Should().NotBe(TrialPeriod.Of(start, 7));
    }

    [Fact]
    public void Should_AllowReadingOnly_When_EntitlementsAreReadOnly()
    {
        Entitlements.ReadOnly.CanPublishPrivateItems.Should().BeFalse();
        Entitlements.ReadOnly.CanReadPrivateRegistry.Should().BeTrue();
        Entitlements.ReadOnly.MaxPrivateItems.Should().Be(0);
        Entitlements.ReadOnly.Analytics.Should().BeFalse();
    }

    [Fact]
    public void Should_NotBeEqual_When_TrialPeriodsDifferOnlyByStartDate()
    {
        var end = new DateOnly(2026, 6, 15);

        TrialPeriod.Between(new DateOnly(2026, 6, 1), end).Should()
            .NotBe(TrialPeriod.Between(new DateOnly(2026, 6, 2), end));
    }

    [Fact]
    public void Should_CompareByValues_When_EntitlementsAreResolvedTwice()
    {
        Entitlements.For(PlanTier.Pro).Should().Be(Entitlements.For(PlanTier.Pro));
        Entitlements.For(PlanTier.Pro).Should().NotBe(Entitlements.ReadOnly);
        Entitlements.ReadOnly.Should().NotBe(Entitlements.None);
    }
}

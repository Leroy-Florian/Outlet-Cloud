using Outlet.Cloud.Domain.Subscriptions;

namespace Outlet.Cloud.Domain.UnitTests.Subscriptions;

public sealed class SubscriptionTests
{
    private static readonly SubscriptionId AnyId = SubscriptionId.From(Guid.NewGuid());
    private static readonly AccountId AnyAccount = AccountId.From(Guid.NewGuid());
    private static readonly DateOnly Day0 = new(2026, 6, 1);

    private static Subscription NewTrial(int days = 14) =>
        Subscription.CreateTrial(AnyId, AnyAccount, TrialPeriod.Of(Day0, days)).Value!;

    [Fact]
    public void Should_StartInTrialing_When_TrialCreated()
    {
        var subscription = NewTrial();

        subscription.Status.Should().Be(SubscriptionStatus.Trialing);
        subscription.Plan.Should().Be(PlanTier.Pro);
        subscription.DomainEvents.Should().ContainSingle(e => e is SubscriptionTrialStartedEvent);
    }

    [Fact]
    public void Should_GrantFullEntitlements_When_Trialing()
    {
        var entitlements = NewTrial().ResolveEntitlements();

        entitlements.CanPublishPrivateItems.Should().BeTrue();
        entitlements.CanReadPrivateRegistry.Should().BeTrue();
        entitlements.MaxPrivateItems.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Should_BecomeActive_When_Converted()
    {
        var subscription = NewTrial();

        var result = subscription.Convert(PlanTier.Pro);

        result.IsSuccess.Should().BeTrue();
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.DomainEvents.Should().Contain(e => e is SubscriptionConvertedEvent);
    }

    [Fact]
    public void Should_Fail_When_ExpiringBeforeTrialElapsed()
    {
        var subscription = NewTrial(days: 14);

        var result = subscription.ExpireTrial(Day0.AddDays(5));

        result.IsFailure.Should().BeTrue();
        subscription.Status.Should().Be(SubscriptionStatus.Trialing);
    }

    [Fact]
    public void Should_Suspend_When_TrialElapsed()
    {
        var subscription = NewTrial(days: 14);

        var result = subscription.ExpireTrial(Day0.AddDays(14));

        result.IsSuccess.Should().BeTrue();
        subscription.Status.Should().Be(SubscriptionStatus.Suspended);
        subscription.DomainEvents.Should().Contain(e => e is SubscriptionSuspendedEvent);
    }

    [Fact]
    public void Should_BeReadOnly_When_Suspended()
    {
        var subscription = NewTrial(days: 1);
        subscription.ExpireTrial(Day0.AddDays(1));

        var entitlements = subscription.ResolveEntitlements();

        entitlements.CanPublishPrivateItems.Should().BeFalse();
        entitlements.CanReadPrivateRegistry.Should().BeTrue();
    }

    [Fact]
    public void Should_Suspend_When_ActivePlanCancelled()
    {
        var subscription = NewTrial();
        subscription.Convert(PlanTier.Pro);

        var result = subscription.Cancel();

        result.IsSuccess.Should().BeTrue();
        subscription.Status.Should().Be(SubscriptionStatus.Suspended);
        subscription.ResolveEntitlements().CanPublishPrivateItems.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_CancellingATrial()
    {
        var subscription = NewTrial();

        var result = subscription.Cancel();

        result.IsFailure.Should().BeTrue();
        subscription.Status.Should().Be(SubscriptionStatus.Trialing);
    }

    [Fact]
    public void Should_Reactivate_When_SuspendedAccountPays()
    {
        var subscription = NewTrial(days: 1);
        subscription.ExpireTrial(Day0.AddDays(1));

        var result = subscription.Reactivate(PlanTier.Pro);

        result.IsSuccess.Should().BeTrue();
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.ResolveEntitlements().CanPublishPrivateItems.Should().BeTrue();
    }

    [Fact]
    public void Should_Expire_When_SuspendedAccountPurged()
    {
        var subscription = NewTrial(days: 1);
        subscription.ExpireTrial(Day0.AddDays(1));

        var result = subscription.Purge();

        result.IsSuccess.Should().BeTrue();
        subscription.Status.Should().Be(SubscriptionStatus.Expired);
        subscription.ResolveEntitlements().CanReadPrivateRegistry.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_When_ReactivatingANonSuspendedSubscription()
    {
        var subscription = NewTrial();

        var result = subscription.Reactivate(PlanTier.Pro);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Should_ReportTrialDaysRemaining_When_Trialing()
    {
        var subscription = NewTrial(days: 14);

        subscription.TrialDaysRemainingAsOf(Day0.AddDays(5)).Should().Be(9);
    }
}

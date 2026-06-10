using Outlet.Cloud.Application.Subscriptions;
using Outlet.Cloud.Application.UnitTests.Fakes;
using Outlet.Cloud.Domain.Subscriptions;

namespace Outlet.Cloud.Application.UnitTests.Subscriptions;

public sealed class EntitlementFlowTests
{
    private static readonly DateOnly Day0 = new(2026, 6, 1);
    private static readonly Guid Account = Guid.NewGuid();
    private static readonly AccountId AccountIdValue = AccountId.From(Account);

    private static Subscription Trial(int days = 14) =>
        Subscription.CreateTrial(SubscriptionId.From(Guid.NewGuid()), AccountIdValue, TrialPeriod.Of(Day0, days)).Value!;

    [Fact]
    public async Task Should_ReturnNone_When_NoSubscription()
    {
        var subscriptions = new FakeSubscriptionRepository();
        var resolver = new SubscriptionEntitlementResolver(subscriptions, new FixedClock(Day0));

        var entitlements = await resolver.ResolveAsync(AccountIdValue);

        entitlements.CanReadPrivateRegistry.Should().BeFalse();
    }

    [Fact]
    public async Task Should_LazilySuspend_When_TrialElapsed()
    {
        var subscriptions = new FakeSubscriptionRepository();
        subscriptions.Seed(Trial(days: 14));
        var resolver = new SubscriptionEntitlementResolver(subscriptions, new FixedClock(Day0.AddDays(20)));

        var entitlements = await resolver.ResolveAsync(AccountIdValue);

        entitlements.CanPublishPrivateItems.Should().BeFalse();
        entitlements.CanReadPrivateRegistry.Should().BeTrue();
        subscriptions.UpdateCount.Should().Be(1, "the elapsed trial is persisted as Suspended on read");
        (await subscriptions.GetByAccountAsync(AccountIdValue))!.Status.Should().Be(SubscriptionStatus.Suspended);
    }

    [Fact]
    public async Task Should_NotSuspend_When_StillWithinTrial()
    {
        var subscriptions = new FakeSubscriptionRepository();
        subscriptions.Seed(Trial(days: 14));
        var resolver = new SubscriptionEntitlementResolver(subscriptions, new FixedClock(Day0.AddDays(5)));

        var entitlements = await resolver.ResolveAsync(AccountIdValue);

        entitlements.CanPublishPrivateItems.Should().BeTrue();
        subscriptions.UpdateCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_ReportTrialDaysRemaining_When_GettingEntitlements()
    {
        var subscriptions = new FakeSubscriptionRepository();
        subscriptions.Seed(Trial(days: 14));
        var clock = new FixedClock(Day0.AddDays(5));
        var useCase = new GetEntitlementsUseCase(subscriptions, new SubscriptionEntitlementResolver(subscriptions, clock), clock);

        var result = await useCase.HandleAsync(new GetEntitlementsQuery(Account));

        result.IsSuccess.Should().BeTrue();
        result.Value!.HasSubscription.Should().BeTrue();
        result.Value.Status.Should().Be(nameof(SubscriptionStatus.Trialing));
        result.Value.TrialDaysRemaining.Should().Be(9);
        result.Value.CanPublishPrivateItems.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Convert_When_Trialing()
    {
        var subscriptions = new FakeSubscriptionRepository();
        subscriptions.Seed(Trial());
        var useCase = new ConvertSubscriptionUseCase(subscriptions);

        var result = await useCase.HandleAsync(new ConvertSubscriptionCommand(Account));

        result.IsSuccess.Should().BeTrue();
        (await subscriptions.GetByAccountAsync(AccountIdValue))!.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public async Task Should_Cancel_When_Active()
    {
        var subscriptions = new FakeSubscriptionRepository();
        var sub = Trial();
        sub.Convert(PlanTier.Pro);
        subscriptions.Seed(sub);
        var useCase = new CancelSubscriptionUseCase(subscriptions);

        var result = await useCase.HandleAsync(new CancelSubscriptionCommand(Account));

        result.IsSuccess.Should().BeTrue();
        (await subscriptions.GetByAccountAsync(AccountIdValue))!.Status.Should().Be(SubscriptionStatus.Suspended);
    }

    [Fact]
    public async Task Should_Fail_When_ConvertingWithoutSubscription()
    {
        var useCase = new ConvertSubscriptionUseCase(new FakeSubscriptionRepository());

        var result = await useCase.HandleAsync(new ConvertSubscriptionCommand(Account));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_ConvertingWithAnEmptyAccountId()
    {
        var useCase = new ConvertSubscriptionUseCase(new FakeSubscriptionRepository());

        var result = await useCase.HandleAsync(new ConvertSubscriptionCommand(Guid.Empty));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Account id is invalid.");
    }

    [Fact]
    public async Task Should_Fail_When_ConvertingASuspendedSubscription()
    {
        var subscriptions = new FakeSubscriptionRepository();
        var sub = Trial(days: 1);
        sub.ExpireTrial(Day0.AddDays(1));
        subscriptions.Seed(sub);
        var useCase = new ConvertSubscriptionUseCase(subscriptions);

        var result = await useCase.HandleAsync(new ConvertSubscriptionCommand(Account));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Only a trialing subscription can be converted.");
        subscriptions.UpdateCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Fail_When_CancellingWithAnEmptyAccountId()
    {
        var useCase = new CancelSubscriptionUseCase(new FakeSubscriptionRepository());

        var result = await useCase.HandleAsync(new CancelSubscriptionCommand(Guid.Empty));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Account id is invalid.");
    }

    [Fact]
    public async Task Should_Fail_When_CancellingWithoutSubscription()
    {
        var useCase = new CancelSubscriptionUseCase(new FakeSubscriptionRepository());

        var result = await useCase.HandleAsync(new CancelSubscriptionCommand(Account));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("This account has no subscription to cancel.");
    }

    [Fact]
    public async Task Should_Fail_When_CancellingATrialingSubscription()
    {
        var subscriptions = new FakeSubscriptionRepository();
        subscriptions.Seed(Trial());
        var useCase = new CancelSubscriptionUseCase(subscriptions);

        var result = await useCase.HandleAsync(new CancelSubscriptionCommand(Account));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Only an active subscription can be cancelled.");
        subscriptions.UpdateCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Fail_When_GettingEntitlementsWithAnEmptyAccountId()
    {
        var subscriptions = new FakeSubscriptionRepository();
        var clock = new FixedClock(Day0);
        var useCase = new GetEntitlementsUseCase(subscriptions, new SubscriptionEntitlementResolver(subscriptions, clock), clock);

        var result = await useCase.HandleAsync(new GetEntitlementsQuery(Guid.Empty));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Account id is invalid.");
    }

    [Fact]
    public async Task Should_ReportNoSubscriptionWithNoEntitlements_When_AccountIsUnknown()
    {
        var subscriptions = new FakeSubscriptionRepository();
        var clock = new FixedClock(Day0);
        var useCase = new GetEntitlementsUseCase(subscriptions, new SubscriptionEntitlementResolver(subscriptions, clock), clock);

        var result = await useCase.HandleAsync(new GetEntitlementsQuery(Account));

        result.IsSuccess.Should().BeTrue();
        result.Value!.HasSubscription.Should().BeFalse();
        result.Value.Status.Should().Be(nameof(SubscriptionStatus.Expired));
        result.Value.TrialDaysRemaining.Should().Be(0);
        result.Value.CanPublishPrivateItems.Should().BeFalse();
        result.Value.CanReadPrivateRegistry.Should().BeFalse();
        result.Value.MaxPrivateItems.Should().Be(0);
        result.Value.Analytics.Should().BeFalse();
    }
}

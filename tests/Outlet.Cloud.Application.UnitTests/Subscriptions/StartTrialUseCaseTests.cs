using Outlet.Cloud.Application.Subscriptions;
using Outlet.Cloud.Application.UnitTests.Fakes;
using Outlet.Cloud.Domain.Organizations;
using Outlet.Cloud.Domain.Subscriptions;

namespace Outlet.Cloud.Application.UnitTests.Subscriptions;

public sealed class StartTrialUseCaseTests
{
    private static readonly DateOnly Today = new(2026, 6, 1);
    private readonly FakeSubscriptionRepository _subscriptions = new();

    private StartTrialUseCase NewUseCase(FakeTrialEligibilityPolicy? policy = null) =>
        new(_subscriptions, policy ?? FakeTrialEligibilityPolicy.Eligible(), new FixedClock(Today));

    [Fact]
    public async Task Should_StartTrial_When_EligibleAndNoExistingSubscription()
    {
        var orgId = Guid.NewGuid();

        var result = await NewUseCase().HandleAsync(new StartTrialCommand(orgId, "dev@acme.test", 14));

        result.IsSuccess.Should().BeTrue();
        var stored = await _subscriptions.GetByOrganizationAsync(OrganizationId.From(orgId));
        stored!.Status.Should().Be(SubscriptionStatus.Trialing);
        stored.Trial!.EndsOn.Should().Be(Today.AddDays(14));
    }

    [Fact]
    public async Task Should_Fail_When_NotEligible()
    {
        var policy = FakeTrialEligibilityPolicy.Rejecting("Disposable e-mail addresses are not eligible for a trial.");

        var result = await NewUseCase(policy).HandleAsync(new StartTrialCommand(Guid.NewGuid(), "x@mailinator.com", 14));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Disposable");
    }

    [Fact]
    public async Task Should_Fail_When_OrganizationAlreadyHasASubscription()
    {
        var orgId = Guid.NewGuid();
        _subscriptions.Seed(Subscription.CreateTrial(
            SubscriptionId.From(Guid.NewGuid()), OrganizationId.From(orgId), TrialPeriod.Of(Today, 14)).Value!);

        var result = await NewUseCase().HandleAsync(new StartTrialCommand(orgId, "dev@acme.test", 14));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_TrialDaysNotPositive()
    {
        var result = await NewUseCase().HandleAsync(new StartTrialCommand(Guid.NewGuid(), "dev@acme.test", 0));

        result.IsFailure.Should().BeTrue();
    }
}

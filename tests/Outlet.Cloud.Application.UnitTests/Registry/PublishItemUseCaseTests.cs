using Outlet.Cloud.Application.Registry;
using Outlet.Cloud.Application.Subscriptions;
using Outlet.Cloud.Application.UnitTests.Fakes;
using Outlet.Cloud.Domain.Organizations;
using Outlet.Cloud.Domain.Registry;
using Outlet.Cloud.Domain.Subscriptions;

namespace Outlet.Cloud.Application.UnitTests.Registry;

public sealed class PublishItemUseCaseTests
{
    private static readonly DateOnly Today = new(2026, 6, 1);

    private readonly FakePublishedItemRepository _items = new();
    private readonly FakeSubscriptionRepository _subscriptions = new();
    private readonly Guid _orgId = Guid.NewGuid();

    public PublishItemUseCaseTests()
    {
        // Every org under test is on an active Pro trial unless a test overrides it.
        _subscriptions.Seed(Subscription.CreateTrial(
            SubscriptionId.From(Guid.NewGuid()), OrganizationId.From(_orgId), TrialPeriod.Of(Today, 14)).Value!);
    }

    private PublishItemUseCase NewUseCase() =>
        new(_items, new SubscriptionEntitlementResolver(_subscriptions, new FixedClock(Today)));

    [Fact]
    public async Task Should_Publish_When_Valid()
    {
        var result = await NewUseCase().HandleAsync(new PublishItemCommand(
            _orgId, "email-smtp", "{\"name\":\"email-smtp\"}", [new PublishedFileInput("SmtpEmailSender.cs", "// code")]));

        result.IsSuccess.Should().BeTrue();
        var stored = await _items.GetAsync(OrganizationId.From(_orgId), RegistryItemName.From("email-smtp"));
        stored.Should().NotBeNull();
        stored!.Files.Should().ContainSingle();
    }

    [Fact]
    public async Task Should_UpsertKeepingId_When_RepublishedBySameName()
    {
        var useCase = NewUseCase();
        var first = await useCase.HandleAsync(new PublishItemCommand(
            _orgId, "email-smtp", "{\"v\":1}", [new PublishedFileInput("a.cs", "v1")]));

        var second = await useCase.HandleAsync(new PublishItemCommand(
            _orgId, "email-smtp", "{\"v\":2}", [new PublishedFileInput("a.cs", "v2")]));

        second.Value.Should().Be(first.Value);
        var stored = await _items.GetAsync(OrganizationId.From(_orgId), RegistryItemName.From("email-smtp"));
        stored!.ManifestJson.Should().Contain("2");
    }

    [Fact]
    public async Task Should_Fail_When_NoFiles()
    {
        var result = await NewUseCase().HandleAsync(new PublishItemCommand(_orgId, "email-smtp", "{}", []));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_NameIsInvalid()
    {
        var result = await NewUseCase().HandleAsync(new PublishItemCommand(
            _orgId, "Bad Name", "{}", [new PublishedFileInput("a.cs", "x")]));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_TrialHasExpired()
    {
        var expiredOrg = Guid.NewGuid();
        _subscriptions.Seed(Subscription.CreateTrial(
            SubscriptionId.From(Guid.NewGuid()), OrganizationId.From(expiredOrg), TrialPeriod.Of(Today, 14)).Value!);

        // A resolver whose clock is past the trial window lazily suspends the subscription.
        var useCase = new PublishItemUseCase(_items, new SubscriptionEntitlementResolver(_subscriptions, new FixedClock(Today.AddDays(30))));

        var result = await useCase.HandleAsync(new PublishItemCommand(
            expiredOrg, "email-smtp", "{}", [new PublishedFileInput("a.cs", "x")]));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("trial has ended");
    }

    [Fact]
    public async Task Should_Fail_When_OrganizationHasNoSubscription()
    {
        var orphanOrg = Guid.NewGuid();
        var useCase = NewUseCase();

        var result = await useCase.HandleAsync(new PublishItemCommand(
            orphanOrg, "email-smtp", "{}", [new PublishedFileInput("a.cs", "x")]));

        result.IsFailure.Should().BeTrue();
    }
}

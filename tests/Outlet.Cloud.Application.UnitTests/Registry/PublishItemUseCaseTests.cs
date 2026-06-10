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
    private readonly FakeOrganizationRepository _orgs = new();
    private readonly FakeSubscriptionRepository _subscriptions = new();

    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _ownerId = Guid.NewGuid();

    public PublishItemUseCaseTests()
    {
        _orgs.Seed(Organization.Create(
            OrganizationId.From(_orgId), OrganizationSlug.From("acme"), OrganizationName.From("Acme"),
            MemberUserId.From(_ownerId)).Value!);

        // The owner account is on an active Pro trial unless a test overrides it.
        _subscriptions.Seed(Subscription.CreateTrial(
            SubscriptionId.From(Guid.NewGuid()), AccountId.From(_ownerId), TrialPeriod.Of(Today, 14)).Value!);
    }

    private PublishItemUseCase NewUseCase(DateOnly? asOf = null) =>
        new(_items, _orgs, new SubscriptionEntitlementResolver(_subscriptions, new FixedClock(asOf ?? Today)));

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
    public async Task Should_Fail_When_OrganizationNotFound()
    {
        var result = await NewUseCase().HandleAsync(new PublishItemCommand(
            Guid.NewGuid(), "email-smtp", "{}", [new PublishedFileInput("a.cs", "x")]));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Should_Fail_When_OwnerTrialHasExpired()
    {
        // A resolver whose clock is past the owner's trial window lazily suspends the account.
        var result = await NewUseCase(asOf: Today.AddDays(30)).HandleAsync(new PublishItemCommand(
            _orgId, "email-smtp", "{}", [new PublishedFileInput("a.cs", "x")]));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("trial has ended");
    }

    [Fact]
    public async Task Should_Fail_When_OwnerHasNoSubscription()
    {
        var orphanOwner = Guid.NewGuid();
        _orgs.Seed(Organization.Create(
            OrganizationId.From(Guid.NewGuid()), OrganizationSlug.From("orphan"), OrganizationName.From("Orphan"),
            MemberUserId.From(orphanOwner)).Value!);
        var orphanOrgId = (await _orgs.GetBySlugAsync(OrganizationSlug.From("orphan")))!.Id.Value;

        var result = await NewUseCase().HandleAsync(new PublishItemCommand(
            orphanOrgId, "email-smtp", "{}", [new PublishedFileInput("a.cs", "x")]));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Fail_When_OrganizationIdIsEmpty()
    {
        var result = await NewUseCase().HandleAsync(new PublishItemCommand(
            Guid.Empty, "email-smtp", "{}", [new PublishedFileInput("a.cs", "x")]));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Organization id is invalid.");
    }

    [Fact]
    public async Task Should_Fail_When_AFilePathIsBlank()
    {
        var result = await NewUseCase().HandleAsync(new PublishItemCommand(
            _orgId, "email-smtp", "{}", [new PublishedFileInput("  ", "content")]));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("invalid");
    }

    [Fact]
    public async Task Should_Fail_When_PrivateItemQuotaIsReached()
    {
        var quota = Entitlements.For(PlanTier.Pro).MaxPrivateItems;
        for (var i = 0; i < quota; i++)
            await _items.UpsertAsync(PublishedItem.Restore(
                PublishedItemId.From(Guid.NewGuid()), OrganizationId.From(_orgId),
                RegistryItemName.From($"item-{i}"), "{}", [PublishedFile.From("a.cs", "x")]));

        var result = await NewUseCase().HandleAsync(new PublishItemCommand(
            _orgId, "one-too-many", "{}", [new PublishedFileInput("a.cs", "x")]));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain($"limit reached ({quota})");
    }

    [Fact]
    public async Task Should_AllowRepublish_When_QuotaIsAlreadyFull()
    {
        var quota = Entitlements.For(PlanTier.Pro).MaxPrivateItems;
        for (var i = 0; i < quota; i++)
            await _items.UpsertAsync(PublishedItem.Restore(
                PublishedItemId.From(Guid.NewGuid()), OrganizationId.From(_orgId),
                RegistryItemName.From($"item-{i}"), "{}", [PublishedFile.From("a.cs", "x")]));

        var result = await NewUseCase().HandleAsync(new PublishItemCommand(
            _orgId, "item-0", "{\"v\":2}", [new PublishedFileInput("a.cs", "v2")]));

        result.IsSuccess.Should().BeTrue();
    }
}

using Outlet.Cloud.Application.Registry;
using Outlet.Cloud.Application.UnitTests.Fakes;
using Outlet.Cloud.Domain.Organizations;
using Outlet.Cloud.Domain.Registry;

namespace Outlet.Cloud.Application.UnitTests.Registry;

public sealed class PublishItemUseCaseTests
{
    private readonly FakePublishedItemRepository _items = new();
    private readonly Guid _orgId = Guid.NewGuid();

    private PublishItemUseCase NewUseCase() => new(_items);

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
}

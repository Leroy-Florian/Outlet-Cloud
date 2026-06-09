using Outlet.Cloud.Application.Ports;
using Outlet.Cloud.Application.Subscriptions;
using Outlet.Cloud.Domain.Organizations;
using Outlet.Cloud.Domain.Registry;
using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Application.Registry;

/// <summary>A file to publish: its registry-relative path and raw text content.</summary>
public sealed record PublishedFileInput(string Path, string Content);

/// <summary>Command: publish (or replace) an item in an organization's private registry.</summary>
public sealed record PublishItemCommand(
    Guid OrganizationId,
    string Name,
    string ManifestJson,
    IReadOnlyList<PublishedFileInput> Files);

/// <summary>
/// Publishes an item into an org's registry. Upserts by (organization, name): a new
/// publish of the same name replaces the previous content while keeping its id.
/// </summary>
public sealed class PublishItemUseCase(IPublishedItemRepository items, SubscriptionEntitlementResolver entitlements)
    : IUseCase<PublishItemCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(PublishItemCommand command, CancellationToken cancellationToken = default)
    {
        var orgResult = Guard.TryBuild(() => OrganizationId.From(command.OrganizationId), "Organization id is invalid.");
        if (orgResult.IsFailure)
            return Result<Guid>.Failure(orgResult.Error!);

        var nameResult = Guard.TryBuild(() => RegistryItemName.From(command.Name), $"Item name '{command.Name}' is invalid.");
        if (nameResult.IsFailure)
            return Result<Guid>.Failure(nameResult.Error!);

        // Server-side authorization: publishing is a Pro feature. An expired trial or a
        // suspended subscription is read-only — the CLI surfaces this with a clear message.
        var allowed = await entitlements.ResolveAsync(orgResult.Value!, cancellationToken);
        if (!allowed.CanPublishPrivateItems)
            return Result<Guid>.Failure(
                "Your Outlet Cloud trial has ended or your subscription is suspended — reactivate to publish private items.");

        var files = new List<PublishedFile>();
        foreach (var file in command.Files)
        {
            var fileResult = Guard.TryBuild(() => PublishedFile.From(file.Path, file.Content), $"File path '{file.Path}' is invalid.");
            if (fileResult.IsFailure)
                return Result<Guid>.Failure(fileResult.Error!);

            files.Add(fileResult.Value!);
        }

        var existing = await items.GetAsync(orgResult.Value!, nameResult.Value!, cancellationToken);

        // A re-publish of an existing item is always allowed; only NEW items count against the quota.
        if (existing is null)
        {
            var current = await items.ListForOrganizationAsync(orgResult.Value!, cancellationToken);
            if (current.Count >= allowed.MaxPrivateItems)
                return Result<Guid>.Failure(
                    $"Private registry item limit reached ({allowed.MaxPrivateItems}). Upgrade your plan to publish more.");
        }

        var id = existing?.Id ?? PublishedItemId.From(Guid.NewGuid());

        var itemResult = PublishedItem.Create(id, orgResult.Value!, nameResult.Value!, command.ManifestJson, files);
        if (itemResult.IsFailure)
            return Result<Guid>.Failure(itemResult.Error!);

        await items.UpsertAsync(itemResult.Value!, cancellationToken);

        return Result<Guid>.Success(id.Value);
    }
}

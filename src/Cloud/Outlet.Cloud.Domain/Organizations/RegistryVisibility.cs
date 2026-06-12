namespace Outlet.Cloud.Domain.Organizations;

/// <summary>
/// Whether an organization's hosted registry may be pulled anonymously.
/// <see cref="Private"/> (the default) requires a personal access token with the
/// read scope; <see cref="Public"/> lets the CLI pull without any credential.
/// Publishing is always authenticated regardless of visibility.
/// </summary>
public enum RegistryVisibility
{
    Private = 0,
    Public = 1,
}

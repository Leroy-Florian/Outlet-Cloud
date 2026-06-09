namespace Outlet.Registry.Auth;

/// <summary>The principal behind a valid bearer token: who owns it and what it may do.</summary>
public sealed record AuthenticatedToken(Guid OwnerId, IReadOnlyList<string> Scopes);

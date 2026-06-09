namespace Outlet.Registry.Auth;

/// <summary>Options for the SHA-256 bearer authenticator, bound via <c>IOptions&lt;PersonalAccessTokenAuthenticationOptions&gt;</c>.</summary>
public sealed class PersonalAccessTokenAuthenticationOptions
{
    /// <summary>The Authorization scheme prefix stripped before hashing the secret. Defaults to <c>"Bearer "</c>.</summary>
    public string BearerPrefix { get; set; } = "Bearer ";
}

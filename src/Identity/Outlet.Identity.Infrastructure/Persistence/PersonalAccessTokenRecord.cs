namespace Outlet.Identity.Infrastructure.Persistence;

/// <summary>
/// EF persistence model for a <c>PersonalAccessToken</c>. Scopes are stored as a
/// single space-delimited string (scope values are lowercase and whitespace-free,
/// so a space is an unambiguous separator).
/// </summary>
public sealed class PersonalAccessTokenRecord
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public string Scopes { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
}

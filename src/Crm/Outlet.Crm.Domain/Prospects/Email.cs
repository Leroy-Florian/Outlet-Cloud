using Outlet.Kernel.Shared;

namespace Outlet.Crm.Domain.Prospects;

public sealed record Email
{
    private Email(string value) => Value = value;

    public string Value { get; }

    public static Result<Email> Create(string value)
    {
        var trimmed = value.Trim();
        var atIndex = trimmed.IndexOf('@', StringComparison.Ordinal);

        if (trimmed.Length is 0 || atIndex <= 0 || atIndex == trimmed.Length - 1 || trimmed.Contains(' ', StringComparison.Ordinal))
        {
            return Result.Failure<Email>($"Email.Invalid: '{value}' is not a valid email address.");
        }

        return Result.Success(new Email(trimmed.ToLowerInvariant()));
    }
}

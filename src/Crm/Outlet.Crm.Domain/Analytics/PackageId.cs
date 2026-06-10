using Outlet.Kernel.Shared;

namespace Outlet.Crm.Domain.Analytics;

public sealed record PackageId
{
    private PackageId(string value) => Value = value;

    public string Value { get; }

    public static Result<PackageId> Create(string value)
    {
        var trimmed = value.Trim();

        if (trimmed.Length is 0)
        {
            return Result.Failure<PackageId>("PackageId.Empty: A package id cannot be empty.");
        }

        return Result.Success(new PackageId(trimmed.ToLowerInvariant()));
    }
}

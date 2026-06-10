using Outlet.Kernel.Shared;

namespace Outlet.Crm.Domain.Products;

/// <summary>Nom complet d'un repository GitHub au format owner/name.</summary>
public sealed record RepositoryName
{
    private RepositoryName(string owner, string name)
    {
        Owner = owner;
        Name = name;
    }

    public string Owner { get; }

    public string Name { get; }

    public string FullName => $"{Owner}/{Name}";

    public static Result<RepositoryName> Create(string fullName)
    {
        var parts = fullName.Trim().Split('/');

        if (parts.Length != 2 || parts[0].Length is 0 || parts[1].Length is 0)
        {
            return Result.Failure<RepositoryName>(
                $"RepositoryName.Invalid: '{fullName}' is not a valid owner/name repository.");
        }

        return Result.Success(new RepositoryName(parts[0], parts[1]));
    }
}

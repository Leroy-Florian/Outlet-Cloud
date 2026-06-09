using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Domain.Registry;

/// <summary>A file shipped by a published item: its registry-relative path and raw text content.</summary>
public sealed class PublishedFile : ValueObject
{
    public string Path { get; }
    public string Content { get; }

    private PublishedFile(string path, string content)
    {
        Path = path;
        Content = content;
    }

    public static PublishedFile From(string path, string content)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("PublishedFile path cannot be empty.", nameof(path));

        return new PublishedFile(path.Trim(), content ?? string.Empty);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Path;
        yield return Content;
    }

    public override string ToString() => Path;
}

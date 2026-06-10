using Outlet.Crm.Domain.Products;
using Outlet.Crm.Domain.Traffic;

namespace Outlet.Crm.Domain.UnitTests.Traffic;

public sealed class TrafficSampleTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    private static readonly ProductId Product = ProductId.New();

    [Fact]
    public void Should_Fail_When_PathIsBlank()
    {
        var result = TrafficSample.Create(Product, "  ", null, null, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("TrafficSample.PathRequired: A path is required.");
    }

    [Fact]
    public void Should_TrimAndKeepLeadingSlash_When_PathIsValid()
    {
        var result = TrafficSample.Create(Product, " /docs/getting-started ", null, null, Now);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Path.Should().Be("/docs/getting-started");
        result.Value.ProductId.Should().Be(Product);
        result.Value.OccurredAt.Should().Be(Now);
    }

    [Fact]
    public void Should_PrependSlash_When_PathHasNone()
    {
        var result = TrafficSample.Create(Product, "docs", null, null, Now);

        result.Value!.Path.Should().Be("/docs");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_NormalizeReferrerToDirect_When_ReferrerIsBlank(string? referrer)
    {
        var result = TrafficSample.Create(Product, "/", referrer, null, Now);

        result.Value!.ReferrerSource.Should().Be("direct");
    }

    [Theory]
    [InlineData("https://www.google.com/search?q=outlet", "google")]
    [InlineData("https://google.fr/", "google")]
    [InlineData("https://github.com/Leroy-Florian/Outlet-CLI", "github")]
    [InlineData("https://news.ycombinator.com/item?id=1", "hackernews")]
    [InlineData("https://old.reddit.com/r/dotnet", "reddit")]
    [InlineData("https://t.co/abc", "twitter")]
    [InlineData("https://x.com/abc", "twitter")]
    [InlineData("https://www.linkedin.com/feed", "linkedin")]
    [InlineData("https://duckduckgo.com/?q=outlet", "duckduckgo")]
    [InlineData("https://www.bing.com/search", "bing")]
    [InlineData("https://Blog.Example.IO/post", "blog.example.io")]
    [InlineData("blog.example.io", "blog.example.io")]
    [InlineData("www.example.com", "example.com")]
    public void Should_NormalizeReferrer_When_HostIsKnownOrUnknown(string referrer, string expected)
    {
        ReferrerSource.Normalize(referrer).Should().Be(expected);
    }

    [Fact]
    public void Should_CategorizeUserAgentAsNull_When_NotSupplied()
    {
        var result = TrafficSample.Create(Product, "/", null, " ", Now);

        result.Value!.UserAgentCategory.Should().BeNull();
    }

    [Theory]
    [InlineData("Mozilla/5.0 (compatible; Googlebot/2.1)")]
    [InlineData("my-crawler/1.0")]
    [InlineData("SpiderThing")]
    [InlineData("curl/8.5.0")]
    [InlineData("Wget/1.21")]
    [InlineData("python-requests/2.32")]
    public void Should_CategorizeUserAgentAsBot_When_KnownBotMarkerPresent(string userAgent)
    {
        UserAgentCategory.Categorize(userAgent).Should().Be("bot");
    }

    [Fact]
    public void Should_CategorizeUserAgentAsBrowser_When_NoBotMarker()
    {
        var result = TrafficSample.Create(Product, "/", null, "Mozilla/5.0 (Macintosh) Safari/605.1.15", Now);

        result.Value!.UserAgentCategory.Should().Be("browser");
    }
}

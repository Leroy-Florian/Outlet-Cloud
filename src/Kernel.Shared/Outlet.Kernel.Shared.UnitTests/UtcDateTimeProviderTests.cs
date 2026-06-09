namespace Outlet.Kernel.Shared.UnitTests;

public sealed class UtcDateTimeProviderTests
{
    [Fact]
    public void UtcNow_Should_ReturnUtcKindTime()
    {
        var provider = new UtcDateTimeProvider();

        var now = provider.UtcNow;

        now.Kind.Should().Be(DateTimeKind.Utc);
        now.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Today_Should_MatchUtcNowDate()
    {
        var provider = new UtcDateTimeProvider();

        var today = provider.Today;

        today.Should().Be(DateOnly.FromDateTime(provider.UtcNow));
    }
}

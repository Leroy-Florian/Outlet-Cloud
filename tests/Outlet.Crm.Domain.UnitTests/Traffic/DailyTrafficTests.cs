using Outlet.Crm.Domain.Products;
using Outlet.Crm.Domain.Traffic;

namespace Outlet.Crm.Domain.UnitTests.Traffic;

public sealed class DailyTrafficTests
{
    private static readonly DateTime Day1Noon = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    private static readonly DateOnly Day1 = new(2026, 6, 1);

    private static readonly ProductId Product = ProductId.New();

    private static TrafficSample Sample(string path, DateTime at, string? referrer = null) =>
        TrafficSample.Create(Product, path, referrer, null, at).Value!;

    [Fact]
    public void Should_ReturnZeroFilledDays_When_NoSamples()
    {
        var report = DailyTraffic.Compute([], Day1, Day1.AddDays(1));

        report.From.Should().Be(Day1);
        report.To.Should().Be(Day1.AddDays(1));
        report.TotalPageViews.Should().Be(0);
        report.Days.Select(d => d.Date).Should().Equal(Day1, Day1.AddDays(1));
        report.Days.Select(d => d.PageViews).Should().Equal(0, 0);
        report.TopPaths.Should().BeEmpty();
        report.TopReferrers.Should().BeEmpty();
    }

    [Fact]
    public void Should_CountPerDay_When_SamplesSpanSeveralDays()
    {
        var report = DailyTraffic.Compute(
        [
            Sample("/", Day1Noon),
            Sample("/docs", Day1Noon.AddHours(1)),
            Sample("/", Day1Noon.AddDays(1)),
        ], Day1, Day1.AddDays(1));

        report.TotalPageViews.Should().Be(3);
        report.Days.Select(d => d.PageViews).Should().Equal(2, 1);
    }

    [Fact]
    public void Should_IncludeRangeBoundaries_When_SamplesLandExactlyOnFromAndTo()
    {
        var report = DailyTraffic.Compute(
        [
            Sample("/before", Day1Noon.AddDays(-1)),
            Sample("/from", Day1.ToDateTime(TimeOnly.MinValue)),
            Sample("/to", Day1.AddDays(1).ToDateTime(new TimeOnly(23, 59, 59))),
            Sample("/after", Day1Noon.AddDays(2)),
        ], Day1, Day1.AddDays(1));

        report.TotalPageViews.Should().Be(2);
        report.TopPaths.Select(p => p.Key).Should().Equal("/from", "/to");
    }

    [Fact]
    public void Should_OrderTopPathsByCountThenKey_When_CountsTie()
    {
        var report = DailyTraffic.Compute(
        [
            Sample("/b", Day1Noon),
            Sample("/b", Day1Noon),
            Sample("/c", Day1Noon),
            Sample("/a", Day1Noon),
        ], Day1, Day1);

        report.TopPaths.Should().Equal(
            new TrafficCount("/b", 2),
            new TrafficCount("/a", 1),
            new TrafficCount("/c", 1));
    }

    [Fact]
    public void Should_CapTopListsAtTen_When_MoreThanTenKeysExist()
    {
        List<TrafficSample> samples = [.. Enumerable.Range(0, 11).Select(i => Sample($"/p{i:D2}", Day1Noon))];

        var report = DailyTraffic.Compute(samples, Day1, Day1);

        report.TopPaths.Should().HaveCount(10);
        report.TopPaths.Select(p => p.Key).Should().NotContain("/p10");
    }

    [Fact]
    public void Should_AggregateTopReferrers_When_SamplesHaveSources()
    {
        var report = DailyTraffic.Compute(
        [
            Sample("/", Day1Noon, "https://google.com/search"),
            Sample("/", Day1Noon, "https://www.google.fr"),
            Sample("/", Day1Noon, null),
        ], Day1, Day1);

        report.TopReferrers.Should().Equal(
            new TrafficCount("google", 2),
            new TrafficCount("direct", 1));
    }
}

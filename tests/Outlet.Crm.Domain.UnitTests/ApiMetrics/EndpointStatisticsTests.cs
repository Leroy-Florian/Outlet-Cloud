using Outlet.Crm.Domain.ApiMetrics;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Domain.UnitTests.ApiMetrics;

public sealed class EndpointStatisticsTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    private static readonly ProductId Product = ProductId.New();

    private static ApiMetricSample Sample(string endpoint, int status, double durationMs) =>
        ApiMetricSample.Create(Product, endpoint, status, durationMs, Now).Value!;

    [Fact]
    public void Should_GroupByEndpoint_When_Computing()
    {
        var stats = EndpointStatisticsCalculator.Compute(
        [
            Sample("/api/a", 200, 10),
            Sample("/api/b", 200, 20),
            Sample("/api/a", 500, 30),
        ]);

        stats.Should().HaveCount(2);
        var a = stats.Single(s => s.Endpoint == "/api/a");
        a.RequestCount.Should().Be(2);
        a.ErrorCount.Should().Be(1);
        a.AverageDurationMs.Should().Be(20);
    }

    [Fact]
    public void Should_ComputeP95_When_ManySamples()
    {
        var samples = Enumerable.Range(1, 100).Select(i => Sample("/api/a", 200, i));

        var stats = EndpointStatisticsCalculator.Compute(samples);

        stats.Single().P95DurationMs.Should().Be(95);
    }

    [Fact]
    public void Should_CountOnlyServerErrors_When_StatusIsAtTheBoundary()
    {
        var stats = EndpointStatisticsCalculator.Compute(
        [
            Sample("/api/a", 499, 10),
            Sample("/api/a", 500, 20),
            Sample("/api/a", 503, 30),
        ]);

        stats.Single().ErrorCount.Should().Be(2);
    }

    [Fact]
    public void Should_AcceptZeroDuration_When_CreatingSample()
    {
        var result = ApiMetricSample.Create(Product, "/api/a", 200, 0, Now);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DurationMs.Should().Be(0);
    }

    [Fact]
    public void Should_RejectNegativeDuration_When_CreatingSample()
    {
        var result = ApiMetricSample.Create(Product, "/api/a", 200, -1, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("ApiMetric.NegativeDuration:");
    }

    [Fact]
    public void Should_RejectBlankEndpoint_When_CreatingSample()
    {
        var result = ApiMetricSample.Create(Product, " ", 200, 1, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("ApiMetric.EndpointRequired:");
    }
}

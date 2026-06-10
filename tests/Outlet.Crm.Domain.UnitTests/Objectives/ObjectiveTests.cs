using Outlet.Crm.Domain.Objectives;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Domain.UnitTests.Objectives;

public sealed class ObjectiveTests
{
    private static readonly DateTime SomeInstant = new(2026, 6, 10, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Should_CreateObjective_When_TargetIsPositive()
    {
        var productId = ProductId.New();

        var result = Objective.Create(productId, ObjectiveMetric.Downloads, 5000m, new DateOnly(2026, 6, 1), SomeInstant);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ProductId.Should().Be(productId);
        result.Value.Metric.Should().Be(ObjectiveMetric.Downloads);
        result.Value.TargetValue.Should().Be(5000m);
        result.Value.Month.Should().Be(new DateOnly(2026, 6, 1));
        result.Value.CreatedAt.Should().Be(SomeInstant);
        result.Value.Id.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void Should_NormalizeMonthToFirstDay_When_AnotherDayIsGiven()
    {
        var result = Objective.Create(null, ObjectiveMetric.Revenue, 100m, new DateOnly(2026, 6, 23), SomeInstant);

        result.Value!.Month.Should().Be(new DateOnly(2026, 6, 1));
    }

    [Fact]
    public void Should_AllowGlobalObjective_When_ProductIdIsNull()
    {
        var result = Objective.Create(null, ObjectiveMetric.Prospects, 10m, new DateOnly(2026, 6, 1), SomeInstant);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ProductId.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Should_Fail_When_TargetIsNotPositive(int target)
    {
        var result = Objective.Create(null, ObjectiveMetric.PageViews, target, new DateOnly(2026, 6, 1), SomeInstant);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ObjectiveErrors.TargetNotPositive);
    }

    [Fact]
    public void Should_UpdateTarget_When_NewTargetIsPositive()
    {
        var objective = Objective.Create(null, ObjectiveMetric.Downloads, 100m, new DateOnly(2026, 6, 1), SomeInstant).Value!;

        var updated = objective.UpdateTarget(250m);

        updated.IsSuccess.Should().BeTrue();
        objective.TargetValue.Should().Be(250m);
    }

    [Fact]
    public void Should_RejectUpdate_When_NewTargetIsNotPositive()
    {
        var objective = Objective.Create(null, ObjectiveMetric.Downloads, 100m, new DateOnly(2026, 6, 1), SomeInstant).Value!;

        var updated = objective.UpdateTarget(0m);

        updated.IsFailure.Should().BeTrue();
        updated.Error.Should().Be(ObjectiveErrors.TargetNotPositive);
        objective.TargetValue.Should().Be(100m);
    }

    [Fact]
    public void Should_ComputeRawProgress_When_ActualIsKnown()
    {
        var objective = Objective.Create(null, ObjectiveMetric.Downloads, 200m, new DateOnly(2026, 6, 1), SomeInstant).Value!;

        objective.ProgressPercent(50m).Should().Be(25m);
        objective.ProgressPercent(0m).Should().Be(0m);
        objective.ProgressPercent(1m).Should().Be(0.5m);
    }

    [Fact]
    public void Should_NotCapProgress_When_TargetIsExceeded()
    {
        var objective = Objective.Create(null, ObjectiveMetric.Downloads, 100m, new DateOnly(2026, 6, 1), SomeInstant).Value!;

        objective.ProgressPercent(250m).Should().Be(250m);
    }
}

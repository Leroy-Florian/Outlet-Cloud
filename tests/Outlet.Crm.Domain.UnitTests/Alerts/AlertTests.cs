using Outlet.Crm.Domain.Alerts;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Domain.UnitTests.Alerts;

public sealed class AlertTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Should_CreateUnacknowledgedAlert_When_MessageIsValid()
    {
        var productId = ProductId.New();

        var result = Alert.Create(productId, AlertType.DownloadsSpike, "  Spike detected  ", Now);

        result.IsSuccess.Should().BeTrue();
        var alert = result.Value!;
        alert.ProductId.Should().Be(productId);
        alert.Type.Should().Be(AlertType.DownloadsSpike);
        alert.Message.Should().Be("Spike detected");
        alert.TriggeredAt.Should().Be(Now);
        alert.Acknowledged.Should().BeFalse();
        alert.Id.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void Should_Fail_When_MessageIsBlank()
    {
        var result = Alert.Create(ProductId.New(), AlertType.DownloadsDrop, "  ", Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AlertErrors.MessageRequired);
    }

    [Fact]
    public void Should_Acknowledge_When_NotYetAcknowledged()
    {
        var alert = Alert.Create(ProductId.New(), AlertType.StarsMilestone, "100 stars", Now).Value!;

        var result = alert.Acknowledge();

        result.IsSuccess.Should().BeTrue();
        alert.Acknowledged.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_AcknowledgingTwice()
    {
        var alert = Alert.Create(ProductId.New(), AlertType.SnapshotFailure, "capture failed", Now).Value!;
        alert.Acknowledge();

        var result = alert.Acknowledge();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AlertErrors.AlreadyAcknowledged);
        alert.Acknowledged.Should().BeTrue();
    }
}

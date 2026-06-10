using Outlet.Crm.Application.Alerts;
using Outlet.Crm.Application.UnitTests.Fakes;
using Outlet.Crm.Domain.Alerts;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Application.UnitTests.Alerts;

public sealed class GetAlertsUseCaseTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeAlertRepository _alerts = new();
    private readonly FakeProductRepository _products = new();
    private readonly Product _product = Product.Create("Outlet", null, Now).Value!;

    public GetAlertsUseCaseTests() => _products.Items.Add(_product);

    private GetAlertsUseCase UseCase => new(_alerts, _products);

    private Alert AddAlert(ProductId productId, AlertType type, DateTime triggeredAt, bool acknowledged = false)
    {
        var alert = Alert.Create(productId, type, "message", triggeredAt).Value!;
        if (acknowledged)
        {
            alert.Acknowledge();
        }

        _alerts.Items.Add(alert);
        return alert;
    }

    [Fact]
    public async Task Should_ListUnacknowledgedFirstThenNewestFirst_When_NoFilterIsGiven()
    {
        var acknowledgedNewest = AddAlert(_product.Id, AlertType.DownloadsSpike, Now, acknowledged: true);
        var oldUnacknowledged = AddAlert(_product.Id, AlertType.DownloadsDrop, Now.AddDays(-2));
        var newUnacknowledged = AddAlert(_product.Id, AlertType.StarsMilestone, Now.AddDays(-1));

        var result = await UseCase.HandleAsync(new GetAlertsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().ContainInOrder(newUnacknowledged, oldUnacknowledged, acknowledgedNewest);
    }

    [Fact]
    public async Task Should_FilterByAcknowledged_When_FlagIsGiven()
    {
        AddAlert(_product.Id, AlertType.DownloadsSpike, Now, acknowledged: true);
        var unacknowledged = AddAlert(_product.Id, AlertType.DownloadsDrop, Now);

        var result = await UseCase.HandleAsync(new GetAlertsQuery(Acknowledged: false), CancellationToken.None);

        result.Value!.Should().ContainSingle().Which.Should().Be(unacknowledged);
    }

    [Fact]
    public async Task Should_FilterByProduct_When_ProductIdIsGiven()
    {
        var other = Product.Create("Other", null, Now).Value!;
        _products.Items.Add(other);
        var mine = AddAlert(_product.Id, AlertType.DownloadsSpike, Now);
        AddAlert(other.Id, AlertType.DownloadsDrop, Now);

        var result = await UseCase.HandleAsync(new GetAlertsQuery(_product.Id.Value), CancellationToken.None);

        result.Value!.Should().ContainSingle().Which.Should().Be(mine);
    }

    [Fact]
    public async Task Should_Fail_When_FilteredProductDoesNotExist()
    {
        var result = await UseCase.HandleAsync(new GetAlertsQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.NotFound:");
    }
}

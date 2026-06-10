using Outlet.Crm.Application.Alerts;
using Outlet.Crm.Application.UnitTests.Fakes;
using Outlet.Crm.Domain.Alerts;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Application.UnitTests.Alerts;

public sealed class AcknowledgeAlertUseCaseTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeAlertRepository _alerts = new();

    private AcknowledgeAlertUseCase UseCase => new(_alerts);

    [Fact]
    public async Task Should_AcknowledgeAndPersist_When_AlertExists()
    {
        var alert = Alert.Create(ProductId.New(), AlertType.DownloadsSpike, "spike", Now).Value!;
        _alerts.Items.Add(alert);

        var result = await UseCase.HandleAsync(new AcknowledgeAlertCommand(alert.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        alert.Acknowledged.Should().BeTrue();
        _alerts.UpdateCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_When_AlertDoesNotExist()
    {
        var result = await UseCase.HandleAsync(new AcknowledgeAlertCommand(AlertId.New()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Alert.NotFound:");
    }

    [Fact]
    public async Task Should_Fail_When_AlertIsAlreadyAcknowledged()
    {
        var alert = Alert.Create(ProductId.New(), AlertType.DownloadsDrop, "drop", Now).Value!;
        alert.Acknowledge();
        _alerts.Items.Add(alert);

        var result = await UseCase.HandleAsync(new AcknowledgeAlertCommand(alert.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AlertErrors.AlreadyAcknowledged);
        _alerts.UpdateCount.Should().Be(0);
    }
}

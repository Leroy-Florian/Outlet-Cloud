using Outlet.Crm.Application.Objectives;
using Outlet.Crm.Application.UnitTests.Fakes;
using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Objectives;
using Outlet.Crm.Domain.Payments;
using Outlet.Crm.Domain.Products;
using Outlet.Crm.Domain.Prospects;
using Outlet.Crm.Domain.Traffic;

namespace Outlet.Crm.Application.UnitTests.Objectives;

public sealed class ObjectiveUseCaseTests
{
    private static readonly DateTime Now = new(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly June = new(2026, 6, 1);

    private readonly FakeObjectiveRepository _objectives = new();
    private readonly FakeProductRepository _products = new();
    private readonly FakeDownloadSnapshotRepository _downloads = new();
    private readonly FakeTrafficSampleRepository _traffic = new();
    private readonly FakePaymentRepository _payments = new();
    private readonly FakeProspectRepository _prospects = new();
    private readonly Product _product = Product.Create("Outlet", null, Now).Value!;

    public ObjectiveUseCaseTests() => _products.Items.Add(_product);

    private SetObjectiveUseCase SetUseCase => new(_objectives, _products, new FixedClock(Now));

    private DeleteObjectiveUseCase DeleteUseCase => new(_objectives);

    private GetObjectivesProgressUseCase ProgressUseCase =>
        new(_objectives, _products, _downloads, _traffic, _payments, _prospects, new FixedClock(Now));

    private Objective AddObjective(ObjectiveMetric metric, decimal target, ProductId? productId = null)
    {
        var objective = Objective.Create(productId, metric, target, June, Now).Value!;
        _objectives.Items.Add(objective);
        return objective;
    }

    [Fact]
    public async Task Should_CreateObjective_When_TripleIsNew()
    {
        var result = await SetUseCase.HandleAsync(
            new SetObjectiveCommand(_product.Id.Value, ObjectiveMetric.Downloads, "2026-06", 5000m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var objective = _objectives.Items.Should().ContainSingle().Subject;
        objective.ProductId.Should().Be(_product.Id);
        objective.Month.Should().Be(June);
        objective.TargetValue.Should().Be(5000m);
        result.Value.Should().Be(objective.Id.Value);
    }

    [Fact]
    public async Task Should_UpdateTarget_When_TripleAlreadyHasAnObjective()
    {
        var existing = AddObjective(ObjectiveMetric.Downloads, 100m, _product.Id);

        var result = await SetUseCase.HandleAsync(
            new SetObjectiveCommand(_product.Id.Value, ObjectiveMetric.Downloads, "2026-06", 900m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(existing.Id.Value);
        _objectives.Items.Should().ContainSingle().Which.TargetValue.Should().Be(900m);
        _objectives.UpdateCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_When_MonthFormatIsInvalid()
    {
        var result = await SetUseCase.HandleAsync(
            new SetObjectiveCommand(null, ObjectiveMetric.Downloads, "juin 2026", 100m), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ObjectiveErrors.InvalidMonth);
        _objectives.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Fail_When_ProductDoesNotExist()
    {
        var result = await SetUseCase.HandleAsync(
            new SetObjectiveCommand(Guid.NewGuid(), ObjectiveMetric.Downloads, "2026-06", 100m), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Product.NotFound:");
    }

    [Fact]
    public async Task Should_Fail_When_TargetIsNotPositive()
    {
        var result = await SetUseCase.HandleAsync(
            new SetObjectiveCommand(null, ObjectiveMetric.Revenue, "2026-06", 0m), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ObjectiveErrors.TargetNotPositive);
    }

    [Fact]
    public async Task Should_RejectNonPositiveTarget_When_UpdatingAnExistingObjective()
    {
        AddObjective(ObjectiveMetric.Downloads, 100m, _product.Id);

        var result = await SetUseCase.HandleAsync(
            new SetObjectiveCommand(_product.Id.Value, ObjectiveMetric.Downloads, "2026-06", -1m), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ObjectiveErrors.TargetNotPositive);
        _objectives.Items.Should().ContainSingle().Which.TargetValue.Should().Be(100m);
    }

    [Fact]
    public async Task Should_RemoveObjective_When_ItExists()
    {
        var objective = AddObjective(ObjectiveMetric.Downloads, 100m);

        var result = await DeleteUseCase.HandleAsync(new DeleteObjectiveCommand(objective.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _objectives.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Fail_When_DeletingUnknownObjective()
    {
        var result = await DeleteUseCase.HandleAsync(
            new DeleteObjectiveCommand(ObjectiveId.New()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Objective.NotFound:");
    }

    [Fact]
    public async Task Should_ComputeDownloadsDelta_When_ObjectiveTargetsDownloads()
    {
        AddObjective(ObjectiveMetric.Downloads, 600m, _product.Id);
        _downloads.Items.Add(Snapshot(new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc), 100));
        _downloads.Items.Add(Snapshot(new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc), 400));

        var result = await ProgressUseCase.HandleAsync(new GetObjectivesProgressQuery("2026-06"), CancellationToken.None);

        var progress = result.Value!.Objectives.Should().ContainSingle().Subject;
        progress.ActualValue.Should().Be(300m);
        progress.ProgressPercent.Should().Be(50m);
    }

    [Fact]
    public async Task Should_CountMonthPageViews_When_ObjectiveTargetsPageViews()
    {
        AddObjective(ObjectiveMetric.PageViews, 4m, _product.Id);
        _traffic.Items.Add(Sample(new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc)));
        _traffic.Items.Add(Sample(new DateTime(2026, 6, 30, 23, 0, 0, DateTimeKind.Utc)));
        _traffic.Items.Add(Sample(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)));

        var result = await ProgressUseCase.HandleAsync(new GetObjectivesProgressQuery("2026-06"), CancellationToken.None);

        var progress = result.Value!.Objectives.Should().ContainSingle().Subject;
        progress.ActualValue.Should().Be(2m);
        progress.ProgressPercent.Should().Be(50m);
    }

    [Fact]
    public async Task Should_SumSettledRevenueOfMonth_When_ObjectiveTargetsRevenue()
    {
        AddObjective(ObjectiveMetric.Revenue, 100m, _product.Id);
        var settled = PaymentAt(new DateTime(2026, 6, 3, 0, 0, 0, DateTimeKind.Utc), 60m);
        settled.Settle();
        _payments.Items.Add(settled);
        _payments.Items.Add(PaymentAt(new DateTime(2026, 6, 4, 0, 0, 0, DateTimeKind.Utc), 40m));
        var outside = PaymentAt(new DateTime(2026, 5, 4, 0, 0, 0, DateTimeKind.Utc), 99m);
        outside.Settle();
        _payments.Items.Add(outside);
        var otherProduct = Product.Create("FluxPDF", null, Now).Value!;
        _products.Items.Add(otherProduct);
        var foreign = PaymentAt(new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc), 33m, otherProduct.Id);
        foreign.Settle();
        _payments.Items.Add(foreign);

        var result = await ProgressUseCase.HandleAsync(new GetObjectivesProgressQuery("2026-06"), CancellationToken.None);

        result.Value!.Objectives.Should().ContainSingle().Which.ActualValue.Should().Be(60m);
    }

    [Fact]
    public async Task Should_CountProspectsCreatedInMonth_When_ObjectiveTargetsProspects()
    {
        AddObjective(ObjectiveMetric.Prospects, 4m);
        _prospects.Items.Add(ProspectAt(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)));
        _prospects.Items.Add(ProspectAt(new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc)));
        _prospects.Items.Add(ProspectAt(new DateTime(2026, 5, 30, 0, 0, 0, DateTimeKind.Utc)));
        _prospects.Items.Add(ProspectAt(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)));

        var result = await ProgressUseCase.HandleAsync(new GetObjectivesProgressQuery("2026-06"), CancellationToken.None);

        var progress = result.Value!.Objectives.Should().ContainSingle().Subject;
        progress.ActualValue.Should().Be(2m);
        progress.ProgressPercent.Should().Be(50m);
    }

    [Fact]
    public async Task Should_IgnoreOtherProductsProspects_When_ObjectiveIsProductScoped()
    {
        var other = Product.Create("FluxPDF", null, Now).Value!;
        _products.Items.Add(other);
        AddObjective(ObjectiveMetric.Prospects, 2m, _product.Id);
        _prospects.Items.Add(ProspectAt(new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc)));
        _prospects.Items.Add(ProspectAt(new DateTime(2026, 6, 7, 0, 0, 0, DateTimeKind.Utc)));
        _prospects.Items.Add(ProspectAt(new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc), other.Id));

        var result = await ProgressUseCase.HandleAsync(new GetObjectivesProgressQuery("2026-06"), CancellationToken.None);

        result.Value!.Objectives.Should().ContainSingle().Which.ActualValue.Should().Be(2m);
    }

    [Fact]
    public async Task Should_AggregateAcrossProducts_When_ObjectiveIsGlobal()
    {
        var other = Product.Create("FluxPDF", null, Now).Value!;
        _products.Items.Add(other);
        AddObjective(ObjectiveMetric.Downloads, 1000m);
        _downloads.Items.Add(Snapshot(new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc), 0));
        _downloads.Items.Add(Snapshot(new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc), 200));
        _downloads.Items.Add(Snapshot(new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc), 0, other.Id));
        _downloads.Items.Add(Snapshot(new DateTime(2026, 6, 6, 0, 0, 0, DateTimeKind.Utc), 300, other.Id));

        var result = await ProgressUseCase.HandleAsync(new GetObjectivesProgressQuery("2026-06"), CancellationToken.None);

        result.Value!.Objectives.Should().ContainSingle().Which.ActualValue.Should().Be(500m);
    }

    [Fact]
    public async Task Should_DefaultToCurrentMonth_When_NoMonthIsGiven()
    {
        AddObjective(ObjectiveMetric.Downloads, 100m, _product.Id);

        var result = await ProgressUseCase.HandleAsync(new GetObjectivesProgressQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Month.Should().Be(June);
        result.Value.Objectives.Should().HaveCount(1);
    }

    [Fact]
    public async Task Should_Fail_When_ProgressMonthIsInvalid()
    {
        var result = await ProgressUseCase.HandleAsync(new GetObjectivesProgressQuery("06/2026"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ObjectiveErrors.InvalidMonth);
    }

    private DownloadSnapshot Snapshot(DateTime capturedAt, long total, ProductId? productId = null) =>
        DownloadSnapshot.Create(
            productId ?? _product.Id, PackageRegistry.NuGet, PackageId.Create("outlet").Value!, total, capturedAt).Value!;

    private TrafficSample Sample(DateTime occurredAt) =>
        TrafficSample.Create(_product.Id, "/docs", null, null, occurredAt).Value!;

    private Payment PaymentAt(DateTime createdAt, decimal amount, ProductId? productId = null) =>
        Payment.Create(productId ?? _product.Id, null, Money.Create(amount, "EUR").Value!, "stripe", "pi", createdAt).Value!;

    private Prospect ProspectAt(DateTime createdAt, ProductId? productId = null) =>
        Prospect.Create(productId ?? _product.Id, null, "Jane", Email.Create("jane@acme.test").Value!, null, null, createdAt).Value!;
}

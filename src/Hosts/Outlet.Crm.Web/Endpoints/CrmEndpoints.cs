using Outlet.Crm.Application.Alerts;
using Outlet.Crm.Application.Analytics;
using Outlet.Crm.Application.ApiMetrics;
using Outlet.Crm.Application.Feedback;
using Outlet.Crm.Application.Invoices;
using Outlet.Crm.Application.Objectives;
using Outlet.Crm.Application.Organizations;
using Outlet.Crm.Application.Payments;
using Outlet.Crm.Application.Products;
using Outlet.Crm.Application.Ports;
using Outlet.Crm.Application.Prospects;
using Outlet.Crm.Application.Traffic;
using Outlet.Crm.Domain.Alerts;
using Outlet.Crm.Domain.Analytics;
using Outlet.Crm.Domain.Feedback;
using Outlet.Crm.Domain.Invoices;
using Outlet.Crm.Domain.Objectives;
using Outlet.Crm.Domain.Prospects;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Web.Endpoints;

/// <summary>
/// PRIMARY ADAPTER — the CRM REST surface under /api (the dashboard's dev proxy
/// forwards /api → this host). Thin: bind, call the use case, map Result to HTTP.
/// </summary>
public static class CrmEndpoints
{
    public static void MapOutletCrm(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        var products = api.MapGroup("/products");

        products.MapGet("/", async (IProductRepository repository, CancellationToken ct) =>
            Results.Ok((await repository.ListAsync(ct)).Select(p => new
            {
                id = p.Id.Value,
                p.Name,
                p.Description,
                packages = p.Packages.Select(t => new { registry = t.Registry.ToString(), packageId = t.PackageId.Value }),
                repositories = p.Repositories.Select(r => r.Repository.FullName),
                p.IsArchived,
                p.CreatedAt,
            })));

        products.MapPost("/", async (CreateProductCommand command, CreateProductUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(command, ct), id => Results.Created($"/api/products/{id.Value}", new { id = id.Value })));

        products.MapPut("/{productId:guid}", async (Guid productId, UpdateProductRequest request, UpdateProductUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(new UpdateProductCommand(productId, request.Name, request.Description), ct)));

        products.MapDelete("/{productId:guid}", async (Guid productId, ArchiveProductUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(new ArchiveProductCommand(productId), ct)));

        products.MapDelete("/{productId:guid}/packages/{registry}/{packageId}",
            async (Guid productId, PackageRegistry registry, string packageId, UntrackPackageUseCase useCase, CancellationToken ct) =>
                ToHttp(await useCase.HandleAsync(new UntrackPackageCommand(productId, registry, packageId), ct)));

        products.MapDelete("/{productId:guid}/repositories/{owner}/{name}",
            async (Guid productId, string owner, string name, UntrackRepositoryUseCase useCase, CancellationToken ct) =>
                ToHttp(await useCase.HandleAsync(new UntrackRepositoryCommand(productId, $"{owner}/{name}"), ct)));

        products.MapGet("/{productId:guid}/analytics/downloads/daily",
            async (Guid productId, DateOnly? from, DateOnly? to, GetProductDailyDownloadsUseCase useCase, CancellationToken ct) =>
                ToHttp(await useCase.HandleAsync(new GetProductDailyDownloadsQuery(productId, from, to), ct), Results.Ok));

        products.MapGet("/{productId:guid}/analytics/traffic/daily",
            async (Guid productId, DateOnly? from, DateOnly? to, GetDailyTrafficUseCase useCase, CancellationToken ct) =>
                ToHttp(await useCase.HandleAsync(new GetDailyTrafficQuery(productId, from, to), ct), Results.Ok));

        products.MapGet("/{productId:guid}/analytics/summary",
            async (Guid productId, int? days, GetProductAnalyticsSummaryUseCase useCase, CancellationToken ct) =>
                ToHttp(await useCase.HandleAsync(new GetProductAnalyticsSummaryQuery(productId, days), ct), Results.Ok));

        api.MapGet("/analytics/portfolio", async (int? days, GetPortfolioUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(new GetPortfolioQuery(days), ct), Results.Ok));

        products.MapPost("/{productId:guid}/packages", async (Guid productId, TrackPackageRequest request, TrackPackageUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(new TrackPackageCommand(productId, request.Registry, request.PackageId), ct)));

        products.MapPost("/{productId:guid}/repositories", async (Guid productId, TrackRepositoryRequest request, TrackRepositoryUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(new TrackRepositoryCommand(productId, request.Repository), ct)));

        products.MapPost("/{productId:guid}/snapshots", async (Guid productId, CaptureProductSnapshotsUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(new CaptureProductSnapshotsCommand(productId), ct), Results.Ok));

        products.MapGet("/{productId:guid}/packages/{registry}/{packageId}/trend",
            async (Guid productId, PackageRegistry registry, string packageId, GetDownloadTrendUseCase useCase, CancellationToken ct) =>
                ToHttp(await useCase.HandleAsync(new GetDownloadTrendQuery(productId, registry, packageId), ct), Results.Ok));

        products.MapGet("/{productId:guid}/repositories/{owner}/{name}/history",
            async (Guid productId, string owner, string name, GetRepositoryHistoryUseCase useCase, CancellationToken ct) =>
                ToHttp(await useCase.HandleAsync(new GetRepositoryHistoryQuery(productId, $"{owner}/{name}"), ct), history =>
                    Results.Ok(history.Select(s => new
                    {
                        repository = s.Repository.FullName,
                        s.OpenIssues,
                        s.Stars,
                        s.Forks,
                        s.CapturedAt,
                    }))));

        products.MapGet("/{productId:guid}/metrics/statistics",
            async (Guid productId, DateTime since, GetEndpointStatisticsUseCase useCase, CancellationToken ct) =>
                ToHttp(await useCase.HandleAsync(new GetEndpointStatisticsQuery(productId, since), ct), Results.Ok));

        products.MapGet("/{productId:guid}/health", async (Guid productId, GetProductHealthUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(new GetProductHealthQuery(productId), ct), health => Results.Ok(new
            {
                health.Total,
                health.Label,
                components = new
                {
                    releaseFreshness = health.ReleaseFreshnessScore,
                    downloadTrend = health.DownloadTrendScore,
                    repoActivity = health.RepoActivityScore,
                    snapshotReliability = health.SnapshotReliabilityScore,
                },
                inputs = new
                {
                    health.Inputs.DaysSinceLatestRelease,
                    health.Inputs.DownloadsPercentChange,
                    health.Inputs.OpenIssuesGrowthPercent,
                    health.Inputs.StarsGrowthPercent,
                    health.Inputs.RecentCaptureFailures,
                },
            })));

        var objectives = api.MapGroup("/objectives");

        objectives.MapPut("/", async (SetObjectiveCommand command, SetObjectiveUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(command, ct), id => Results.Ok(new { id })));

        objectives.MapDelete("/{id:guid}", async (Guid id, DeleteObjectiveUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(new DeleteObjectiveCommand(new ObjectiveId(id)), ct)));

        objectives.MapGet("/progress", async (string? month, GetObjectivesProgressUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(new GetObjectivesProgressQuery(month), ct), report => Results.Ok(new
            {
                month = $"{report.Month.Year:D4}-{report.Month.Month:D2}",
                objectives = report.Objectives.Select(o => new
                {
                    o.Id,
                    o.ProductId,
                    metric = o.Metric.ToString(),
                    o.TargetValue,
                    o.ActualValue,
                    o.ProgressPercent,
                }),
            })));

        var invoices = api.MapGroup("/invoices");

        invoices.MapPost("/", async (CreateInvoiceCommand command, CreateInvoiceUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(command, ct), created =>
                Results.Created($"/api/invoices/{created.Id}", new { id = created.Id, invoiceNumber = created.InvoiceNumber })));

        invoices.MapGet("/", async (InvoiceStatus? status, GetInvoicesUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(new GetInvoicesQuery(status), ct), items =>
                Results.Ok(items.Select(ToInvoiceResponse))));

        invoices.MapPost("/{id:guid}/issue", async (Guid id, IssueInvoiceUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(new IssueInvoiceCommand(new InvoiceId(id)), ct)));

        invoices.MapPost("/{id:guid}/pay", async (Guid id, PayInvoiceRequest? request, MarkInvoicePaidUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(new MarkInvoicePaidCommand(new InvoiceId(id), request?.PaymentId), ct)));

        invoices.MapPost("/{id:guid}/cancel", async (Guid id, CancelInvoiceUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(new CancelInvoiceCommand(new InvoiceId(id)), ct)));

        var organizations = api.MapGroup("/organizations");

        organizations.MapGet("/", async (IOrganizationRepository repository, CancellationToken ct) =>
            Results.Ok((await repository.ListAsync(ct)).Select(o => new
            {
                id = o.Id.Value,
                o.Name,
                o.Website,
                o.CreatedAt,
            })));

        organizations.MapPost("/", async (CreateOrganizationCommand command, CreateOrganizationUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(command, ct), id => Results.Created($"/api/organizations/{id.Value}", new { id = id.Value })));

        organizations.MapGet("/{organizationId:guid}/payments", async (Guid organizationId, IPaymentRepository repository, CancellationToken ct) =>
            Results.Ok((await repository.ListAsync(ct))
                .Where(p => p.OrganizationId?.Value == organizationId)
                .Select(p => new
                {
                    p.Id,
                    productId = p.ProductId.Value,
                    amount = p.Amount.Amount,
                    currency = p.Amount.Currency,
                    p.Source,
                    status = p.Status.ToString(),
                    p.CreatedAt,
                })));

        var prospects = api.MapGroup("/prospects");

        prospects.MapGet("/", async (IProspectRepository repository, CancellationToken ct) =>
            Results.Ok((await repository.ListAsync(ct)).Select(p => new
            {
                id = p.Id.Value,
                productId = p.ProductId.Value,
                organizationId = p.OrganizationId == null ? (Guid?)null : p.OrganizationId.Value.Value,
                p.Name,
                email = p.Email.Value,
                p.Company,
                estimatedValue = p.EstimatedValue?.Amount,
                estimatedValueCurrency = p.EstimatedValue?.Currency,
                stage = p.Stage.ToString(),
                p.LossReason,
                p.CreatedAt,
            })));

        prospects.MapGet("/stats", async (GetProspectPipelineStatsUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(new GetProspectPipelineStatsQuery(), ct), report => Results.Ok(new
            {
                report.TotalProspects,
                report.TotalEstimatedValue,
                stages = report.Stages.Select(s => new
                {
                    stage = s.Stage.ToString(),
                    s.Count,
                    s.TotalEstimatedValue,
                    s.ConversionRateToNext,
                }),
            })));

        prospects.MapPost("/", async (CreateProspectCommand command, CreateProspectUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(command, ct), id => Results.Created($"/api/prospects/{id.Value}", new { id = id.Value })));

        prospects.MapPost("/{id:guid}/stage", async (Guid id, ProspectStage target, AdvanceProspectStageUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(new AdvanceProspectStageCommand(new ProspectId(id), target), ct)));

        prospects.MapPatch("/{id:guid}", async (Guid id, UpdateProspectRequest request, UpdateProspectUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(
                new UpdateProspectCommand(new ProspectId(id), request.EstimatedValue, request.EstimatedValueCurrency, request.Company), ct)));

        prospects.MapPost("/{id:guid}/lose", async (Guid id, LoseProspectRequest request, LoseProspectUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(new LoseProspectCommand(new ProspectId(id), request.Reason), ct)));

        var feedback = api.MapGroup("/feedback");

        feedback.MapPost("/", async (SubmitFeedbackCommand command, SubmitFeedbackUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(command, ct), id => Results.Created($"/api/feedback/{id.Value}", new { id = id.Value })));

        feedback.MapGet("/", async (Guid? productId, FeedbackStatus? status, FeedbackCategory? category, GetFeedbackInboxUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(new GetFeedbackInboxQuery(productId, status, category), ct), inbox => Results.Ok(new
            {
                items = inbox.Items.Select(f => new
                {
                    id = f.Id.Value,
                    productId = f.ProductId.Value,
                    category = f.Category.ToString(),
                    f.Message,
                    reporterEmail = f.ReporterEmail?.Value,
                    f.SourceApp,
                    status = f.Status.ToString(),
                    f.ReceivedAt,
                }),
                counts = new
                {
                    inbox.Counts.New,
                    inbox.Counts.Triaged,
                    inbox.Counts.Resolved,
                    inbox.Counts.Dismissed,
                    inbox.Counts.Total,
                },
            })));

        feedback.MapPost("/{id:guid}/triage", async (Guid id, TriageFeedbackUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(new TriageFeedbackCommand(new FeedbackId(id)), ct)));

        feedback.MapPost("/{id:guid}/resolve", async (Guid id, ResolveFeedbackUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(new ResolveFeedbackCommand(new FeedbackId(id)), ct)));

        feedback.MapPost("/{id:guid}/dismiss", async (Guid id, DismissFeedbackUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(new DismissFeedbackCommand(new FeedbackId(id)), ct)));

        var traffic = api.MapGroup("/traffic");

        traffic.MapPost("/", async (RecordTrafficEventCommand command, RecordTrafficEventUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(command, ct)));

        var metrics = api.MapGroup("/metrics");

        metrics.MapPost("/", async (RecordApiMetricCommand command, RecordApiMetricUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(command, ct)));

        var payments = api.MapGroup("/payments");

        payments.MapGet("/", async (IPaymentRepository repository, CancellationToken ct) =>
            Results.Ok((await repository.ListAsync(ct)).Select(p => new
            {
                p.Id,
                productId = p.ProductId.Value,
                organizationId = p.OrganizationId == null ? (Guid?)null : p.OrganizationId.Value.Value,
                amount = p.Amount.Amount,
                currency = p.Amount.Currency,
                p.Source,
                p.ExternalReference,
                p.IsRecurring,
                status = p.Status.ToString(),
                p.CreatedAt,
            })));

        payments.MapPost("/", async (RecordPaymentCommand command, RecordPaymentUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(command, ct), id => Results.Created($"/api/payments/{id}", new { id })));

        payments.MapPost("/{id:guid}/settle", async (Guid id, SettlePaymentUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(new SettlePaymentCommand(id), ct)));

        api.MapGet("/revenue/metrics", async (int? months, GetRevenueMetricsUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(new GetRevenueMetricsQuery(months), ct), report => Results.Ok(new
            {
                report.PrimaryCurrency,
                report.Months,
                mrr = report.MonthlyRecurringRevenue,
                report.ChurnMonths,
                series = report.Series.Select(point => new
                {
                    month = $"{point.Year:D4}-{point.Month:D2}",
                    point.Total,
                    point.Recurring,
                    point.Cumulative,
                    byProduct = point.ByProduct.Select(p => new { productId = p.ProductId, p.Amount }),
                }),
                currencyTotals = report.CurrencyTotals.Select(c => new { c.Currency, c.Total }),
            })));

        var alerts = api.MapGroup("/alerts");

        alerts.MapGet("/", async (Guid? productId, bool? acknowledged, GetAlertsUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(new GetAlertsQuery(productId, acknowledged), ct), items =>
                Results.Ok(items.Select(ToAlertResponse))));

        alerts.MapPost("/{id:guid}/acknowledge", async (Guid id, AcknowledgeAlertUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(new AcknowledgeAlertCommand(new AlertId(id)), ct)));

        products.MapPost("/{productId:guid}/alerts/evaluate", async (Guid productId, EvaluateAlertsUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(new EvaluateAlertsCommand(productId), ct), created =>
                Results.Ok(created.Select(ToAlertResponse))));
    }

    private static object ToAlertResponse(Alert alert) => new
    {
        id = alert.Id.Value,
        productId = alert.ProductId.Value,
        type = alert.Type.ToString(),
        alert.Message,
        alert.TriggeredAt,
        alert.Acknowledged,
    };

    private sealed record TrackPackageRequest(PackageRegistry Registry, string PackageId);

    private sealed record UpdateProductRequest(string Name, string? Description);

    private sealed record TrackRepositoryRequest(string Repository);

    private sealed record UpdateProspectRequest(decimal? EstimatedValue, string? EstimatedValueCurrency, string? Company);

    private sealed record LoseProspectRequest(string Reason);

    private sealed record PayInvoiceRequest(Guid? PaymentId);

    private static object ToInvoiceResponse(Invoice invoice) => new
    {
        id = invoice.Id.Value,
        invoice.InvoiceNumber,
        invoice.CustomerName,
        customerEmail = invoice.CustomerEmail?.Value,
        invoice.CustomerAddress,
        status = invoice.Status.ToString(),
        invoice.Currency,
        invoice.Total,
        lines = invoice.Lines.Select(l => new
        {
            l.Description,
            l.Quantity,
            unitPrice = l.UnitPrice.Amount,
            l.LineTotal,
        }),
        invoice.CreatedAt,
        invoice.IssuedAt,
        invoice.PaidAt,
        invoice.PaymentId,
    };

    private static IResult ToHttp(Result result) =>
        result.IsSuccess ? Results.NoContent() : ToProblem(result.Error!);

    private static IResult ToHttp<TValue>(Result<TValue> result, Func<TValue, IResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value!) : ToProblem(result.Error!);

    /// <summary>
    /// Errors travel as "{Code}: {message}" strings (kernel Result convention);
    /// the code prefix decides the HTTP status, the rest fills the problem detail.
    /// </summary>
    private static IResult ToProblem(string error)
    {
        var separator = error.IndexOf(':', StringComparison.Ordinal);
        var code = separator > 0 ? error[..separator] : "Error";
        var detail = separator > 0 ? error[(separator + 1)..].Trim() : error;

        return Results.Problem(
            title: code,
            detail: detail,
            statusCode: code.Contains("NotFound", StringComparison.Ordinal) || code.Contains("NotTracked", StringComparison.Ordinal) ? 404 : 400);
    }
}

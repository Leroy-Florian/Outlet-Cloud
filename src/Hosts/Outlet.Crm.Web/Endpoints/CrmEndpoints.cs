using Outlet.Crm.Application.Analytics;
using Outlet.Crm.Application.ApiMetrics;
using Outlet.Crm.Application.Organizations;
using Outlet.Crm.Application.Payments;
using Outlet.Crm.Application.Products;
using Outlet.Crm.Application.Ports;
using Outlet.Crm.Application.Prospects;
using Outlet.Crm.Domain.Analytics;
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
                p.CreatedAt,
            })));

        products.MapPost("/", async (CreateProductCommand command, CreateProductUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(command, ct), id => Results.Created($"/api/products/{id.Value}", new { id = id.Value })));

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
                stage = p.Stage.ToString(),
                p.CreatedAt,
            })));

        prospects.MapPost("/", async (CreateProspectCommand command, CreateProspectUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(command, ct), id => Results.Created($"/api/prospects/{id.Value}", new { id = id.Value })));

        prospects.MapPost("/{id:guid}/stage", async (Guid id, ProspectStage target, AdvanceProspectStageUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(new AdvanceProspectStageCommand(new ProspectId(id), target), ct)));

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
                status = p.Status.ToString(),
                p.CreatedAt,
            })));

        payments.MapPost("/", async (RecordPaymentCommand command, RecordPaymentUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(command, ct), id => Results.Created($"/api/payments/{id}", new { id })));

        payments.MapPost("/{id:guid}/settle", async (Guid id, SettlePaymentUseCase useCase, CancellationToken ct) =>
            ToHttp(await useCase.HandleAsync(new SettlePaymentCommand(id), ct)));
    }

    private sealed record TrackPackageRequest(PackageRegistry Registry, string PackageId);

    private sealed record TrackRepositoryRequest(string Repository);

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

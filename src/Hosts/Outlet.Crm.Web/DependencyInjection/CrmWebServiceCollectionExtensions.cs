using Outlet.Crm.Application.Analytics;
using Outlet.Crm.Application.ApiMetrics;
using Outlet.Crm.Application.Organizations;
using Outlet.Crm.Application.Payments;
using Outlet.Crm.Application.Products;
using Outlet.Crm.Application.Prospects;
using Outlet.Kernel.Shared;

namespace Outlet.Crm.Web.DependencyInjection;

/// <summary>
/// Wires the Outlet Crm composition root: the context's use cases and the clock.
/// Persistence and HTTP adapters come from <c>AddOutletCrmInfrastructure</c> /
/// <c>AddOutletCrmStatsClients</c>.
/// </summary>
public static class CrmWebServiceCollectionExtensions
{
    public static IServiceCollection AddOutletCrmWeb(this IServiceCollection services)
    {
        services.AddSingleton<ICurrentDateTimeProvider, UtcDateTimeProvider>();

        services.AddScoped<CreateProductUseCase>();
        services.AddScoped<TrackPackageUseCase>();
        services.AddScoped<TrackRepositoryUseCase>();
        services.AddScoped<CreateOrganizationUseCase>();
        services.AddScoped<CreateProspectUseCase>();
        services.AddScoped<AdvanceProspectStageUseCase>();
        services.AddScoped<CaptureDownloadSnapshotUseCase>();
        services.AddScoped<CaptureProductSnapshotsUseCase>();
        services.AddScoped<GetDownloadTrendUseCase>();
        services.AddScoped<GetRepositoryHistoryUseCase>();
        services.AddScoped<RecordApiMetricUseCase>();
        services.AddScoped<GetEndpointStatisticsUseCase>();
        services.AddScoped<RecordPaymentUseCase>();
        services.AddScoped<SettlePaymentUseCase>();

        return services;
    }
}

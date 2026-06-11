using Outlet.Crm.Application.Alerts;
using Outlet.Crm.Application.Analytics;
using Outlet.Crm.Application.ApiMetrics;
using Outlet.Crm.Application.Feedback;
using Outlet.Crm.Application.Objectives;
using Outlet.Crm.Application.Organizations;
using Outlet.Crm.Application.Payments;
using Outlet.Crm.Application.Products;
using Outlet.Crm.Application.Prospects;
using Outlet.Crm.Application.Traffic;
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
        services.AddScoped<UpdateProductUseCase>();
        services.AddScoped<ArchiveProductUseCase>();
        services.AddScoped<UntrackPackageUseCase>();
        services.AddScoped<UntrackRepositoryUseCase>();
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
        services.AddScoped<GetProductDailyDownloadsUseCase>();
        services.AddScoped<GetProductAnalyticsSummaryUseCase>();
        services.AddScoped<RecordTrafficEventUseCase>();
        services.AddScoped<GetDailyTrafficUseCase>();
        services.AddScoped<RecordPaymentUseCase>();
        services.AddScoped<ProcessBillingEventUseCase>();
        services.AddScoped<SettlePaymentUseCase>();
        services.AddScoped<GetPortfolioUseCase>();
        services.AddScoped<UpdateProspectUseCase>();
        services.AddScoped<LoseProspectUseCase>();
        services.AddScoped<GetProspectPipelineStatsUseCase>();
        services.AddScoped<SubmitFeedbackUseCase>();
        services.AddScoped<TriageFeedbackUseCase>();
        services.AddScoped<ResolveFeedbackUseCase>();
        services.AddScoped<DismissFeedbackUseCase>();
        services.AddScoped<GetFeedbackInboxUseCase>();
        services.AddScoped<EvaluateAlertsUseCase>();
        services.AddScoped<AcknowledgeAlertUseCase>();
        services.AddScoped<GetAlertsUseCase>();
        services.AddScoped<GetRevenueMetricsUseCase>();
        services.AddScoped<GetProductHealthUseCase>();
        services.AddScoped<SetObjectiveUseCase>();
        services.AddScoped<DeleteObjectiveUseCase>();
        services.AddScoped<GetObjectivesProgressUseCase>();

        return services;
    }
}

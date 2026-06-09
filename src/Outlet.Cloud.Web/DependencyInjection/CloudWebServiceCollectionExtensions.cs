using Microsoft.Extensions.DependencyInjection;
using Outlet.Cloud.Application.Organizations;
using Outlet.Cloud.Application.Registry;
using Outlet.Cloud.Application.Subscriptions;
using Outlet.Cloud.Web.Authentication;
using Outlet.Cloud.Web.Composition;
using Outlet.Identity.Application.AccessTokens;
using Outlet.Identity.Application.Users;
using Outlet.Kernel.Shared;

namespace Outlet.Cloud.Web.DependencyInjection;

/// <summary>
/// Wires the Outlet Cloud composition root: the use cases of both bounded contexts,
/// the cross-context token issuer and the bearer authenticator. Persistence adapters
/// come from <c>AddOutletIdentityInfrastructure</c> / <c>AddOutletCloudInfrastructure</c>.
/// </summary>
public static class CloudWebServiceCollectionExtensions
{
    public static IServiceCollection AddOutletCloudWeb(this IServiceCollection services)
    {
        services.AddSingleton<ICurrentDateTimeProvider, UtcDateTimeProvider>();

        services.AddScoped<RegisterUserUseCase>();
        services.AddScoped<IssuePersonalAccessTokenUseCase>();
        services.AddScoped<RevokePersonalAccessTokenUseCase>();

        services.AddScoped<CreateOrganizationUseCase>();
        services.AddScoped<AddMemberUseCase>();
        services.AddScoped<ChangeMemberRoleUseCase>();
        services.AddScoped<RemoveMemberUseCase>();
        services.AddScoped<PublishItemUseCase>();

        services.AddScoped<SubscriptionEntitlementResolver>();
        services.AddScoped<StartTrialUseCase>();
        services.AddScoped<ConvertSubscriptionUseCase>();
        services.AddScoped<CancelSubscriptionUseCase>();
        services.AddScoped<GetEntitlementsUseCase>();

        services.AddScoped<OrganizationTokenIssuer>();
        services.AddScoped<PersonalAccessTokenAuthenticator>();

        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;

namespace Outlet.Registry.Auth;

/// <summary>
/// DI wiring for the SHA-256 personal access token authenticator. You still register your
/// own <see cref="IPersonalAccessTokenStore"/> (over your persistence) and a
/// <see cref="TimeProvider"/> (e.g. <c>services.AddSingleton(TimeProvider.System)</c>).
/// </summary>
public static class PersonalAccessTokenAuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddSha256PersonalAccessTokenAuthentication(
        this IServiceCollection services,
        Action<PersonalAccessTokenAuthenticationOptions>? configure = null)
    {
        services.Configure(configure ?? (_ => { }));
        services.AddScoped<IPersonalAccessTokenAuthenticator, Sha256PersonalAccessTokenAuthenticator>();

        return services;
    }
}

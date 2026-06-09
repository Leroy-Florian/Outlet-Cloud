using Outlet.Identity.Application.Ports;
using Outlet.Identity.Domain.AccessTokens;

namespace Outlet.Identity.Application.UnitTests.Fakes;

public sealed class FakeTokenSecretFactory(string secret = "outlet_pat_testsecret") : ITokenSecretFactory
{
    public GeneratedTokenSecret Create() =>
        new(PersonalAccessTokenId.From(Guid.NewGuid()), secret, TokenHash.From("abcdef1234567890"));
}

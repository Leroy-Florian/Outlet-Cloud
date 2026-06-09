using System.Security.Cryptography;
using System.Text;
using Outlet.Identity.Infrastructure.Security;

namespace Outlet.Identity.Infrastructure.UnitTests;

public sealed class Sha256TokenSecretFactoryTests
{
    [Fact]
    public void Should_ProducePrefixedSecret_WithMatchingHash()
    {
        var generated = new Sha256TokenSecretFactory().Create();

        generated.Secret.Should().StartWith("outlet_pat_");

        var expected = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(generated.Secret)));
        generated.Hash.Value.Should().Be(expected);
    }

    [Fact]
    public void Should_ProduceUniqueSecrets_OnEachCall()
    {
        var factory = new Sha256TokenSecretFactory();

        factory.Create().Secret.Should().NotBe(factory.Create().Secret);
    }
}

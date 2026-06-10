using Outlet.Crm.Domain.Organizations;
using Outlet.Crm.Domain.Payments;
using Outlet.Crm.Domain.Products;
using Outlet.Crm.Domain.Prospects;

namespace Outlet.Crm.Domain.UnitTests;

public sealed class ErrorMessageAndIdentityTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Should_EmbedId_When_FormattingNotFoundErrors()
    {
        var prospectId = ProspectId.New();
        var organizationId = OrganizationId.New();
        var paymentId = Guid.NewGuid();
        var productId = ProductId.New();

        ProspectErrors.NotFound(prospectId).Should().Be(
            $"Prospect.NotFound: Prospect '{prospectId.Value}' was not found.");
        OrganizationErrors.NotFound(organizationId).Should().Be(
            $"Organization.NotFound: Organization '{organizationId.Value}' was not found.");
        PaymentErrors.NotFound(paymentId).Should().Be(
            $"Payment.NotFound: Payment '{paymentId}' was not found.");
        ProductErrors.NotFound(productId).Should().Be(
            $"Product.NotFound: Product '{productId.Value}' was not found.");
    }

    [Fact]
    public void Should_GenerateDistinctIds_When_CallingNew()
    {
        ProspectId.New().Should().NotBe(ProspectId.New());
        OrganizationId.New().Should().NotBe(OrganizationId.New());
    }

    [Fact]
    public void Should_ExposeCreationState_When_ProspectIsCreated()
    {
        var productId = ProductId.New();
        var organizationId = OrganizationId.New();

        var prospect = Prospect.Create(
            productId, organizationId, "Ada", Email.Create("ada@example.com").Value!, "Acme", Now).Value!;

        prospect.ProductId.Should().Be(productId);
        prospect.OrganizationId.Should().Be(organizationId);
        prospect.Name.Should().Be("Ada");
        prospect.Email.Value.Should().Be("ada@example.com");
        prospect.Company.Should().Be("Acme");
        prospect.CreatedAt.Should().Be(Now);
        prospect.Stage.Should().Be(ProspectStage.New);
        prospect.Interactions.Should().BeEmpty();
    }

    [Fact]
    public void Should_ExposeCreationState_When_PaymentIsCreated()
    {
        var productId = ProductId.New();
        var organizationId = OrganizationId.New();

        var payment = Payment.Create(
            productId, organizationId, Money.Create(49.99m, "EUR").Value!, "stripe", "pi_123", Now).Value!;

        payment.ProductId.Should().Be(productId);
        payment.OrganizationId.Should().Be(organizationId);
        payment.Amount.Amount.Should().Be(49.99m);
        payment.Source.Should().Be("stripe");
        payment.ExternalReference.Should().Be("pi_123");
        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public void Should_ExposeCreationState_When_OrganizationIsCreated()
    {
        var organization = Organization.Create("Acme", "https://acme.example", Now).Value!;

        organization.Name.Should().Be("Acme");
        organization.Website.Should().Be("https://acme.example");
        organization.CreatedAt.Should().Be(Now);
    }
}

using Outlet.Crm.Domain.Invoices;
using Outlet.Crm.Domain.Payments;
using Outlet.Crm.Domain.Prospects;

namespace Outlet.Crm.Domain.UnitTests.Invoices;

public sealed class InvoiceTests
{
    private static readonly DateTime SomeInstant = new(2026, 6, 10, 8, 0, 0, DateTimeKind.Utc);

    private static Money Eur(decimal amount) => Money.Create(amount, "EUR").Value!;

    private static Invoice SomeInvoice() =>
        Invoice.Create(
            "INV-2026-0001",
            "Acme Corp",
            Email.Create("billing@acme.test").Value,
            "1 rue de la Paix, Paris",
            [new InvoiceLineDraft("Outlet Pro licence", 2m, Eur(49.5m)), new InvoiceLineDraft("Support", 1m, Eur(100m))],
            SomeInstant).Value!;

    [Fact]
    public void Should_CreateDraftWithComputedTotal_When_LinesAreValid()
    {
        var invoice = SomeInvoice();

        invoice.Status.Should().Be(InvoiceStatus.Draft);
        invoice.InvoiceNumber.Should().Be("INV-2026-0001");
        invoice.CustomerName.Should().Be("Acme Corp");
        invoice.CustomerEmail!.Value.Should().Be("billing@acme.test");
        invoice.CustomerAddress.Should().Be("1 rue de la Paix, Paris");
        invoice.Currency.Should().Be("EUR");
        invoice.Total.Should().Be(199m);
        invoice.Lines.Should().HaveCount(2);
        invoice.Lines[0].LineTotal.Should().Be(99m);
        invoice.CreatedAt.Should().Be(SomeInstant);
        invoice.IssuedAt.Should().BeNull();
        invoice.PaidAt.Should().BeNull();
        invoice.PaymentId.Should().BeNull();
    }

    [Fact]
    public void Should_AllowMissingEmailAndAddress_When_Creating()
    {
        var invoice = Invoice.Create(
            "INV-2026-0002", "Solo Dev", null, "   ", [new InvoiceLineDraft("Licence", 1m, Eur(10m))], SomeInstant).Value!;

        invoice.CustomerEmail.Should().BeNull();
        invoice.CustomerAddress.Should().BeNull();
    }

    [Fact]
    public void Should_Fail_When_InvoiceNumberIsBlank()
    {
        var result = Invoice.Create(" ", "Acme", null, null, [new InvoiceLineDraft("x", 1m, Eur(1m))], SomeInstant);

        result.Error.Should().Be(InvoiceErrors.NumberRequired);
    }

    [Fact]
    public void Should_Fail_When_CustomerNameIsBlank()
    {
        var result = Invoice.Create("INV-2026-0001", "  ", null, null, [new InvoiceLineDraft("x", 1m, Eur(1m))], SomeInstant);

        result.Error.Should().Be(InvoiceErrors.CustomerNameRequired);
    }

    [Fact]
    public void Should_Fail_When_NoLineIsProvided()
    {
        var result = Invoice.Create("INV-2026-0001", "Acme", null, null, [], SomeInstant);

        result.Error.Should().Be(InvoiceErrors.LinesRequired);
    }

    [Fact]
    public void Should_Fail_When_ALineHasNoDescription()
    {
        var result = Invoice.Create(
            "INV-2026-0001", "Acme", null, null, [new InvoiceLineDraft(" ", 1m, Eur(1m))], SomeInstant);

        result.Error.Should().Be(InvoiceErrors.LineDescriptionRequired);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Should_Fail_When_ALineQuantityIsNotPositive(int quantity)
    {
        var result = Invoice.Create(
            "INV-2026-0001", "Acme", null, null, [new InvoiceLineDraft("x", quantity, Eur(1m))], SomeInstant);

        result.Error.Should().Be(InvoiceErrors.LineQuantityNotPositive);
    }

    [Fact]
    public void Should_Fail_When_LinesMixCurrencies()
    {
        var result = Invoice.Create(
            "INV-2026-0001",
            "Acme",
            null,
            null,
            [new InvoiceLineDraft("a", 1m, Eur(1m)), new InvoiceLineDraft("b", 1m, Money.Create(1m, "USD").Value!)],
            SomeInstant);

        result.Error.Should().Be(InvoiceErrors.MixedCurrencies);
    }

    [Fact]
    public void Should_StampIssuedAt_When_IssuingADraft()
    {
        var invoice = SomeInvoice();
        var issuedAt = SomeInstant.AddDays(1);

        var result = invoice.Issue(issuedAt);

        result.IsSuccess.Should().BeTrue();
        invoice.Status.Should().Be(InvoiceStatus.Issued);
        invoice.IssuedAt.Should().Be(issuedAt);
    }

    [Fact]
    public void Should_RejectIssue_When_InvoiceIsNotDraft()
    {
        var invoice = SomeInvoice();
        invoice.Issue(SomeInstant);

        var result = invoice.Issue(SomeInstant);

        result.Error.Should().Be(InvoiceErrors.InvalidTransition(InvoiceStatus.Issued, InvoiceStatus.Issued));
    }

    [Fact]
    public void Should_StampPaidAtAndLinkPayment_When_PayingAnIssuedInvoice()
    {
        var invoice = SomeInvoice();
        invoice.Issue(SomeInstant);
        var paymentId = Guid.NewGuid();
        var paidAt = SomeInstant.AddDays(3);

        var result = invoice.MarkPaid(paidAt, paymentId);

        result.IsSuccess.Should().BeTrue();
        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.PaidAt.Should().Be(paidAt);
        invoice.PaymentId.Should().Be(paymentId);
    }

    [Fact]
    public void Should_RejectPayment_When_InvoiceIsStillDraft()
    {
        var invoice = SomeInvoice();

        var result = invoice.MarkPaid(SomeInstant, null);

        result.Error.Should().Be(InvoiceErrors.InvalidTransition(InvoiceStatus.Draft, InvoiceStatus.Paid));
        invoice.PaidAt.Should().BeNull();
    }

    [Fact]
    public void Should_Cancel_When_InvoiceIsDraftOrIssued()
    {
        var draft = SomeInvoice();
        draft.Cancel().IsSuccess.Should().BeTrue();
        draft.Status.Should().Be(InvoiceStatus.Cancelled);

        var issued = SomeInvoice();
        issued.Issue(SomeInstant);
        issued.Cancel().IsSuccess.Should().BeTrue();
        issued.Status.Should().Be(InvoiceStatus.Cancelled);
    }

    [Fact]
    public void Should_RejectCancel_When_InvoiceIsPaid()
    {
        var invoice = SomeInvoice();
        invoice.Issue(SomeInstant);
        invoice.MarkPaid(SomeInstant, null);

        var result = invoice.Cancel();

        result.Error.Should().Be(InvoiceErrors.InvalidTransition(InvoiceStatus.Paid, InvoiceStatus.Cancelled));
    }

    [Fact]
    public void Should_RejectCancel_When_InvoiceIsAlreadyCancelled()
    {
        var invoice = SomeInvoice();
        invoice.Cancel();

        var result = invoice.Cancel();

        result.Error.Should().Be(InvoiceErrors.InvalidTransition(InvoiceStatus.Cancelled, InvoiceStatus.Cancelled));
    }
}

using Outlet.Crm.Application.Invoices;
using Outlet.Crm.Application.UnitTests.Fakes;
using Outlet.Crm.Domain.Invoices;

namespace Outlet.Crm.Application.UnitTests.Invoices;

public sealed class InvoiceUseCaseTests
{
    private static readonly DateTime Now = new(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeInvoiceRepository _invoices = new();

    private CreateInvoiceUseCase CreateUseCase => new(_invoices, new FixedClock(Now));

    private IssueInvoiceUseCase IssueUseCase => new(_invoices, new FixedClock(Now));

    private MarkInvoicePaidUseCase PayUseCase => new(_invoices, new FixedClock(Now));

    private CancelInvoiceUseCase CancelUseCase => new(_invoices);

    private GetInvoicesUseCase GetUseCase => new(_invoices);

    private static CreateInvoiceCommand SomeCommand(string customer = "Acme Corp") =>
        new(customer, "billing@acme.test", "1 rue de la Paix", [new InvoiceLineRequest("Licence", 2m, 49.5m, "EUR")]);

    private async Task<Invoice> CreateInvoiceAsync()
    {
        var created = await CreateUseCase.HandleAsync(SomeCommand(), CancellationToken.None);
        return _invoices.Items.Single(i => i.Id.Value == created.Value!.Id);
    }

    [Fact]
    public async Task Should_CreateDraftWithSequentialNumber_When_CommandIsValid()
    {
        var result = await CreateUseCase.HandleAsync(SomeCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.InvoiceNumber.Should().Be("INV-2026-0001");
        var invoice = _invoices.Items.Should().ContainSingle().Subject;
        invoice.Status.Should().Be(InvoiceStatus.Draft);
        invoice.Total.Should().Be(99m);
        invoice.Currency.Should().Be("EUR");
        invoice.CustomerEmail!.Value.Should().Be("billing@acme.test");
        invoice.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public async Task Should_IncrementSequence_When_InvoicesExistForTheYear()
    {
        await CreateUseCase.HandleAsync(SomeCommand(), CancellationToken.None);

        var second = await CreateUseCase.HandleAsync(SomeCommand("Beta Ltd"), CancellationToken.None);

        second.Value!.InvoiceNumber.Should().Be("INV-2026-0002");
    }

    [Fact]
    public async Task Should_AllowMissingEmail_When_CreatingAnInvoice()
    {
        var result = await CreateUseCase.HandleAsync(
            new CreateInvoiceCommand("Acme", null, null, [new InvoiceLineRequest("Licence", 1m, 10m, "EUR")]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _invoices.Items.Should().ContainSingle().Which.CustomerEmail.Should().BeNull();
    }

    [Fact]
    public async Task Should_Fail_When_CustomerEmailIsInvalid()
    {
        var result = await CreateUseCase.HandleAsync(
            new CreateInvoiceCommand("Acme", "not-an-email", null, [new InvoiceLineRequest("Licence", 1m, 10m, "EUR")]),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Email.Invalid:");
        _invoices.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Fail_When_ALineHasAnInvalidCurrency()
    {
        var result = await CreateUseCase.HandleAsync(
            new CreateInvoiceCommand("Acme", null, null, [new InvoiceLineRequest("Licence", 1m, 10m, "EURO")]),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Money.InvalidCurrency:");
    }

    [Fact]
    public async Task Should_Fail_When_NoLineIsProvided()
    {
        var result = await CreateUseCase.HandleAsync(
            new CreateInvoiceCommand("Acme", null, null, []), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InvoiceErrors.LinesRequired);
        _invoices.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_IssueInvoice_When_ItIsDraft()
    {
        var invoice = await CreateInvoiceAsync();

        var result = await IssueUseCase.HandleAsync(new IssueInvoiceCommand(invoice.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        invoice.Status.Should().Be(InvoiceStatus.Issued);
        invoice.IssuedAt.Should().Be(Now);
        _invoices.UpdateCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_When_IssuingUnknownInvoice()
    {
        var result = await IssueUseCase.HandleAsync(new IssueInvoiceCommand(InvoiceId.New()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Invoice.NotFound:");
    }

    [Fact]
    public async Task Should_NotPersist_When_TransitionIsInvalid()
    {
        var invoice = await CreateInvoiceAsync();

        var result = await PayUseCase.HandleAsync(new MarkInvoicePaidCommand(invoice.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Invoice.InvalidTransition:");
        _invoices.UpdateCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_MarkPaidWithPaymentReference_When_InvoiceIsIssued()
    {
        var invoice = await CreateInvoiceAsync();
        await IssueUseCase.HandleAsync(new IssueInvoiceCommand(invoice.Id), CancellationToken.None);
        var paymentId = Guid.NewGuid();

        var result = await PayUseCase.HandleAsync(new MarkInvoicePaidCommand(invoice.Id, paymentId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.PaidAt.Should().Be(Now);
        invoice.PaymentId.Should().Be(paymentId);
        _invoices.UpdateCount.Should().Be(2);
    }

    [Fact]
    public async Task Should_NotPersist_When_IssuingAnAlreadyIssuedInvoice()
    {
        var invoice = await CreateInvoiceAsync();
        await IssueUseCase.HandleAsync(new IssueInvoiceCommand(invoice.Id), CancellationToken.None);

        var result = await IssueUseCase.HandleAsync(new IssueInvoiceCommand(invoice.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Invoice.InvalidTransition:");
        _invoices.UpdateCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_NotPersist_When_CancellingAPaidInvoice()
    {
        var invoice = await CreateInvoiceAsync();
        await IssueUseCase.HandleAsync(new IssueInvoiceCommand(invoice.Id), CancellationToken.None);
        await PayUseCase.HandleAsync(new MarkInvoicePaidCommand(invoice.Id), CancellationToken.None);

        var result = await CancelUseCase.HandleAsync(new CancelInvoiceCommand(invoice.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Invoice.InvalidTransition:");
        _invoices.UpdateCount.Should().Be(2);
    }

    [Fact]
    public async Task Should_Fail_When_PayingUnknownInvoice()
    {
        var result = await PayUseCase.HandleAsync(new MarkInvoicePaidCommand(InvoiceId.New()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Invoice.NotFound:");
    }

    [Fact]
    public async Task Should_CancelInvoice_When_ItIsNotPaid()
    {
        var invoice = await CreateInvoiceAsync();

        var result = await CancelUseCase.HandleAsync(new CancelInvoiceCommand(invoice.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        invoice.Status.Should().Be(InvoiceStatus.Cancelled);
        _invoices.UpdateCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_When_CancellingUnknownInvoice()
    {
        var result = await CancelUseCase.HandleAsync(new CancelInvoiceCommand(InvoiceId.New()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Invoice.NotFound:");
    }

    [Fact]
    public async Task Should_FilterByStatus_When_ListingInvoices()
    {
        var first = await CreateInvoiceAsync();
        await CreateUseCase.HandleAsync(SomeCommand("Beta Ltd"), CancellationToken.None);
        await IssueUseCase.HandleAsync(new IssueInvoiceCommand(first.Id), CancellationToken.None);

        var issuedOnly = await GetUseCase.HandleAsync(new GetInvoicesQuery(InvoiceStatus.Issued), CancellationToken.None);
        var all = await GetUseCase.HandleAsync(new GetInvoicesQuery(), CancellationToken.None);

        issuedOnly.Value!.Should().ContainSingle().Which.Id.Should().Be(first.Id);
        all.Value!.Should().HaveCount(2);
    }
}

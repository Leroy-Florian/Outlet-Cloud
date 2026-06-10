namespace Outlet.Crm.Domain.Invoices;

public static class InvoiceErrors
{
    public const string NumberRequired =
        "Invoice.NumberRequired: An invoice requires a sequential number.";

    public const string CustomerNameRequired =
        "Invoice.CustomerNameRequired: An invoice requires a customer name.";

    public const string LinesRequired =
        "Invoice.LinesRequired: An invoice requires at least one line.";

    public const string LineDescriptionRequired =
        "Invoice.LineDescriptionRequired: Every invoice line requires a description.";

    public const string LineQuantityNotPositive =
        "Invoice.LineQuantityNotPositive: Every invoice line quantity must be strictly positive.";

    public const string MixedCurrencies =
        "Invoice.MixedCurrencies: All lines of an invoice must share the same currency.";

    public static string NotFound(InvoiceId id) =>
        $"Invoice.NotFound: Invoice '{id.Value}' was not found.";

    public static string InvalidTransition(InvoiceStatus from, InvoiceStatus to) =>
        $"Invoice.InvalidTransition: Cannot move an invoice from {from} to {to}.";
}

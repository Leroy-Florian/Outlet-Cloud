namespace Outlet.Crm.Domain.Payments;

public static class PaymentErrors
{
    public const string SourceRequired =
        "Payment.SourceRequired: A payment requires a source (provider) identifier.";

    public const string NotPending =
        "Payment.NotPending: Only a pending payment can be settled or failed.";

    public const string NotSettled =
        "Payment.NotSettled: Only a settled payment can be refunded.";

    public const string ExternalReferenceRequired =
        "Payment.ExternalReferenceRequired: A billing event requires an external reference.";

    public static string UnknownBillingStatus(string status) =>
        $"Payment.UnknownBillingStatus: Billing status '{status}' is not supported (expected paid, pending or refunded).";

    public static string NotFoundByReference(string externalReference) =>
        $"Payment.NotFound: No payment with external reference '{externalReference}' was found.";

    public static string NotFound(Guid id) =>
        $"Payment.NotFound: Payment '{id}' was not found.";
}

namespace Outlet.Crm.Domain.Payments;

public enum PaymentStatus
{
    Pending = 0,
    Settled = 1,
    Failed = 2,
    Refunded = 3,
}

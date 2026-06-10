using Outlet.Kernel.Shared;

namespace Outlet.Crm.Domain.Payments;

public sealed record Money
{
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Result<Money> Create(decimal amount, string currency)
    {
        if (amount < 0)
        {
            return Result.Failure<Money>("Money.Negative: An amount cannot be negative.");
        }

        if (currency.Trim().Length is not 3)
        {
            return Result.Failure<Money>("Money.InvalidCurrency: A currency must be a 3-letter ISO code.");
        }

        return Result.Success(new Money(amount, currency.Trim().ToUpperInvariant()));
    }
}

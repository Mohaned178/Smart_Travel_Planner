namespace SmartTravelPlanner.Domain.ValueObjects;

public sealed record Money(decimal Amount, string CurrencyCode)
{
    public Money Add(Money other)
    {
        if (CurrencyCode != other.CurrencyCode)
            throw new InvalidOperationException($"Cannot add Money with different currencies: {CurrencyCode} and {other.CurrencyCode}");

        return this with { Amount = Amount + other.Amount };
    }

    public Money Subtract(Money other)
    {
        if (CurrencyCode != other.CurrencyCode)
            throw new InvalidOperationException($"Cannot subtract Money with different currencies: {CurrencyCode} and {other.CurrencyCode}");

        return this with { Amount = Amount - other.Amount };
    }

    public static Money Zero(string currencyCode) => new(0m, currencyCode);
}

using PolicyPlatform.Domain.Common;

namespace PolicyPlatform.Domain.Policies;

/// <summary>Reference-type record (not a record struct): EF Core's OwnedNavigationBuilder.OwnsOne
/// requires a reference type for nested owned value objects (record structs are rejected
/// with CS0452). Value equality and immutability are unaffected — records give both
/// regardless of struct/class.</summary>
public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new DomainException("Money amount cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
        {
            throw new DomainException("Currency must be a 3-letter ISO code (e.g. PLN, EUR).");
        }

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public static Money Zero(string currency) => new(0m, currency);

    public Money Add(Money other)
    {
        if (other.Currency != Currency)
        {
            throw new DomainException($"Cannot add {other.Currency} to {Currency}.");
        }

        return new Money(Amount + other.Amount, Currency);
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";
}

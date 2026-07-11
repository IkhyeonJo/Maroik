using ErrorOr;
using Maroik.Core.Contract.Domains.Primitives;

namespace Maroik.Core.Contract.Domains.ValueObjects;

/// <summary>
/// Value object representing a monetary amount with an explicit currency code (e.g. "KRW", "USD").
/// Arithmetic operations enforce currency homogeneity.
/// </summary>
public sealed class Money : ValueObject
{
    /// <summary>Numeric amount (can be negative for debits).</summary>
    public decimal Amount { get; }

    /// <summary>ISO 4217 currency code in upper-case (e.g. "KRW", "USD").</summary>
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// Re-creates a <see cref="Money"/> from values already stored in the database.
    /// Skips validation — only call this when the source is trusted (repository layer).
    /// </summary>
    internal static Money FromTrustedSource(decimal amount, string currency) =>
        new(amount, currency.ToUpperInvariant());

    /// <summary>
    /// Creates a <see cref="Money"/> value object.
    /// Returns a validation error if <paramref name="currency"/> is null or empty.
    /// </summary>
    public static ErrorOr<Money> Create(decimal amount, string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            return Error.Validation("Money.CurrencyEmpty", "Currency code cannot be empty.");

        return new Money(amount, currency.ToUpperInvariant());
    }

    /// <summary>Creates a zero-balance <see cref="Money"/> with the given currency.</summary>
    public static Money Zero(string currency) => new(0m, currency.ToUpperInvariant());

    /// <summary>Returns a new <see cref="Money"/> whose amount is the sum of both operands.</summary>
    /// <exception cref="InvalidOperationException">Thrown when currencies differ.</exception>
    public Money Add(Money other)
    {
        if (!Currency.Equals(other.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Cannot add {Currency} and {other.Currency}.");

        return new Money(Amount + other.Amount, Currency);
    }

    /// <summary>Returns a new <see cref="Money"/> whose amount equals this minus <paramref name="other"/>.</summary>
    /// <exception cref="InvalidOperationException">Thrown when currencies differ.</exception>
    public Money Subtract(Money other)
    {
        if (!Currency.Equals(other.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Cannot subtract {other.Currency} from {Currency}.");

        return new Money(Amount - other.Amount, Currency);
    }

    /// <inheritdoc/>
    protected override IEnumerable<object?> GetAtomicValues()
    {
        yield return Amount;
        yield return Currency;
    }

    /// <inheritdoc/>
    public override string ToString() => $"{Amount} {Currency}";
}

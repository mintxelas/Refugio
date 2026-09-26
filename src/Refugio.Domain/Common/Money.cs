namespace Refugio.Domain.Common;

/// <summary>
/// A non-negative monetary amount. Guards the "amount can't be negative" invariant shared by
/// donations, expenses and fundraising goals. Converts implicitly to/from <c>decimal</c> so
/// entities keep storing a plain decimal column — this only centralizes the validation.
/// </summary>
public readonly record struct Money
{
    public decimal Value { get; }

    public Money(decimal value)
    {
        if (value < 0)
            throw new ArgumentException("Amount cannot be negative.", nameof(value));
        Value = value;
    }

    public static implicit operator decimal(Money money) => money.Value;
    public static implicit operator Money(decimal value) => new(value);
}

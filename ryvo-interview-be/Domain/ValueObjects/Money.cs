using ryvo_interview_be.Domain.Exceptions;

namespace ryvo_interview_be.Domain;

/// <summary>
/// A USD amount rounded to cents. USD is the only currency the domain supports.
/// </summary>
public readonly record struct Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency = "USD")
    {
        if (amount < 0)
            throw new DomainException("Money cannot be negative.");
        if (!string.Equals(currency, "USD", StringComparison.OrdinalIgnoreCase))
            throw new DomainException("Only USD is supported.");

        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = "USD";
    }

    public static Money Zero => new(0m);

    public static Money FromHoursAndRate(decimal hours, Money hourlyRate)
    {
        if (hours < 0)
            throw new DomainException("Hours cannot be negative.");

        return new Money(hours * hourlyRate.Amount, hourlyRate.Currency);
    }

    public static Money operator +(Money left, Money right) =>
        new(left.Amount + right.Amount);
}

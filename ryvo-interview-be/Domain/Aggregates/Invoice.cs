namespace ryvo_interview_be.Domain;

/// <summary>
/// One billable day for one contractor, dated in the client's local time zone.
/// </summary>
public sealed record InvoiceLineItem(
    Guid ContractorId,
    string ContractorName,
    DateOnly WorkDateClientLocal,
    decimal ApprovedHours,
    Money HourlyRate,
    Money LineTotal);

/// <summary>
/// An immutable invoice for one client over a date range. Totals are derived from
/// the line items, so an invoice can never disagree with its own lines.
/// </summary>
public sealed class Invoice
{
    public Guid Id { get; }
    public Guid ClientId { get; }
    public DateOnly RangeStart { get; }
    public DateOnly RangeEnd { get; }
    public DateTimeOffset GeneratedAtUtc { get; }
    public IReadOnlyList<InvoiceLineItem> LineItems { get; }
    public decimal TotalApprovedHours { get; }
    public Money TotalAmount { get; }

    internal Invoice(
        Guid id,
        Guid clientId,
        DateOnly rangeStart,
        DateOnly rangeEnd,
        DateTimeOffset generatedAtUtc,
        IReadOnlyList<InvoiceLineItem> lineItems)
    {
        Id = id;
        ClientId = clientId;
        RangeStart = rangeStart;
        RangeEnd = rangeEnd;
        GeneratedAtUtc = generatedAtUtc;
        LineItems = lineItems;
        TotalApprovedHours = lineItems.Sum(li => li.ApprovedHours);
        TotalAmount = lineItems.Aggregate(Money.Zero, (acc, li) => acc + li.LineTotal);
    }
}

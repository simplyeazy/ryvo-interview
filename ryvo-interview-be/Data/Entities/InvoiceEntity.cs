namespace ryvo_interview_be.Data.Entities;

public sealed class InvoiceEntity
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public DateOnly RangeStart { get; set; }
    public DateOnly RangeEnd { get; set; }
    public DateTimeOffset GeneratedAtUtc { get; set; }
    public decimal TotalApprovedHours { get; set; }
    public decimal TotalAmountUsd { get; set; }
    public List<InvoiceLineEntity> LineItems { get; set; } = [];
}

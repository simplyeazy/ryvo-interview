namespace ryvo_interview_be.Data.Entities;

public sealed class InvoiceLineEntity
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid ContractorId { get; set; }
    public string ContractorName { get; set; } = string.Empty;
    public DateOnly WorkDateClientLocal { get; set; }
    public decimal ApprovedHours { get; set; }
    public decimal HourlyRateUsd { get; set; }
    public decimal LineTotalUsd { get; set; }
    public InvoiceEntity? Invoice { get; set; }
}

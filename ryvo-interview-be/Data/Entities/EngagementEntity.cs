namespace ryvo_interview_be.Data.Entities;

public sealed class EngagementEntity
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public Guid ContractorId { get; set; }
    public decimal HourlyRateUsd { get; set; }
}

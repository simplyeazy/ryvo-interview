using ryvo_interview_be.Domain;

namespace ryvo_interview_be.Data.Entities;

public sealed class TimesheetWeekEntity
{
    public Guid Id { get; set; }
    public Guid EngagementId { get; set; }
    public Guid ContractorId { get; set; }
    public Guid ClientId { get; set; }
    public string ContractorTimeZoneId { get; set; } = string.Empty;
    public DateOnly WeekStartLocal { get; set; }
    public TimesheetStatus Status { get; set; }
    public string? RejectionComment { get; set; }
    public List<TimeEntryEntity> Entries { get; set; } = [];
}

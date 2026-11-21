namespace ryvo_interview_be.Data.Entities;

public sealed class TimeEntryEntity
{
    public Guid Id { get; set; }
    public Guid WeekId { get; set; }
    public DateTimeOffset StartUtc { get; set; }
    public DateTimeOffset EndUtc { get; set; }
    public string Description { get; set; } = string.Empty;
    public TimesheetWeekEntity? Week { get; set; }
}

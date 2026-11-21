namespace ryvo_interview_be.Domain;

/// <summary>
/// A block of worked time belonging to a <see cref="TimesheetWeek"/>. Created and
/// changed only through the week, which enforces the boundary, overlap and daily
/// cap rules.
/// </summary>
public sealed class TimeEntry
{
    public Guid Id { get; }
    public TimeRange Period { get; private set; }
    public string Description { get; private set; }

    internal TimeEntry(Guid id, TimeRange period, string? description)
    {
        Id = id;
        Period = period;
        Description = description?.Trim() ?? string.Empty;
    }

    internal void Update(TimeRange newPeriod, string? description)
    {
        Period = newPeriod;
        Description = description?.Trim() ?? string.Empty;
    }
}

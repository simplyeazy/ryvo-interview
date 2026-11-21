namespace ryvo_interview_be.Domain;

/// <summary>
/// A Monday-to-Sunday week in the contractor's time zone, plus the equivalent UTC
/// interval. Entries are only accepted inside it.
/// </summary>
public sealed class WeekBoundary
{
    public DateOnly WeekStartLocal { get; }
    public DateOnly WeekEndLocal => WeekStartLocal.AddDays(6);
    public DateTimeOffset StartUtc { get; }
    public DateTimeOffset EndUtcExclusive { get; }

    public WeekBoundary(DateOnly anyDateInWeek, TimeZoneInfo contractorTimeZone)
    {
        // Anchor to the Monday of the week containing anyDateInWeek.
        var diffFromMonday = ((int)anyDateInWeek.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        WeekStartLocal = anyDateInWeek.AddDays(-diffFromMonday);

        var startLocal = WeekStartLocal.ToDateTime(TimeOnly.MinValue);
        var endLocalExclusive = WeekStartLocal.AddDays(7).ToDateTime(TimeOnly.MinValue);

        StartUtc = new DateTimeOffset(startLocal, contractorTimeZone.GetUtcOffset(startLocal))
            .ToUniversalTime();
        EndUtcExclusive = new DateTimeOffset(endLocalExclusive, contractorTimeZone.GetUtcOffset(endLocalExclusive))
            .ToUniversalTime();
    }

    public bool Contains(TimeRange range) =>
        range.StartUtc >= StartUtc && range.EndUtc <= EndUtcExclusive;
}

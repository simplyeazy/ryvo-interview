using ryvo_interview_be.Domain.Exceptions;

namespace ryvo_interview_be.Domain;

/// <summary>
/// A half-open UTC interval, <c>[StartUtc, EndUtc)</c>. Both ends must already be
/// UTC so the same instant is never stored twice under different offsets.
/// </summary>
public sealed record TimeRange
{
    public DateTimeOffset StartUtc { get; }
    public DateTimeOffset EndUtc { get; }

    public TimeRange(DateTimeOffset startUtc, DateTimeOffset endUtc)
    {
        if (startUtc.Offset != TimeSpan.Zero || endUtc.Offset != TimeSpan.Zero)
            throw new DomainException("Timestamp must be stored in UTC.");
        if (endUtc <= startUtc)
            throw new DomainException("End time must be strictly after start time.");

        StartUtc = startUtc;
        EndUtc = endUtc;
    }

    public long DurationTicks => (EndUtc - StartUtc).Ticks;

    public decimal TotalHours => (decimal)DurationTicks / TimeSpan.TicksPerHour;

    public bool OverlapsWith(TimeRange other) => StartUtc < other.EndUtc && EndUtc > other.StartUtc;

    /// <summary>
    /// Splits a UTC range into ticks per local calendar date in the specified time zone.
    /// Ticks are accumulated as integers so the 12-hour daily cap cannot drift.
    /// </summary>
    public IReadOnlyDictionary<DateOnly, long> GetTicksPerLocalDate(TimeZoneInfo timeZone)
    {
        var breakdown = new Dictionary<DateOnly, long>();
        var cursorUtc = StartUtc;

        while (cursorUtc < EndUtc)
        {
            var localCursor = TimeZoneInfo.ConvertTime(cursorUtc, timeZone);
            var localDate = DateOnly.FromDateTime(localCursor.DateTime);

            var nextMidnightLocal = localDate.AddDays(1).ToDateTime(TimeOnly.MinValue);
            var nextMidnightOffset = timeZone.GetUtcOffset(nextMidnightLocal);
            var nextMidnightUtc = new DateTimeOffset(nextMidnightLocal, nextMidnightOffset)
                .ToUniversalTime();

            // Guard against non-progress on pathological time-zone data.
            if (nextMidnightUtc <= cursorUtc)
                nextMidnightUtc = EndUtc;

            var segmentEndUtc = EndUtc < nextMidnightUtc ? EndUtc : nextMidnightUtc;
            var segmentTicks = (segmentEndUtc - cursorUtc).Ticks;

            breakdown[localDate] = breakdown.GetValueOrDefault(localDate) + segmentTicks;
            cursorUtc = segmentEndUtc;
        }

        return breakdown;
    }

    /// <summary>
    /// Hours per local calendar date, rounded to 4 decimal places for display.
    /// Use <see cref="GetTicksPerLocalDate"/> for validation and billing.
    /// </summary>
    public IReadOnlyDictionary<DateOnly, decimal> GetHoursPerLocalDate(TimeZoneInfo timeZone) =>
        GetTicksPerLocalDate(timeZone).ToDictionary(
            kvp => kvp.Key,
            kvp => decimal.Round(
                (decimal)kvp.Value / TimeSpan.TicksPerHour, 4, MidpointRounding.AwayFromZero));
}

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ryvo_interview_be.Data;

/// <summary>
/// SQLite has no native DateTimeOffset type and cannot order by the TEXT form
/// it emits. Store UTC ticks instead: exact, sortable and comparable in SQL.
/// </summary>
public sealed class DateTimeOffsetToUtcTicksConverter : ValueConverter<DateTimeOffset, long>
{
    public DateTimeOffsetToUtcTicksConverter()
        : base(
            value => value.UtcTicks,
            ticks => new DateTimeOffset(ticks, TimeSpan.Zero))
    {
    }
}

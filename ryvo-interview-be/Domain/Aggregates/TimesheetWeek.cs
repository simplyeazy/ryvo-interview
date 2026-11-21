using ryvo_interview_be.Domain.Exceptions;

namespace ryvo_interview_be.Domain;

/// <summary>
/// A contractor's week in their own time zone. This is the aggregate root for
/// time entries: every edit goes through it so the boundary, overlap and
/// 12-hour daily cap rules are enforced in one place.
/// </summary>
public sealed class TimesheetWeek
{
    public const decimal MaxDailyHours = 12.0m;
    private static readonly long MaxDailyTicks = (long)(MaxDailyHours * TimeSpan.TicksPerHour);

    private readonly List<TimeEntry> _entries = [];

    public Guid Id { get; }
    public Guid EngagementId { get; }
    public Guid ContractorId { get; }
    public Guid ClientId { get; }
    public string ContractorTimeZoneId { get; }
    public WeekBoundary Boundary { get; }
    public TimesheetStatus Status { get; private set; }
    public string? RejectionComment { get; private set; }
    public IReadOnlyCollection<TimeEntry> Entries => _entries.AsReadOnly();

    public decimal TotalHours =>
        _entries.Sum(e => (decimal)e.Period.DurationTicks) / TimeSpan.TicksPerHour;

    public TimesheetWeek(
        Guid id,
        Engagement engagement,
        User contractor,
        DateOnly anyDateInWeek)
    {
        if (contractor.Role != UserRole.Contractor || contractor.Id != engagement.ContractorId)
            throw new DomainException("Timesheet week must belong to the engagement's contractor.");

        Id = id;
        EngagementId = engagement.Id;
        ContractorId = engagement.ContractorId;
        ClientId = engagement.ClientId;
        ContractorTimeZoneId = contractor.TimeZoneId;
        Boundary = new WeekBoundary(anyDateInWeek, contractor.GetTimeZone());
        Status = TimesheetStatus.Draft;
    }

    /// <summary>
    /// Rebuilds a week from persisted state without re-running creation rules.
    /// Used only by the persistence layer.
    /// </summary>
    internal static TimesheetWeek Rehydrate(
        Guid id,
        Engagement engagement,
        User contractor,
        DateOnly weekStartLocal,
        TimesheetStatus status,
        string? rejectionComment,
        IEnumerable<TimeEntry> entries)
    {
        var week = new TimesheetWeek(id, engagement, contractor, weekStartLocal);
        week.Status = status;
        week.RejectionComment = rejectionComment;
        week._entries.AddRange(entries);
        return week;
    }

    public TimeEntry AddEntry(Guid actorContractorId, TimeRange period, string? description = null)
    {
        EnsureCanContractorEdit(actorContractorId);
        ValidateEntryRules(period, ignoreEntryId: null);

        var entry = new TimeEntry(Guid.NewGuid(), period, description);
        _entries.Add(entry);
        return entry;
    }

    public void UpdateEntry(
        Guid actorContractorId,
        Guid entryId,
        TimeRange newPeriod,
        string? description = null)
    {
        EnsureCanContractorEdit(actorContractorId);

        var existing = _entries.FirstOrDefault(e => e.Id == entryId)
            ?? throw new DomainException("Time entry not found in this week.");

        ValidateEntryRules(newPeriod, ignoreEntryId: entryId);
        existing.Update(newPeriod, description);
    }

    public void RemoveEntry(Guid actorContractorId, Guid entryId)
    {
        EnsureCanContractorEdit(actorContractorId);

        var existing = _entries.FirstOrDefault(e => e.Id == entryId)
            ?? throw new DomainException("Time entry not found in this week.");

        _entries.Remove(existing);
    }

    public void Submit(Guid actorContractorId)
    {
        EnsureCanContractorEdit(actorContractorId);
        if (_entries.Count == 0)
            throw new DomainException("Cannot submit an empty timesheet week.");

        Status = TimesheetStatus.Submitted;
        RejectionComment = null;
    }

    public void Approve(Guid actorClientId)
    {
        EnsureIsAssignedClient(actorClientId);
        if (Status != TimesheetStatus.Submitted)
            throw new DomainException($"Only submitted weeks can be approved. Current status: {Status}.");

        Status = TimesheetStatus.Approved;
        RejectionComment = null;
    }

    public void Reject(Guid actorClientId, string comment)
    {
        EnsureIsAssignedClient(actorClientId);
        if (Status != TimesheetStatus.Submitted)
            throw new DomainException($"Only submitted weeks can be rejected. Current status: {Status}.");
        if (string.IsNullOrWhiteSpace(comment))
            throw new DomainException("A rejection comment is required.");

        Status = TimesheetStatus.Rejected;
        RejectionComment = comment.Trim();
    }

    /// <summary>
    /// Hours per local calendar date, projected into the given time zone.
    /// Rounded to 4 decimal places only after the per-day tick totals are summed.
    /// </summary>
    public IReadOnlyDictionary<DateOnly, decimal> GetDailyTotalsInTimeZone(TimeZoneInfo targetTimeZone)
    {
        var ticksPerDate = new Dictionary<DateOnly, long>();

        foreach (var entry in _entries)
        {
            foreach (var (date, ticks) in entry.Period.GetTicksPerLocalDate(targetTimeZone))
                ticksPerDate[date] = ticksPerDate.GetValueOrDefault(date) + ticks;
        }

        return ticksPerDate.ToDictionary(
            kvp => kvp.Key,
            kvp => decimal.Round(
                (decimal)kvp.Value / TimeSpan.TicksPerHour, 4, MidpointRounding.AwayFromZero));
    }

    /// <summary>
    /// Daily totals in the contractor's local time zone, the zone that defines the week.
    /// </summary>
    public IReadOnlyDictionary<DateOnly, decimal> GetContractorDailyTotals() =>
        GetDailyTotalsInTimeZone(TimeZoneInfo.FindSystemTimeZoneById(ContractorTimeZoneId));

    private void EnsureIsAssignedClient(Guid actorClientId)
    {
        if (actorClientId != ClientId)
            throw new UnauthorizedDomainException("Clients can only review their own contractors' weeks.");
    }

    private void EnsureCanContractorEdit(Guid actorContractorId)
    {
        if (actorContractorId != ContractorId)
            throw new UnauthorizedDomainException("Only the assigned contractor can edit this timesheet.");
        if (Status is TimesheetStatus.Submitted or TimesheetStatus.Approved)
            throw new TimesheetLockedException($"Timesheet is locked while in '{Status}' status.");
    }

    private void ValidateEntryRules(TimeRange candidatePeriod, Guid? ignoreEntryId)
    {
        if (!Boundary.Contains(candidatePeriod))
            throw new DomainException("Time entry falls outside the boundaries of this timesheet week.");

        var activeEntries = _entries.Where(e => e.Id != ignoreEntryId).ToList();

        if (activeEntries.Any(e => e.Period.OverlapsWith(candidatePeriod)))
            throw new OverlappingTimeEntryException("Time entry overlaps with an existing entry.");

        // The 12-hour cap is per local calendar day in the contractor's zone.
        // Accumulate ticks (long) so repeated fractional hours cannot drift past it.
        var contractorTz = TimeZoneInfo.FindSystemTimeZoneById(ContractorTimeZoneId);
        var dailyTicks = new Dictionary<DateOnly, long>();

        foreach (var entry in activeEntries)
        {
            foreach (var (date, ticks) in entry.Period.GetTicksPerLocalDate(contractorTz))
                dailyTicks[date] = dailyTicks.GetValueOrDefault(date) + ticks;
        }

        foreach (var (date, addedTicks) in candidatePeriod.GetTicksPerLocalDate(contractorTz))
        {
            var projectedTicks = dailyTicks.GetValueOrDefault(date) + addedTicks;
            if (projectedTicks > MaxDailyTicks)
            {
                var projectedHours = (decimal)projectedTicks / TimeSpan.TicksPerHour;
                throw new DailyCapExceededException(
                    $"Daily limit of {MaxDailyHours} hours exceeded on {date:yyyy-MM-dd} ({projectedHours:0.##}h).");
            }
        }
    }
}

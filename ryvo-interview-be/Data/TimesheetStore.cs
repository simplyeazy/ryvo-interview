using Microsoft.EntityFrameworkCore;
using ryvo_interview_be.Data.Entities;
using ryvo_interview_be.Domain;
using ryvo_interview_be.Domain.Exceptions;

namespace ryvo_interview_be.Data;

/// <summary>
/// SQLite-backed store. Maps the persistence entities to the domain model,
/// applies the aggregate's behaviour, then writes the result back.
/// </summary>
public sealed class TimesheetStore(RyvoDbContext db)
{
    public async Task<IReadOnlyList<User>> ListUsersAsync(CancellationToken ct = default)
    {
        var users = await db.Users.AsNoTracking().OrderBy(u => u.Name).ToListAsync(ct);
        return users.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Engagement>> ListEngagementsAsync(CancellationToken ct = default)
    {
        var engagements = await db.Engagements.AsNoTracking().ToListAsync(ct);
        return engagements.Select(ToDomain).ToList();
    }

    public async Task<User> RequireUserAsync(Guid id, CancellationToken ct = default)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
        return user is null
            ? throw new KeyNotFoundException($"User {id} was not found.")
            : ToDomain(user);
    }

    public async Task<Engagement> RequireEngagementAsync(Guid id, CancellationToken ct = default)
    {
        var engagement = await db.Engagements.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
        return engagement is null
            ? throw new KeyNotFoundException($"Engagement {id} was not found.")
            : ToDomain(engagement);
    }

    /// <summary>
    /// Resolves the dev user from the X-User-Id header, falling back to the
    /// dev_user cookie set by POST /api/dev/login/{userId}.
    /// </summary>
    public async Task<User> RequireActorAsync(HttpContext context, CancellationToken ct = default)
    {
        var header = context.Request.Headers["X-User-Id"].ToString();
        var raw = !string.IsNullOrWhiteSpace(header)
            ? header
            : context.Request.Cookies["dev_user"];

        if (!Guid.TryParse(raw, out var id))
            throw new UnauthorizedAccessException(
                "Log in via POST /api/dev/login/{userId} or send the X-User-Id header.");

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
        return user is null
            ? throw new UnauthorizedAccessException($"Unknown dev user id '{id}'.")
            : ToDomain(user);
    }

    public static void EnsureCanAccess(User actor, Engagement engagement)
    {
        if (actor.Id != engagement.ContractorId && actor.Id != engagement.ClientId)
            throw new UnauthorizedDomainException("You are not part of this engagement.");
    }

    public async Task<TimesheetWeek> GetWeekAsync(
        Guid engagementId,
        DateOnly weekOf,
        bool createIfMissing,
        CancellationToken ct = default)
    {
        var (engagement, contractor, weekStart) = await ResolveWeekContextAsync(engagementId, weekOf, ct);
        var record = await FindWeekRecordAsync(engagementId, weekStart, tracking: true, ct);

        if (record is null)
        {
            if (!createIfMissing)
                throw new KeyNotFoundException("No timesheet week exists for that date.");

            record = NewWeekRecord(engagement, contractor, weekStart);
            db.TimesheetWeeks.Add(record);
            await db.SaveChangesAsync(ct);
        }

        return ToDomain(record, ToDomain(engagement), ToDomain(contractor));
    }

    public async Task<T> MutateWeekAsync<T>(
        Guid engagementId,
        DateOnly weekOf,
        bool createIfMissing,
        Func<TimesheetWeek, T> mutate,
        CancellationToken ct = default)
    {
        var (engagement, contractor, weekStart) = await ResolveWeekContextAsync(engagementId, weekOf, ct);
        var record = await FindWeekRecordAsync(engagementId, weekStart, tracking: true, ct);

        if (record is null)
        {
            if (!createIfMissing)
                throw new KeyNotFoundException("No timesheet week exists for that date.");

            record = NewWeekRecord(engagement, contractor, weekStart);
            db.TimesheetWeeks.Add(record);
        }

        var week = ToDomain(record, ToDomain(engagement), ToDomain(contractor));
        var result = mutate(week);
        ApplyChanges(record, week);
        await db.SaveChangesAsync(ct);
        return result;
    }

    public async Task<IReadOnlyList<TimesheetWeek>> WeeksForClientAsync(
        Guid clientId,
        DateOnly weekOf,
        CancellationToken ct = default)
    {
        var engagements = await db.Engagements
            .AsNoTracking()
            .Where(e => e.ClientId == clientId)
            .ToListAsync(ct);

        var contractorIds = engagements.Select(e => e.ContractorId).Distinct().ToList();
        var contractors = await db.Users
            .AsNoTracking()
            .Where(u => contractorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, ct);

        var result = new List<TimesheetWeek>();

        foreach (var engagement in engagements)
        {
            var contractor = contractors[engagement.ContractorId];

            var weekStart = NormalizeWeekStart(
                weekOf, TimeZoneInfo.FindSystemTimeZoneById(contractor.TimeZoneId));

            var record = await FindWeekRecordAsync(engagement.Id, weekStart, tracking: false, ct);
            if (record is not null)
                result.Add(ToDomain(record, ToDomain(engagement), ToDomain(contractor)));
        }

        return result;
    }

    public async Task<Invoice> GenerateInvoiceAsync(
        User client,
        DateOnly rangeStart,
        DateOnly rangeEnd,
        CancellationToken ct = default)
    {
        var engagementEntities = await db.Engagements.AsNoTracking().ToListAsync(ct);
        var userEntities = await db.Users.AsNoTracking().ToListAsync(ct);

        var weekEntities = await db.TimesheetWeeks
            .AsNoTracking()
            .Include(w => w.Entries)
            .Where(w => w.ClientId == client.Id && w.Status == TimesheetStatus.Approved)
            .ToListAsync(ct);

        var engagementById = engagementEntities.ToDictionary(e => e.Id);
        var userById = userEntities.ToDictionary(u => u.Id);

        var weeks = weekEntities
            .Select(w => ToDomain(
                w,
                ToDomain(engagementById[w.EngagementId]),
                ToDomain(userById[w.ContractorId])))
            .ToList();

        var invoice = new InvoiceGenerator().Generate(
            client,
            rangeStart,
            rangeEnd,
            engagementEntities.Select(ToDomain),
            userEntities.Select(ToDomain),
            weeks,
            DateTimeOffset.UtcNow);

        db.Invoices.Add(ToEntity(invoice));
        await db.SaveChangesAsync(ct);
        return invoice;
    }

    public async Task<IReadOnlyList<Invoice>> InvoicesForClientAsync(
        Guid clientId,
        CancellationToken ct = default)
    {
        var records = await db.Invoices
            .AsNoTracking()
            .Include(i => i.LineItems)
            .Where(i => i.ClientId == clientId)
            .OrderByDescending(i => i.GeneratedAtUtc)
            .ToListAsync(ct);

        return records.Select(ToDomain).ToList();
    }

    /// <summary>
    /// Loads an invoice only if it belongs to the given client. Anything else is
    /// reported as missing so invoice ids from other clients cannot be probed.
    /// </summary>
    public async Task<Invoice> RequireInvoiceAsync(
        Guid invoiceId,
        Guid clientId,
        CancellationToken ct = default)
    {
        var record = await db.Invoices
            .AsNoTracking()
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.ClientId == clientId, ct);

        return record is null
            ? throw new KeyNotFoundException($"Invoice {invoiceId} was not found.")
            : ToDomain(record);
    }

    private async Task<(EngagementEntity Engagement, UserEntity Contractor, DateOnly WeekStart)>
        ResolveWeekContextAsync(Guid engagementId, DateOnly weekOf, CancellationToken ct)
    {
        var engagement = await db.Engagements
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == engagementId, ct)
            ?? throw new KeyNotFoundException($"Engagement {engagementId} was not found.");

        var contractor = await db.Users
            .AsNoTracking()
            .FirstAsync(u => u.Id == engagement.ContractorId, ct);

        var weekStart = NormalizeWeekStart(
            weekOf, TimeZoneInfo.FindSystemTimeZoneById(contractor.TimeZoneId));

        return (engagement, contractor, weekStart);
    }

    private Task<TimesheetWeekEntity?> FindWeekRecordAsync(
        Guid engagementId,
        DateOnly weekStart,
        bool tracking,
        CancellationToken ct)
    {
        var query = db.TimesheetWeeks
            .Include(w => w.Entries)
            .Where(w => w.EngagementId == engagementId && w.WeekStartLocal == weekStart);

        if (!tracking)
            query = query.AsNoTracking();

        return query.FirstOrDefaultAsync(ct);
    }

    private static TimesheetWeekEntity NewWeekRecord(
        EngagementEntity engagement,
        UserEntity contractor,
        DateOnly weekStart) =>
        new()
        {
            Id = Guid.NewGuid(),
            EngagementId = engagement.Id,
            ContractorId = engagement.ContractorId,
            ClientId = engagement.ClientId,
            ContractorTimeZoneId = contractor.TimeZoneId,
            WeekStartLocal = weekStart,
            Status = TimesheetStatus.Draft
        };

    private void ApplyChanges(TimesheetWeekEntity record, TimesheetWeek week)
    {
        record.Status = week.Status;
        record.RejectionComment = week.RejectionComment;
        record.ContractorTimeZoneId = week.ContractorTimeZoneId;
        record.WeekStartLocal = week.Boundary.WeekStartLocal;

        var existingById = record.Entries.ToDictionary(e => e.Id);

        foreach (var entry in week.Entries)
        {
            if (existingById.Remove(entry.Id, out var entity))
            {
                entity.StartUtc = entry.Period.StartUtc;
                entity.EndUtc = entry.Period.EndUtc;
                entity.Description = entry.Description;
            }
            else
            {
                // Add explicitly with a FK only (no navigation): an entity with an
                // already-set key that is merely hung off a tracked collection is
                // treated as an UPDATE by EF's change detection.
                db.TimeEntries.Add(new TimeEntryEntity
                {
                    Id = entry.Id,
                    WeekId = record.Id,
                    StartUtc = entry.Period.StartUtc,
                    EndUtc = entry.Period.EndUtc,
                    Description = entry.Description
                });
            }
        }

        foreach (var removed in existingById.Values)
            db.TimeEntries.Remove(removed);
    }

    private static DateOnly NormalizeWeekStart(DateOnly anyDate, TimeZoneInfo timeZone) =>
        new WeekBoundary(anyDate, timeZone).WeekStartLocal;

    private static User ToDomain(UserEntity entity) =>
        new(entity.Id, entity.Name, entity.Role, entity.TimeZoneId);

    private static Engagement ToDomain(EngagementEntity entity) =>
        new(entity.Id, entity.ClientId, entity.ContractorId, new Money(entity.HourlyRateUsd));

    private static TimeEntry ToDomain(TimeEntryEntity entity) =>
        new(entity.Id, new TimeRange(entity.StartUtc, entity.EndUtc), entity.Description);

    private static TimesheetWeek ToDomain(
        TimesheetWeekEntity entity,
        Engagement engagement,
        User contractor) =>
        TimesheetWeek.Rehydrate(
            entity.Id,
            engagement,
            contractor,
            entity.WeekStartLocal,
            entity.Status,
            entity.RejectionComment,
            entity.Entries.Select(ToDomain));

    private static Invoice ToDomain(InvoiceEntity entity) =>
        new(
            entity.Id,
            entity.ClientId,
            entity.RangeStart,
            entity.RangeEnd,
            entity.GeneratedAtUtc,
            entity.LineItems
                .OrderBy(l => l.WorkDateClientLocal)
                .ThenBy(l => l.ContractorName)
                .Select(l => new InvoiceLineItem(
                    l.ContractorId,
                    l.ContractorName,
                    l.WorkDateClientLocal,
                    l.ApprovedHours,
                    new Money(l.HourlyRateUsd),
                    new Money(l.LineTotalUsd)))
                .ToList());

    private static InvoiceEntity ToEntity(Invoice invoice) =>
        new()
        {
            Id = invoice.Id,
            ClientId = invoice.ClientId,
            RangeStart = invoice.RangeStart,
            RangeEnd = invoice.RangeEnd,
            GeneratedAtUtc = invoice.GeneratedAtUtc,
            TotalApprovedHours = invoice.TotalApprovedHours,
            TotalAmountUsd = invoice.TotalAmount.Amount,
            LineItems = invoice.LineItems
                .Select(li => new InvoiceLineEntity
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoice.Id,
                    ContractorId = li.ContractorId,
                    ContractorName = li.ContractorName,
                    WorkDateClientLocal = li.WorkDateClientLocal,
                    ApprovedHours = li.ApprovedHours,
                    HourlyRateUsd = li.HourlyRate.Amount,
                    LineTotalUsd = li.LineTotal.Amount
                })
                .ToList()
        };
}

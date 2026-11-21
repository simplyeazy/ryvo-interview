using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ryvo_interview_be.Data;
using ryvo_interview_be.Domain;

namespace ryvo_interview_be.Tests;

/// <summary>
/// Exercises the SQLite store against a real (in-memory) SQLite database,
/// including the migrations that create and seed the schema.
/// </summary>
public sealed class PersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly RyvoDbContext _db;
    private readonly TimesheetStore _store;

    public PersistenceTests()
    {
        // A single open connection keeps the :memory: database alive.
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _db = NewContext();
        _db.Database.Migrate();
        _store = new TimesheetStore(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private RyvoDbContext NewContext() =>
        new(new DbContextOptionsBuilder<RyvoDbContext>()
            .UseSqlite(_connection)
            .Options);

    private static readonly Guid AcmeBudiEngagement = SeedData.AcmeBudiEngagementId;
    private static readonly DateOnly WeekOf = new(2026, 11, 23);

    [Fact]
    public async Task Migrations_SeedUsersEngagementsAndWeeks()
    {
        var users = await _store.ListUsersAsync();
        var engagements = await _store.ListEngagementsAsync();

        Assert.Equal(4, users.Count);
        Assert.Equal(3, engagements.Count);

        var week = await _store.GetWeekAsync(AcmeBudiEngagement, new DateOnly(2026, 11, 16), false);
        Assert.Equal(TimesheetStatus.Approved, week.Status);
        Assert.Equal(20m, week.TotalHours);
        Assert.Equal(3, week.Entries.Count);
    }

    [Fact]
    public async Task AddEntry_IsPersisted_AndReadableFromANewContext()
    {
        var start = new DateTimeOffset(2026, 11, 25, 1, 0, 0, TimeSpan.Zero);
        var period = new TimeRange(start, start.AddHours(4));

        var added = await _store.MutateWeekAsync(
            AcmeBudiEngagement, WeekOf, true,
            week => week.AddEntry(SeedData.BudiId, period, "persisted work"));

        // Drop the tracked state and read through a fresh DbContext.
        _db.Dispose();
        using var fresh = NewContext();
        var store = new TimesheetStore(fresh);

        var week = await store.GetWeekAsync(AcmeBudiEngagement, WeekOf, false);

        var entry = Assert.Single(week.Entries, e => e.Description == "persisted work");
        Assert.Equal(added.Id, entry.Id);
        Assert.Equal(period, entry.Period);
        Assert.Equal(4m, entry.Period.TotalHours);

        // The seeded draft entry is still there alongside it.
        Assert.Equal(2, week.Entries.Count);
        Assert.Equal(10m, week.TotalHours);
    }

    [Fact]
    public async Task UpdateAndRemoveEntry_ArePersisted()
    {
        var before = await _store.MutateWeekAsync(
            AcmeBudiEngagement, WeekOf, true,
            week => week.AddEntry(
                SeedData.BudiId,
                new TimeRange(
                    new DateTimeOffset(2026, 11, 25, 1, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 11, 25, 5, 0, 0, TimeSpan.Zero)),
                "original"));

        var shorter = new TimeRange(
            new DateTimeOffset(2026, 11, 25, 1, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 25, 3, 0, 0, TimeSpan.Zero));

        await _store.MutateWeekAsync(AcmeBudiEngagement, WeekOf, false, week =>
        {
            week.UpdateEntry(SeedData.BudiId, before.Id, shorter, "shortened");
            return true;
        });

        _db.Dispose();
        using var fresh = NewContext();
        var store = new TimesheetStore(fresh);

        var updated = await store.GetWeekAsync(AcmeBudiEngagement, WeekOf, false);
        var entry = Assert.Single(updated.Entries, e => e.Id == before.Id);
        Assert.Equal("shortened", entry.Description);
        Assert.Equal(2m, entry.Period.TotalHours);

        await store.MutateWeekAsync(AcmeBudiEngagement, WeekOf, false, week =>
        {
            week.RemoveEntry(SeedData.BudiId, before.Id);
            return true;
        });

        _db.Dispose();
        using var again = NewContext();
        var reloaded = await new TimesheetStore(again)
            .GetWeekAsync(AcmeBudiEngagement, WeekOf, false);

        Assert.DoesNotContain(reloaded.Entries, e => e.Id == before.Id);
        Assert.Single(reloaded.Entries);
    }

    [Fact]
    public async Task StatusChanges_ArePersisted()
    {
        await _store.MutateWeekAsync(
            AcmeBudiEngagement, WeekOf, true,
            week =>
            {
                week.Submit(SeedData.BudiId);
                return true;
            });

        _db.Dispose();
        using var fresh = NewContext();
        var store = new TimesheetStore(fresh);

        var submitted = await store.GetWeekAsync(AcmeBudiEngagement, WeekOf, false);
        Assert.Equal(TimesheetStatus.Submitted, submitted.Status);

        await store.MutateWeekAsync(AcmeBudiEngagement, WeekOf, false, week =>
        {
            week.Reject(SeedData.AcmeId, "  Please add details.  ");
            return true;
        });

        _db.Dispose();
        using var again = NewContext();
        var rejected = await new TimesheetStore(again)
            .GetWeekAsync(AcmeBudiEngagement, WeekOf, false);

        Assert.Equal(TimesheetStatus.Rejected, rejected.Status);
        Assert.Equal("Please add details.", rejected.RejectionComment);
    }

    [Fact]
    public async Task GenerateInvoice_PersistsInvoiceAndLineItems()
    {
        var client = await _store.RequireUserAsync(SeedData.AcmeId);

        var invoice = await _store.GenerateInvoiceAsync(
            client, new DateOnly(2026, 11, 16), new DateOnly(2026, 11, 30));

        Assert.Equal(20m, invoice.TotalApprovedHours);
        Assert.Equal(new Money(1500m), invoice.TotalAmount);
        Assert.Equal(3, invoice.LineItems.Count);

        _db.Dispose();
        using var fresh = NewContext();
        var store = new TimesheetStore(fresh);

        var reloaded = await store.RequireInvoiceAsync(invoice.Id, SeedData.AcmeId);
        Assert.Equal(invoice.Id, reloaded.Id);
        Assert.Equal(20m, reloaded.TotalApprovedHours);
        Assert.Equal(new Money(1500m), reloaded.TotalAmount);
        Assert.Equal(
            invoice.LineItems.Select(l => (l.ContractorId, l.WorkDateClientLocal, l.ApprovedHours, l.LineTotal)),
            reloaded.LineItems.Select(l => (l.ContractorId, l.WorkDateClientLocal, l.ApprovedHours, l.LineTotal)));

        // Another client cannot see it.
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            store.RequireInvoiceAsync(invoice.Id, SeedData.GlobexId));
    }

    [Fact]
    public async Task GetWeek_DoesNotCreateWhenMissing()
    {
        var missing = new DateOnly(2027, 3, 1);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _store.GetWeekAsync(AcmeBudiEngagement, missing, false));

        var created = await _store.GetWeekAsync(AcmeBudiEngagement, missing, true);
        Assert.Equal(TimesheetStatus.Draft, created.Status);
        Assert.Empty(created.Entries);
        Assert.Equal(new DateOnly(2027, 3, 1), created.Boundary.WeekStartLocal);
    }
}

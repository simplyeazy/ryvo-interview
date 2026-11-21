using ryvo_interview_be.Domain;
using ryvo_interview_be.Domain.Exceptions;

namespace ryvo_interview_be.Tests;

public class TimesheetAndInvoiceDomainTests
{
    private static readonly User JakartaContractor = new(
        Guid.NewGuid(), "Budi", UserRole.Contractor, "Asia/Jakarta");

    private static readonly User NewYorkClient = new(
        Guid.NewGuid(), "Acme Corp", UserRole.Client, "America/New_York");

    private static readonly Engagement Contract = new(
        Guid.NewGuid(), NewYorkClient.Id, JakartaContractor.Id, new Money(75.00m));

    private static TimeRange Utc(int y, int mo, int d, int h, int mi, int h2, int mi2) =>
        new(new DateTimeOffset(y, mo, d, h, mi, 0, TimeSpan.Zero),
            new DateTimeOffset(y, mo, d, h2, mi2, 0, TimeSpan.Zero));

    [Fact]
    public void AddEntry_Exceeding12HourDailyCapInContractorTimeZone_ThrowsDailyCapExceeded()
    {
        var week = new TimesheetWeek(
            Guid.NewGuid(), Contract, JakartaContractor, new DateOnly(2026, 11, 16));

        // 08:00-16:00 WIB (8h)
        var morningShift = Utc(2026, 11, 16, 1, 0, 9, 0);
        // 17:00-22:00 WIB (5h) -> 13h total on the same WIB day
        var eveningShift = Utc(2026, 11, 16, 10, 0, 15, 0);

        week.AddEntry(JakartaContractor.Id, morningShift);

        Assert.Throws<DailyCapExceededException>(() =>
            week.AddEntry(JakartaContractor.Id, eveningShift));
    }

    [Fact]
    public void AddEntry_Exactly12HoursInTenMinuteChunks_IsAllowed()
    {
        var week = new TimesheetWeek(
            Guid.NewGuid(), Contract, JakartaContractor, new DateOnly(2026, 11, 16));

        // 72 x 10 minutes = exactly 12h. Guards against decimal drift at the cap.
        for (var i = 0; i < 72; i++)
        {
            var start = new DateTimeOffset(2026, 11, 16, 1, 0, 0, TimeSpan.Zero)
                .AddMinutes(i * 10);
            week.AddEntry(JakartaContractor.Id, new TimeRange(start, start.AddMinutes(10)));
        }

        Assert.Equal(12m, week.TotalHours);
    }

    [Fact]
    public void Submit_LocksWeekFromFurtherEdits_UntilClientRejectsWithComment()
    {
        var week = new TimesheetWeek(
            Guid.NewGuid(), Contract, JakartaContractor, new DateOnly(2026, 11, 16));
        var shift = Utc(2026, 11, 16, 2, 0, 6, 0);

        week.AddEntry(JakartaContractor.Id, shift);
        week.Submit(JakartaContractor.Id);

        Assert.Throws<TimesheetLockedException>(() =>
            week.AddEntry(JakartaContractor.Id, shift));

        week.Reject(NewYorkClient.Id, "Please split design and coding tasks.");

        Assert.Equal(TimesheetStatus.Rejected, week.Status);
        Assert.Equal("Please split design and coding tasks.", week.RejectionComment);

        week.UpdateEntry(
            JakartaContractor.Id, week.Entries.Single().Id, shift, "Updated description");
    }

    [Fact]
    public void Reject_WithoutComment_Throws()
    {
        var week = new TimesheetWeek(
            Guid.NewGuid(), Contract, JakartaContractor, new DateOnly(2026, 11, 16));
        week.AddEntry(JakartaContractor.Id, Utc(2026, 11, 16, 2, 0, 6, 0));
        week.Submit(JakartaContractor.Id);

        Assert.Throws<DomainException>(() => week.Reject(NewYorkClient.Id, "   "));
    }

    [Fact]
    public void InvoiceGenerator_BillsOnlyApprovedHoursInClientTimeZone_WithUsdPrecision()
    {
        var approvedWeek = new TimesheetWeek(
            Guid.NewGuid(), Contract, JakartaContractor, new DateOnly(2026, 11, 16));
        var unapprovedWeek = new TimesheetWeek(
            Guid.NewGuid(), Contract, JakartaContractor, new DateOnly(2026, 11, 23));

        // 2026-11-16 14:00-17:30 UTC = 09:00-12:30 EST (3.5h)
        approvedWeek.AddEntry(JakartaContractor.Id, Utc(2026, 11, 16, 14, 0, 17, 30));
        approvedWeek.Submit(JakartaContractor.Id);
        approvedWeek.Approve(NewYorkClient.Id);

        // Draft week must be excluded.
        unapprovedWeek.AddEntry(JakartaContractor.Id, Utc(2026, 11, 23, 2, 0, 6, 0));

        var invoice = new InvoiceGenerator().Generate(
            NewYorkClient,
            new DateOnly(2026, 11, 16),
            new DateOnly(2026, 11, 30),
            [Contract],
            [JakartaContractor],
            [approvedWeek, unapprovedWeek],
            DateTimeOffset.UtcNow);

        Assert.Equal(3.5m, invoice.TotalApprovedHours);
        Assert.Equal(new Money(262.50m), invoice.TotalAmount);
    }

    [Fact]
    public void GetContractorDailyTotals_SplitsShiftAcrossMidnight()
    {
        var week = new TimesheetWeek(
            Guid.NewGuid(), Contract, JakartaContractor, new DateOnly(2026, 11, 16));

        // 2026-11-16 14:00Z - 2026-11-17 02:00Z = 21:00-09:00 WIB.
        // 3h on the 16th, 9h on the 17th.
        week.AddEntry(JakartaContractor.Id, new TimeRange(
            new DateTimeOffset(2026, 11, 16, 14, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 17, 2, 0, 0, TimeSpan.Zero)));

        var totals = week.GetContractorDailyTotals();

        Assert.Equal(3m, totals[new DateOnly(2026, 11, 16)]);
        Assert.Equal(9m, totals[new DateOnly(2026, 11, 17)]);
    }
}

using System.Globalization;
using System.Text;
using ryvo_interview_be.Api;
using ryvo_interview_be.Domain;

namespace ryvo_interview_be.Tests;

public sealed class CsvExportTests
{
    private static readonly User Client = new(
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        "Acme Corp",
        UserRole.Client,
        "America/New_York");

    [Fact]
    public void InvoiceCsv_UsesInvariantNumbers_EvenUnderCommaDecimalCulture()
    {
        using var culture = new CultureScope("id-ID");
        var invoice = ApprovedInvoiceFor("Budi Santoso");

        var rows = Rows(CsvExports.Invoice(invoice));

        Assert.Equal(
            "Contractor,Work Date (Client Local),Approved Hours,Hourly Rate (USD),Line Total (USD)",
            rows[0]);
        Assert.Equal("Budi Santoso,2026-11-16,3.50,75.00,262.50", rows[1]);
        Assert.Equal("TOTAL,,3.50,,262.50", rows[2]);

        // The whole point: id-ID would render these as 3,50 / 262,50 without the invariant culture.
        Assert.DoesNotContain("3,50", Encoding.UTF8.GetString(CsvExports.Invoice(invoice)));
    }

    [Fact]
    public void InvoiceCsv_QuotesNamesWithCommasAndQuotes()
    {
        var invoice = ApprovedInvoiceFor("O\"Brien, Ltd");

        var rows = Rows(CsvExports.Invoice(invoice));

        Assert.Equal("\"O\"\"Brien, Ltd\",2026-11-16,3.50,75.00,262.50", rows[1]);
    }

    [Fact]
    public void WeekCsv_RendersContractorLocalTimesWithOffset()
    {
        var week = WeekWithEntry(
            "Design review",
            new DateTimeOffset(2026, 11, 16, 1, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 16, 9, 0, 0, TimeSpan.Zero));

        var rows = Rows(CsvExports.Week(week));

        Assert.Equal("Start (Contractor Local),End (Contractor Local),Hours,Description", rows[0]);
        Assert.Equal("2026-11-16T08:00:00+07:00,2026-11-16T16:00:00+07:00,8.00,Design review", rows[1]);
    }

    [Fact]
    public void WeekCsv_OrdersEntriesChronologically_AndEscapesDescriptions()
    {
        var week = WeekWithEntry(
            "second",
            new DateTimeOffset(2026, 11, 16, 6, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 16, 8, 0, 0, TimeSpan.Zero));

        week.AddEntry(
            week.ContractorId,
            new TimeRange(
                new DateTimeOffset(2026, 11, 16, 1, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 11, 16, 3, 0, 0, TimeSpan.Zero)),
            "Fix login, retry \"v2\"");

        var rows = Rows(CsvExports.Week(week));

        Assert.Contains(",\"Fix login, retry \"\"v2\"\"\"", rows[1]);
        Assert.EndsWith(",second", rows[2]);
    }

    [Fact]
    public void CsvBuilder_EndsRowsWithCrLf_AndLeavesEmptyValuesBare()
    {
        var bytes = new CsvBuilder()
            .Row("a", null, "b c")
            .Row("x")
            .ToUtf8Bytes();

        Assert.Equal("a,,b c\r\nx\r\n", Encoding.UTF8.GetString(bytes));
        Assert.False(bytes.Length > 3 && bytes[0] == 0xEF, "CSV must not start with a UTF-8 BOM.");
    }

    [Fact]
    public void CsvBuilder_QuotesValuesWithSurroundingSpaces()
    {
        var text = Encoding.UTF8.GetString(new CsvBuilder().Row(" padded ", "clean").ToUtf8Bytes());
        Assert.Equal("\" padded \",clean\r\n", text);
    }

    [Theory]
    [InlineData("Acme Corp", "invoice-acme-corp-2026-11-16-to-2026-11-30.csv")]
    [InlineData("  Acme   Corp!! ", "invoice-acme-corp-2026-11-16-to-2026-11-30.csv")]
    [InlineData("---", "invoice-export-2026-11-16-to-2026-11-30.csv")]
    public void InvoiceFileName_IsAsciiSlug(string clientName, string expected)
    {
        var invoice = ApprovedInvoiceFor("Budi Santoso");

        Assert.Equal(expected, CsvExports.InvoiceFileName(invoice, clientName));
    }

    private static TimesheetWeek WeekWithEntry(string description, DateTimeOffset start, DateTimeOffset end)
    {
        var contractor = new User(Guid.NewGuid(), "Budi Santoso", UserRole.Contractor, "Asia/Jakarta");
        var engagement = new Engagement(Guid.NewGuid(), Client.Id, contractor.Id, new Money(75m));
        var week = new TimesheetWeek(Guid.NewGuid(), engagement, contractor, new DateOnly(2026, 11, 16));

        week.AddEntry(contractor.Id, new TimeRange(start, end), description);
        return week;
    }

    private static Invoice ApprovedInvoiceFor(string contractorName)
    {
        var contractor = new User(Guid.NewGuid(), contractorName, UserRole.Contractor, "Asia/Jakarta");
        var engagement = new Engagement(Guid.NewGuid(), Client.Id, contractor.Id, new Money(75m));
        var week = new TimesheetWeek(Guid.NewGuid(), engagement, contractor, new DateOnly(2026, 11, 16));

        // 14:00-17:30 UTC on 2026-11-16 is 09:00-12:30 in New York: 3.5h.
        week.AddEntry(contractor.Id, new TimeRange(
            new DateTimeOffset(2026, 11, 16, 14, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 16, 17, 30, 0, TimeSpan.Zero)));
        week.Submit(contractor.Id);
        week.Approve(Client.Id);

        return new InvoiceGenerator().Generate(
            Client,
            new DateOnly(2026, 11, 16),
            new DateOnly(2026, 11, 30),
            [engagement],
            [contractor],
            [week],
            DateTimeOffset.UtcNow);
    }

    private static string[] Rows(byte[] csv) =>
        Encoding.UTF8.GetString(csv).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _original = CultureInfo.CurrentCulture;

        public CultureScope(string name) => CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);

        public void Dispose() => CultureInfo.CurrentCulture = _original;
    }
}

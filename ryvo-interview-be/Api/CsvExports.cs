using System.Globalization;
using System.Text;
using ryvo_interview_be.Domain;

namespace ryvo_interview_be.Api;

/// <summary>
/// CSV projections of the domain's documents. Every number and date is formatted
/// with <see cref="CultureInfo.InvariantCulture"/>: a server running under a
/// comma-decimal culture such as en-ID must never emit <c>3,50</c> into a
/// comma-delimited file.
/// </summary>
public static class CsvExports
{
    public const string ContentType = "text/csv; charset=utf-8";

    private const string DateFormat = "yyyy-MM-dd";
    private const string TimestampFormat = "yyyy-MM-dd'T'HH:mm:sszzz";
    private const string DecimalFormat = "0.00";

    /// <summary>
    /// One row per invoice line, in client-local work dates, followed by a TOTAL
    /// row. Machines should use the JSON invoice; this file is for accounting.
    /// </summary>
    public static byte[] Invoice(Invoice invoice)
    {
        var csv = new CsvBuilder()
            .Row(
                "Contractor",
                "Work Date (Client Local)",
                "Approved Hours",
                "Hourly Rate (USD)",
                "Line Total (USD)");

        foreach (var line in invoice.LineItems)
        {
            csv.Row(
                line.ContractorName,
                line.WorkDateClientLocal.ToString(DateFormat, CultureInfo.InvariantCulture),
                Format(line.ApprovedHours),
                Format(line.HourlyRate.Amount),
                Format(line.LineTotal.Amount));
        }

        csv.Row(
            "TOTAL",
            string.Empty,
            Format(invoice.TotalApprovedHours),
            string.Empty,
            Format(invoice.TotalAmount.Amount));

        return csv.ToUtf8Bytes();
    }

    /// <summary>
    /// One row per time entry, ordered chronologically, with start and end shown in
    /// the contractor's time zone (offset included, so the instant stays exact).
    /// </summary>
    public static byte[] Week(TimesheetWeek week)
    {
        var contractorTimeZone = TimeZoneInfo.FindSystemTimeZoneById(week.ContractorTimeZoneId);

        var csv = new CsvBuilder()
            .Row(
                "Start (Contractor Local)",
                "End (Contractor Local)",
                "Hours",
                "Description");

        foreach (var entry in week.Entries.OrderBy(e => e.Period.StartUtc))
        {
            var startLocal = TimeZoneInfo.ConvertTime(entry.Period.StartUtc, contractorTimeZone);
            var endLocal = TimeZoneInfo.ConvertTime(entry.Period.EndUtc, contractorTimeZone);

            csv.Row(
                startLocal.ToString(TimestampFormat, CultureInfo.InvariantCulture),
                endLocal.ToString(TimestampFormat, CultureInfo.InvariantCulture),
                Format(entry.Period.TotalHours),
                entry.Description);
        }

        return csv.ToUtf8Bytes();
    }

    public static string InvoiceFileName(Invoice invoice, string clientName) =>
        $"invoice-{Slug(clientName)}" +
        $"-{invoice.RangeStart.ToString(DateFormat, CultureInfo.InvariantCulture)}" +
        $"-to-{invoice.RangeEnd.ToString(DateFormat, CultureInfo.InvariantCulture)}.csv";

    public static string WeekFileName(TimesheetWeek week, string contractorName) =>
        $"timesheet-{Slug(contractorName)}" +
        $"-{week.Boundary.WeekStartLocal.ToString(DateFormat, CultureInfo.InvariantCulture)}.csv";

    private static string Format(decimal value) => value.ToString(DecimalFormat, CultureInfo.InvariantCulture);

    /// <summary>
    /// ASCII-only slug for Content-Disposition: header values must not depend on
    /// the client's filename encoding.
    /// </summary>
    private static string Slug(string value)
    {
        var builder = new StringBuilder(value.Length);
        var lastWasDash = false;

        foreach (var c in value.Trim().ToLowerInvariant())
        {
            var keep = c is >= 'a' and <= 'z' or >= '0' and <= '9';
            if (keep)
            {
                builder.Append(c);
                lastWasDash = false;
            }
            else if (!lastWasDash && builder.Length > 0)
            {
                builder.Append('-');
                lastWasDash = true;
            }
        }

        var slug = builder.ToString().Trim('-');
        return slug.Length == 0 ? "export" : slug;
    }
}

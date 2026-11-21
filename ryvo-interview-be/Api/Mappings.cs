using ryvo_interview_be.Domain;

namespace ryvo_interview_be.Api;

public static class Mappings
{
    public static UserResponse ToResponse(this User user) =>
        new(user.Id, user.Name, user.Role.ToString(), user.TimeZoneId);

    public static EngagementResponse ToResponse(
        this Engagement engagement,
        IReadOnlyDictionary<Guid, string> userNames) =>
        new(
            engagement.Id,
            engagement.ClientId,
            userNames.GetValueOrDefault(engagement.ClientId, "Unknown"),
            engagement.ContractorId,
            userNames.GetValueOrDefault(engagement.ContractorId, "Unknown"),
            engagement.HourlyRate.Amount);

    public static EntryResponse ToResponse(this TimeEntry entry) =>
        new(
            entry.Id,
            entry.Period.StartUtc,
            entry.Period.EndUtc,
            entry.Period.TotalHours,
            entry.Description);

    public static WeekResponse ToResponse(this TimesheetWeek week) =>
        new(
            week.Id,
            week.EngagementId,
            week.ContractorId,
            week.ClientId,
            week.ContractorTimeZoneId,
            week.Boundary.WeekStartLocal,
            week.Boundary.WeekEndLocal,
            week.Boundary.StartUtc,
            week.Boundary.EndUtcExclusive,
            week.Status.ToString(),
            week.RejectionComment,
            week.TotalHours,
            week.GetContractorDailyTotals(),
            week.Entries
                .OrderBy(e => e.Period.StartUtc)
                .Select(ToResponse)
                .ToList());

    public static InvoiceResponse ToResponse(this Invoice invoice) =>
        new(
            invoice.Id,
            invoice.ClientId,
            invoice.RangeStart,
            invoice.RangeEnd,
            invoice.GeneratedAtUtc,
            invoice.TotalApprovedHours,
            invoice.TotalAmount.Amount,
            invoice.LineItems
                .Select(li => new InvoiceLineResponse(
                    li.ContractorId,
                    li.ContractorName,
                    li.WorkDateClientLocal,
                    li.ApprovedHours,
                    li.HourlyRate.Amount,
                    li.LineTotal.Amount))
                .ToList());
}

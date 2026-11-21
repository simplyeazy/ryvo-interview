using ryvo_interview_be.Domain.Exceptions;

namespace ryvo_interview_be.Domain;

/// <summary>
/// Builds an immutable invoice for one client over a date range of client-local
/// dates. Only approved weeks belonging to that client's engagements are billable.
/// </summary>
public sealed class InvoiceGenerator
{
    public Invoice Generate(
        User client,
        DateOnly rangeStart,
        DateOnly rangeEnd,
        IEnumerable<Engagement> clientEngagements,
        IEnumerable<User> contractors,
        IEnumerable<TimesheetWeek> timesheetWeeks,
        DateTimeOffset nowUtc)
    {
        if (client.Role != UserRole.Client)
            throw new UnauthorizedDomainException("Invoices can only be generated for a client.");
        if (rangeEnd < rangeStart)
            throw new DomainException("Invoice end date cannot be earlier than start date.");

        var clientTz = client.GetTimeZone();
        var engagementById = clientEngagements
            .Where(e => e.ClientId == client.Id)
            .ToDictionary(e => e.Id);

        var contractorNames = contractors.ToDictionary(c => c.Id, c => c.Name);

        var approvedWeeks = timesheetWeeks
            .Where(w => w.ClientId == client.Id
                        && w.Status == TimesheetStatus.Approved
                        && engagementById.ContainsKey(w.EngagementId));

        // Group ticks per engagement and client-local date, so hours are rounded
        // once per billable line instead of once per entry.
        var groupedTicks = new Dictionary<(Guid EngagementId, DateOnly LocalDate), long>();

        foreach (var week in approvedWeeks)
        {
            foreach (var entry in week.Entries)
            {
                foreach (var (clientLocalDate, ticks) in entry.Period.GetTicksPerLocalDate(clientTz))
                {
                    if (clientLocalDate < rangeStart || clientLocalDate > rangeEnd)
                        continue;

                    var key = (week.EngagementId, clientLocalDate);
                    groupedTicks[key] = groupedTicks.GetValueOrDefault(key) + ticks;
                }
            }
        }

        var lineItems = groupedTicks
            .OrderBy(kvp => kvp.Key.LocalDate)
            .ThenBy(kvp => contractorNames.GetValueOrDefault(
                engagementById[kvp.Key.EngagementId].ContractorId))
            .Select(kvp =>
            {
                var engagement = engagementById[kvp.Key.EngagementId];
                var roundedHours = decimal.Round(
                    (decimal)kvp.Value / TimeSpan.TicksPerHour, 2, MidpointRounding.AwayFromZero);
                var lineTotal = Money.FromHoursAndRate(roundedHours, engagement.HourlyRate);

                return new InvoiceLineItem(
                    ContractorId: engagement.ContractorId,
                    ContractorName: contractorNames.GetValueOrDefault(
                        engagement.ContractorId, "Unknown"),
                    WorkDateClientLocal: kvp.Key.LocalDate,
                    ApprovedHours: roundedHours,
                    HourlyRate: engagement.HourlyRate,
                    LineTotal: lineTotal);
            })
            .ToList();

        return new Invoice(
            id: Guid.NewGuid(),
            clientId: client.Id,
            rangeStart: rangeStart,
            rangeEnd: rangeEnd,
            generatedAtUtc: nowUtc,
            lineItems: lineItems);
    }
}

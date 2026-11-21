namespace ryvo_interview_be.Api;

public sealed record UserResponse(Guid Id, string Name, string Role, string TimeZoneId);

public sealed record EngagementResponse(
    Guid Id,
    Guid ClientId,
    string ClientName,
    Guid ContractorId,
    string ContractorName,
    decimal HourlyRateUsd);

public sealed record UpsertEntryRequest(
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    string? Description);

public sealed record RejectRequest(string Comment);

public sealed record GenerateInvoiceRequest(DateOnly RangeStart, DateOnly RangeEnd);

public sealed record EntryResponse(
    Guid Id,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    decimal Hours,
    string Description);

public sealed record WeekResponse(
    Guid Id,
    Guid EngagementId,
    Guid ContractorId,
    Guid ClientId,
    string ContractorTimeZoneId,
    DateOnly WeekStartLocal,
    DateOnly WeekEndLocal,
    DateTimeOffset WeekStartUtc,
    DateTimeOffset WeekEndUtcExclusive,
    string Status,
    string? RejectionComment,
    decimal TotalHours,
    IReadOnlyDictionary<DateOnly, decimal> DailyTotals,
    IReadOnlyList<EntryResponse> Entries);

public sealed record InvoiceLineResponse(
    Guid ContractorId,
    string ContractorName,
    DateOnly WorkDateClientLocal,
    decimal ApprovedHours,
    decimal HourlyRateUsd,
    decimal LineTotalUsd);

public sealed record InvoiceResponse(
    Guid Id,
    Guid ClientId,
    DateOnly RangeStart,
    DateOnly RangeEnd,
    DateTimeOffset GeneratedAtUtc,
    decimal TotalApprovedHours,
    decimal TotalAmountUsd,
    IReadOnlyList<InvoiceLineResponse> LineItems);

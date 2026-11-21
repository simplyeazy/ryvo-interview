namespace ryvo_interview_be.Domain.Exceptions;

/// <summary>
/// The proposed entry shares time with an existing entry in the same week.
/// The API maps this to HTTP 409.
/// </summary>
public sealed class OverlappingTimeEntryException(string message) : DomainException(message);

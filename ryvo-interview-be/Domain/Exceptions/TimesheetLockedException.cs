namespace ryvo_interview_be.Domain.Exceptions;

/// <summary>
/// The week is submitted or approved, so its entries can no longer be edited.
/// The API maps this to HTTP 409.
/// </summary>
public sealed class TimesheetLockedException(string message) : DomainException(message);

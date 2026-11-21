namespace ryvo_interview_be.Domain.Exceptions;

/// <summary>
/// An entry would push a local calendar day past the 12-hour cap in the
/// contractor's time zone. The API maps this to HTTP 422.
/// </summary>
public sealed class DailyCapExceededException(string message) : DomainException(message);

namespace ryvo_interview_be.Domain.Exceptions;

/// <summary>
/// Base type for a violated domain rule. The API maps this to HTTP 400.
/// </summary>
public class DomainException(string message) : Exception(message);

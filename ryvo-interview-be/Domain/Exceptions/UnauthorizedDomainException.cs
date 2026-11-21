namespace ryvo_interview_be.Domain.Exceptions;

/// <summary>
/// The caller is known but not allowed to perform this action. The API maps this
/// to HTTP 403.
/// </summary>
public sealed class UnauthorizedDomainException(string message) : DomainException(message);

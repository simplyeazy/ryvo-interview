using ryvo_interview_be.Domain.Exceptions;

namespace ryvo_interview_be.Domain;

/// <summary>
/// A client's contract with one contractor at a fixed hourly rate.
/// </summary>
public sealed class Engagement
{
    public Guid Id { get; }
    public Guid ClientId { get; }
    public Guid ContractorId { get; }
    public Money HourlyRate { get; }

    public Engagement(Guid id, Guid clientId, Guid contractorId, Money hourlyRate)
    {
        if (clientId == contractorId)
            throw new DomainException("Client and contractor cannot be the same.");
        if (hourlyRate.Amount <= 0)
            throw new DomainException("Hourly rate must be greater than zero.");

        Id = id;
        ClientId = clientId;
        ContractorId = contractorId;
        HourlyRate = hourlyRate;
    }
}

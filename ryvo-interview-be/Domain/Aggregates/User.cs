using ryvo_interview_be.Domain.Exceptions;

namespace ryvo_interview_be.Domain;

/// <summary>
/// A client, who reviews and pays, or a contractor, who records hours.
/// </summary>
public sealed class User
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public UserRole Role { get; private set; }
    public string TimeZoneId { get; private set; }

    public User(Guid id, string name, UserRole role, string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("User name is required.");
        _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

        Id = id;
        Name = name;
        Role = role;
        TimeZoneId = timeZoneId;
    }

    public TimeZoneInfo GetTimeZone() => TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
}

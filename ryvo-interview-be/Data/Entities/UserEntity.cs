using ryvo_interview_be.Domain;

namespace ryvo_interview_be.Data.Entities;

public sealed class UserEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string TimeZoneId { get; set; } = string.Empty;
}

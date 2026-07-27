namespace DogPlatform.Contracts.IntegrationEvents.Identity;

public sealed class UserRegisteredIntegrationEvent : IntegrationEvent
{
    public UserRegisteredIntegrationEvent(Guid userId, string email, string fullName)
    {
        UserId = userId;
        Email = email;
        FullName = fullName;
    }

    public Guid UserId { get; }
    public string Email { get; }
    public string FullName { get; }
}

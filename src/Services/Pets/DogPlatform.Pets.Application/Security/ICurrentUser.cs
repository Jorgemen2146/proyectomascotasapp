namespace DogPlatform.Pets.Application.Security;

public interface ICurrentUser
{
    Guid UserId { get; }
    bool IsAuthenticated { get; }
}

namespace DogPlatform.Genealogy.Application.Security;

public interface ICurrentUser
{
    Guid UserId { get; }
    string Email { get; }
    string DisplayName { get; }
    bool IsAuthenticated { get; }
}

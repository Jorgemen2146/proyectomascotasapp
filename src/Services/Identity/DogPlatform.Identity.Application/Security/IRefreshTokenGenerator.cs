namespace DogPlatform.Identity.Application.Security;

public interface IRefreshTokenGenerator
{
    string Generate();
    int RefreshTokenDays { get; }
}

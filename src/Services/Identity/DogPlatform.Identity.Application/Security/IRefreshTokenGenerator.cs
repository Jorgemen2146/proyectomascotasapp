namespace DogPlatform.Identity.Application.Security;

public interface IRefreshTokenGenerator
{
    RefreshTokenResult Generate(DateTime utcNow);
}

using DogPlatform.Identity.Domain.Aggregates.User;

namespace DogPlatform.Identity.Application.Security;

public interface IJwtTokenGenerator
{
    JwtTokenResult GenerateAccessToken(User user);
}

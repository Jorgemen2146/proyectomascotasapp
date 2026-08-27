using System.Security.Claims;
using DogPlatform.Pets.Application.Security;
using Microsoft.AspNetCore.Http;

namespace DogPlatform.Pets.Infrastructure.Security;

public sealed class CurrentUserService : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (Guid.TryParse(user?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return userId;
            return Guid.TryParse(user?.FindFirstValue("sub"), out userId)
                ? userId
                : Guid.Empty;
        }
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}

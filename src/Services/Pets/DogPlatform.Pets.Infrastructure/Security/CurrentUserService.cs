using System.Security.Claims;
using DogPlatform.Pets.Application.Security;

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
            var claim = _httpContextAccessor.HttpContext?.User.FindFirst("sub");
            return Guid.TryParse(claim?.Value, out var userId) ? userId : Guid.Empty;
        }
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}

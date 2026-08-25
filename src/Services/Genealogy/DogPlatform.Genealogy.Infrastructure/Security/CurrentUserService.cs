using System.Security.Claims;
using DogPlatform.Genealogy.Application.Security;
using Microsoft.AspNetCore.Http;

namespace DogPlatform.Genealogy.Infrastructure.Security;

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
            var claim = user?.FindFirst("sub") ?? user?.FindFirst(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim?.Value, out var userId) ? userId : Guid.Empty;
        }
    }

    public string Email =>
        _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value ??
        _httpContextAccessor.HttpContext?.User.FindFirst("email")?.Value ?? string.Empty;

    public string DisplayName
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var first = user?.FindFirst("given_name")?.Value ??
                        user?.FindFirst(ClaimTypes.GivenName)?.Value;
            var last = user?.FindFirst("family_name")?.Value ??
                       user?.FindFirst(ClaimTypes.Surname)?.Value;
            return string.Join(' ', new[] { first, last }.Where(value => !string.IsNullOrWhiteSpace(value)))
                is { Length: > 0 } name ? name : Email;
        }
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}

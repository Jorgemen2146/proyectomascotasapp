using DogPlatform.Notification.Application;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace DogPlatform.Notification.Infrastructure;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid UserId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            var claim = user?.FindFirst("sub") ?? user?.FindFirst(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim?.Value, out var userId) ? userId : Guid.Empty;
        }
    }
}

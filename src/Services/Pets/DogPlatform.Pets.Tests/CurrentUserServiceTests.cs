using System.Security.Claims;
using DogPlatform.Pets.Infrastructure.Security;
using Microsoft.AspNetCore.Http;

namespace DogPlatform.Pets.Tests;

public sealed class CurrentUserServiceTests
{
    [Fact]
    public void UserId_UsesNameIdentifierProducedByDefaultJwtMapping()
    {
        var userId = Guid.NewGuid();
        var currentUser = Create(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));

        Assert.Equal(userId, currentUser.UserId);
    }

    [Fact]
    public void UserId_UsesSubWhenInboundClaimMappingIsDisabled()
    {
        var userId = Guid.NewGuid();
        var currentUser = Create(new Claim("sub", userId.ToString()));

        Assert.Equal(userId, currentUser.UserId);
    }

    [Fact]
    public void UserId_FallsBackToValidSubWhenNameIdentifierIsInvalid()
    {
        var userId = Guid.NewGuid();
        var currentUser = Create(
            new Claim(ClaimTypes.NameIdentifier, "not-a-guid"),
            new Claim("sub", userId.ToString()));

        Assert.Equal(userId, currentUser.UserId);
    }

    [Fact]
    public void UserId_MissingClaim_ReturnsEmptyForControlledApplicationRejection()
    {
        var currentUser = Create();

        Assert.Equal(Guid.Empty, currentUser.UserId);
    }

    [Fact]
    public void UserId_InvalidClaim_ReturnsEmptyForControlledApplicationRejection()
    {
        var currentUser = Create(new Claim(ClaimTypes.NameIdentifier, "not-a-guid"));

        Assert.Equal(Guid.Empty, currentUser.UserId);
    }

    private static CurrentUserService Create(params Claim[] claims)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
        };
        return new CurrentUserService(new HttpContextAccessor { HttpContext = context });
    }
}

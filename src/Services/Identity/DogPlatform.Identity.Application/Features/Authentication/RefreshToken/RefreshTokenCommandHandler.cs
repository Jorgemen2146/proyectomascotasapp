using DogPlatform.Identity.Application.Security;
using DogPlatform.Identity.Domain.Aggregates.RefreshToken;
using DogPlatform.Identity.Domain.Errors;
using DogPlatform.Identity.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Identity.Application.Features.Authentication.RefreshToken;

internal sealed class RefreshTokenCommandHandler
    : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResponse>>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly TimeProvider _timeProvider;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        TimeProvider timeProvider)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _timeProvider = timeProvider;
    }

    public async Task<Result<RefreshTokenResponse>> Handle(
        RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        var existingToken = await _refreshTokenRepository
            .GetByTokenAsync(command.RefreshToken, cancellationToken);

        if (existingToken is null)
            return Result.Failure<RefreshTokenResponse>(RefreshTokenErrors.Invalid);

        if (!existingToken.IsActive(utcNow))
            return Result.Failure<RefreshTokenResponse>(RefreshTokenErrors.Invalid);

        var user = await _userRepository.GetByIdAsync(existingToken.UserId, cancellationToken);

        if (user is null || !user.IsActive)
            return Result.Failure<RefreshTokenResponse>(UserErrors.NotFound);

        existingToken.Revoke(utcNow);
        await _refreshTokenRepository.UpdateAsync(existingToken, cancellationToken);

        var jwtResult = _jwtTokenGenerator.GenerateAccessToken(user);

        var newRawToken = _refreshTokenGenerator.Generate();
        var newExpiresAt = utcNow.AddDays(_refreshTokenGenerator.RefreshTokenDays);

        var newRefreshToken = Domain.Aggregates.RefreshToken.RefreshToken.Create(
            Guid.NewGuid(),
            user.Id,
            newRawToken,
            newExpiresAt,
            utcNow);

        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new RefreshTokenResponse(
            jwtResult.AccessToken,
            jwtResult.ExpiresAtUtc,
            newRawToken,
            newExpiresAt));
    }
}

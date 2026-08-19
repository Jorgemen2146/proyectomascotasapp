using DogPlatform.Identity.Application.Security;
using DogPlatform.Identity.Domain.Aggregates.RefreshToken;
using DogPlatform.Identity.Domain.Errors;
using DogPlatform.Identity.Domain.Repositories;
using DogPlatform.Identity.Domain.ValueObjects;
using DogPlatform.SharedKernel.Primitives;
using MediatR;
using RefreshTokenAggregate = DogPlatform.Identity.Domain.Aggregates.RefreshToken.RefreshToken;

namespace DogPlatform.Identity.Application.Features.Authentication.Login;

internal sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private static readonly Error InvalidCredentials =
        Error.Unauthorized("Auth.InvalidCredentials", "The email or password is incorrect.");

    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly TimeProvider _timeProvider;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IIdentityUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        TimeProvider timeProvider)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _timeProvider = timeProvider;
    }

    public async Task<Result<LoginResponse>> Handle(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(command.Email);
        if (emailResult.IsFailure)
            return Result.Failure<LoginResponse>(InvalidCredentials);

        var user = await _userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);

        if (user is null)
            return Result.Failure<LoginResponse>(InvalidCredentials);

        if (!user.IsActive)
            return Result.Failure<LoginResponse>(InvalidCredentials);

        var passwordValid = _passwordHasher.Verify(
            command.Password,
            user.PasswordHash,
            user.PasswordSalt);

        if (!passwordValid)
            return Result.Failure<LoginResponse>(InvalidCredentials);

        if (!user.IsEmailConfirmed)
            return Result.Failure<LoginResponse>(UserErrors.EmailNotVerified);

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        var jwtResult = _jwtTokenGenerator.GenerateAccessToken(user);

        var refreshResult = _refreshTokenGenerator.Generate(utcNow);

        var refreshToken = RefreshTokenAggregate.Create(
            Guid.NewGuid(),
            user.Id,
            refreshResult.Token,
            refreshResult.ExpiresAtUtc,
            utcNow);

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        user.RecordLogin(utcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new LoginResponse(
            user.Id,
            user.FullName.FirstName,
            user.FullName.LastName,
            user.Email.Value,
            jwtResult.AccessToken,
            jwtResult.ExpiresAtUtc,
            refreshResult.Token,
            refreshResult.ExpiresAtUtc));
    }
}

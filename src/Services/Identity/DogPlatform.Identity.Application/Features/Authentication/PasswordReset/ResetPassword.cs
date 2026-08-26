using DogPlatform.Identity.Application.Security;
using DogPlatform.Identity.Domain.Errors;
using DogPlatform.Identity.Domain.Repositories;
using DogPlatform.Identity.Domain.ValueObjects;
using DogPlatform.SharedKernel.Primitives;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;

namespace DogPlatform.Identity.Application.Features.Authentication.PasswordReset;

public sealed record ResetPasswordCommand(
    string Email,
    string Code,
    string NewPassword,
    string ConfirmPassword) : IRequest<Result>;

public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().MaximumLength(200).EmailAddress()
            .WithErrorCode("PASSWORD_RESET_CODE_INVALID");
        RuleFor(x => x.Code)
            .NotEmpty().Matches("^[0-9]{6}$")
            .WithErrorCode("PASSWORD_RESET_CODE_INVALID");
        RuleFor(x => x.NewPassword)
            .ApplyDogPlatformPasswordPolicy()
            .WithErrorCode("PASSWORD_RESET_PASSWORD_INVALID");
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword)
            .WithMessage("Password confirmation must match the new password.")
            .WithErrorCode("PASSWORD_RESET_PASSWORD_INVALID");
    }
}

internal sealed class ResetPasswordCommandHandler(
    IUserRepository users,
    IPasswordResetCodeRepository resetCodes,
    IRefreshTokenRepository refreshTokens,
    IPasswordResetCodeService codeService,
    IPasswordHasher passwordHasher,
    IIdentityUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IOptions<PasswordResetOptions> options,
    IValidator<ResetPasswordCommand> validator)
    : IRequestHandler<ResetPasswordCommand, Result>
{
    public async Task<Result> Handle(
        ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure(validation.Errors.Any(error =>
                    error.PropertyName is nameof(ResetPasswordCommand.NewPassword)
                        or nameof(ResetPasswordCommand.ConfirmPassword))
                ? PasswordResetErrors.InvalidPassword
                : PasswordResetErrors.InvalidCode);
        }

        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
            return Result.Failure(PasswordResetErrors.InvalidCode);

        var user = await users.GetByEmailAsync(emailResult.Value, cancellationToken);
        if (user is null)
            return Result.Failure(PasswordResetErrors.InvalidCode);

        var resetCode = await resetCodes.GetLatestByUserIdAsync(user.Id, cancellationToken);
        if (resetCode is null || resetCode.UsedAtUtc.HasValue)
            return Result.Failure(PasswordResetErrors.InvalidCode);

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        if (resetCode.IsLocked(options.Value.MaxAttempts))
            return Result.Failure(PasswordResetErrors.LockedCode);

        if (resetCode.IsExpired(utcNow))
        {
            resetCode.Revoke();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure(PasswordResetErrors.ExpiredCode);
        }

        if (!codeService.Verify(request.Code, resetCode.CodeHash))
        {
            var locked = resetCode.RecordFailedAttempt(options.Value.MaxAttempts);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure(locked
                ? PasswordResetErrors.LockedCode
                : PasswordResetErrors.InvalidCode);
        }

        var hash = passwordHasher.Hash(request.NewPassword);
        user.ChangePassword(hash.Hash, hash.Salt, utcNow);
        resetCode.MarkUsed(utcNow);

        var otherCodes = await resetCodes.GetPendingByUserIdAsync(user.Id, cancellationToken);
        foreach (var otherCode in otherCodes.Where(code => code.Id != resetCode.Id))
            otherCode.Revoke();

        var activeRefreshTokens = await refreshTokens.GetActiveByUserIdAsync(
            user.Id, utcNow, cancellationToken);
        foreach (var refreshToken in activeRefreshTokens)
        {
            refreshToken.Revoke(utcNow);
            await refreshTokens.UpdateAsync(refreshToken, cancellationToken);
        }

        await users.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

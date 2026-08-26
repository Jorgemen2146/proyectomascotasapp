using DogPlatform.Identity.Application.Security;
using DogPlatform.Identity.Domain.Errors;
using DogPlatform.Identity.Domain.Repositories;
using DogPlatform.Identity.Domain.ValueObjects;
using DogPlatform.SharedKernel.Primitives;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;

namespace DogPlatform.Identity.Application.Features.Authentication.PasswordReset;

public sealed record VerifyResetCodeCommand(string Email, string Code)
    : IRequest<Result<VerifyResetCodeResponse>>;

public sealed record VerifyResetCodeResponse(bool Valid);

public sealed class VerifyResetCodeValidator : AbstractValidator<VerifyResetCodeCommand>
{
    public VerifyResetCodeValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(200).EmailAddress();
        RuleFor(x => x.Code).NotEmpty().Matches("^[0-9]{6}$");
    }
}

internal sealed class VerifyResetCodeCommandHandler(
    IUserRepository users,
    IPasswordResetCodeRepository resetCodes,
    IPasswordResetCodeService codeService,
    IIdentityUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IOptions<PasswordResetOptions> options,
    IValidator<VerifyResetCodeCommand> validator)
    : IRequestHandler<VerifyResetCodeCommand, Result<VerifyResetCodeResponse>>
{
    public async Task<Result<VerifyResetCodeResponse>> Handle(
        VerifyResetCodeCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure<VerifyResetCodeResponse>(PasswordResetErrors.InvalidCode);

        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
            return Result.Failure<VerifyResetCodeResponse>(PasswordResetErrors.InvalidCode);

        var user = await users.GetByEmailAsync(emailResult.Value, cancellationToken);
        if (user is null)
            return Result.Failure<VerifyResetCodeResponse>(PasswordResetErrors.InvalidCode);

        var resetCode = await resetCodes.GetLatestByUserIdAsync(user.Id, cancellationToken);
        if (resetCode is null || resetCode.UsedAtUtc.HasValue)
            return Result.Failure<VerifyResetCodeResponse>(PasswordResetErrors.InvalidCode);

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        if (resetCode.IsLocked(options.Value.MaxAttempts))
            return Result.Failure<VerifyResetCodeResponse>(PasswordResetErrors.LockedCode);

        if (resetCode.IsExpired(utcNow))
        {
            resetCode.Revoke();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<VerifyResetCodeResponse>(PasswordResetErrors.ExpiredCode);
        }

        if (!codeService.Verify(request.Code, resetCode.CodeHash))
        {
            var locked = resetCode.RecordFailedAttempt(options.Value.MaxAttempts);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<VerifyResetCodeResponse>(locked
                ? PasswordResetErrors.LockedCode
                : PasswordResetErrors.InvalidCode);
        }

        return Result.Success(new VerifyResetCodeResponse(true));
    }
}

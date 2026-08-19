using DogPlatform.Identity.Application.Security;
using DogPlatform.Identity.Domain.Aggregates.User;
using DogPlatform.Identity.Domain.Errors;
using DogPlatform.Identity.Domain.Repositories;
using DogPlatform.Identity.Domain.ValueObjects;
using DogPlatform.SharedKernel.Primitives;
using FluentValidation;
using MediatR;

namespace DogPlatform.Identity.Application.Features.Authentication.VerifyEmail;

internal sealed class VerifyEmailCommandHandler
    : IRequestHandler<VerifyEmailCommand, Result<VerifyEmailResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IEmailVerificationCodeService _verificationCodeService;
    private readonly TimeProvider _timeProvider;
    private readonly IValidator<VerifyEmailCommand> _validator;

    public VerifyEmailCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        IEmailVerificationCodeService verificationCodeService,
        TimeProvider timeProvider,
        IValidator<VerifyEmailCommand> validator)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _verificationCodeService = verificationCodeService;
        _timeProvider = timeProvider;
        _validator = validator;
    }

    public async Task<Result<VerifyEmailResponse>> Handle(
        VerifyEmailCommand request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<VerifyEmailResponse>(Error.Validation(
                "VerifyEmail.Validation",
                string.Join(" ", validationResult.Errors.Select(error => error.ErrorMessage))));
        }

        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
            return Result.Failure<VerifyEmailResponse>(emailResult.Error);

        var user = await _userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);
        if (user is null)
            return Result.Failure<VerifyEmailResponse>(UserErrors.EmailVerificationCodeInvalid);

        if (user.IsEmailConfirmed)
            return Result.Failure<VerifyEmailResponse>(UserErrors.EmailAlreadyConfirmed);

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        if (user.EmailVerificationAttempts >= User.MaximumEmailVerificationAttempts)
        {
            user.InvalidateEmailVerificationCode(utcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<VerifyEmailResponse>(UserErrors.EmailVerificationAttemptsExceeded);
        }

        if (user.EmailVerificationCodeHash is null ||
            user.EmailVerificationCodeExpiresAt is null)
        {
            return Result.Failure<VerifyEmailResponse>(UserErrors.EmailVerificationCodeUnavailable);
        }

        if (user.EmailVerificationCodeExpiresAt <= utcNow)
        {
            user.InvalidateEmailVerificationCode(utcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<VerifyEmailResponse>(UserErrors.EmailVerificationCodeExpired);
        }

        if (!_verificationCodeService.Verify(request.Code, user.EmailVerificationCodeHash))
        {
            var attemptsExceeded = user.RecordFailedEmailVerificationAttempt(utcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var error = attemptsExceeded
                ? UserErrors.EmailVerificationAttemptsExceeded
                : UserErrors.EmailVerificationCodeInvalid;

            return Result.Failure<VerifyEmailResponse>(error);
        }

        var confirmResult = user.ConfirmEmail(utcNow);
        if (confirmResult.IsFailure)
            return Result.Failure<VerifyEmailResponse>(confirmResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new VerifyEmailResponse(
            user.Email.Value,
            user.IsEmailConfirmed,
            user.EmailConfirmedAt!.Value));
    }
}

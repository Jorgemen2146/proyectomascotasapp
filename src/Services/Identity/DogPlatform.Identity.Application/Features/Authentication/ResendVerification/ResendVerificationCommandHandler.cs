using DogPlatform.Identity.Application.Communication;
using DogPlatform.Identity.Application.Security;
using DogPlatform.Identity.Domain.Repositories;
using DogPlatform.Identity.Domain.ValueObjects;
using DogPlatform.SharedKernel.Primitives;
using FluentValidation;
using MediatR;

namespace DogPlatform.Identity.Application.Features.Authentication.ResendVerification;

internal sealed class ResendVerificationCommandHandler
    : IRequestHandler<ResendVerificationCommand, Result<ResendVerificationResponse>>
{
    private const string GenericMessage =
        "If the account is eligible, a new verification code has been sent.";

    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IEmailVerificationCodeService _verificationCodeService;
    private readonly IEmailSender _emailSender;
    private readonly TimeProvider _timeProvider;
    private readonly IValidator<ResendVerificationCommand> _validator;

    public ResendVerificationCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        IEmailVerificationCodeService verificationCodeService,
        IEmailSender emailSender,
        TimeProvider timeProvider,
        IValidator<ResendVerificationCommand> validator)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _verificationCodeService = verificationCodeService;
        _emailSender = emailSender;
        _timeProvider = timeProvider;
        _validator = validator;
    }

    public async Task<Result<ResendVerificationResponse>> Handle(
        ResendVerificationCommand request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<ResendVerificationResponse>(Error.Validation(
                "ResendVerification.Validation",
                string.Join(" ", validationResult.Errors.Select(error => error.ErrorMessage))));
        }

        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
            return Result.Failure<ResendVerificationResponse>(emailResult.Error);

        var user = await _userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);
        if (user is null || user.IsEmailConfirmed)
            return Result.Success(new ResendVerificationResponse(GenericMessage));

        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        if (user.EmailVerificationLastSentAt is not null &&
            utcNow < user.EmailVerificationLastSentAt.Value.Add(EmailVerificationPolicy.ResendCooldown))
        {
            return Result.Success(new ResendVerificationResponse(GenericMessage));
        }

        var verificationCode = _verificationCodeService.Generate();
        var issueResult = user.IssueEmailVerificationCode(
            verificationCode.Hash,
            utcNow.Add(EmailVerificationPolicy.CodeLifetime),
            utcNow);

        if (issueResult.IsFailure)
            return Result.Success(new ResendVerificationResponse(GenericMessage));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _emailSender.SendVerificationCodeAsync(
            user.Email.Value,
            verificationCode.Code,
            cancellationToken);

        return Result.Success(new ResendVerificationResponse(GenericMessage));
    }
}

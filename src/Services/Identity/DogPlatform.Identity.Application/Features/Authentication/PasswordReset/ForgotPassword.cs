using DogPlatform.Identity.Application.Communication;
using DogPlatform.Identity.Application.Security;
using DogPlatform.Identity.Domain.Aggregates.PasswordResetCode;
using DogPlatform.Identity.Domain.Repositories;
using DogPlatform.Identity.Domain.ValueObjects;
using DogPlatform.SharedKernel.Primitives;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DogPlatform.Identity.Application.Features.Authentication.PasswordReset;

public sealed record ForgotPasswordCommand(string Email, string? CreatedFromIp = null)
    : IRequest<Result<ForgotPasswordResponse>>;

public sealed record ForgotPasswordResponse(string Message);

public sealed class ForgotPasswordValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordValidator() => RuleFor(x => x.Email)
        .NotEmpty().MaximumLength(200).EmailAddress();
}

internal sealed class ForgotPasswordCommandHandler(
    IUserRepository users,
    IPasswordResetCodeRepository resetCodes,
    IPasswordResetCodeService codeService,
    IEmailSender emailSender,
    IIdentityUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IOptions<PasswordResetOptions> options,
    IValidator<ForgotPasswordCommand> validator,
    ILogger<ForgotPasswordCommandHandler> logger)
    : IRequestHandler<ForgotPasswordCommand, Result<ForgotPasswordResponse>>
{
    public const string GenericMessage =
        "Si el correo está registrado, te enviaremos un código.";

    public async Task<Result<ForgotPasswordResponse>> Handle(
        ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure<ForgotPasswordResponse>(Error.Validation(
                "ForgotPassword.Validation",
                string.Join(" ", validation.Errors.Select(error => error.ErrorMessage))));

        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
            return Result.Success(new ForgotPasswordResponse(GenericMessage));

        var user = await users.GetByEmailAsync(emailResult.Value, cancellationToken);
        if (user is null)
            return Result.Success(new ForgotPasswordResponse(GenericMessage));

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var previousCodes = await resetCodes.GetPendingByUserIdAsync(user.Id, cancellationToken);
        var latest = previousCodes.OrderByDescending(code => code.CreatedAtUtc).FirstOrDefault();
        if (latest is not null &&
            utcNow < latest.CreatedAtUtc.AddSeconds(options.Value.ResendCooldownSeconds))
        {
            return Result.Success(new ForgotPasswordResponse(GenericMessage));
        }

        foreach (var previousCode in previousCodes)
            previousCode.Revoke();

        var generated = codeService.Generate();
        var resetCode = PasswordResetCode.Create(
            Guid.NewGuid(),
            user.Id,
            generated.Hash,
            utcNow,
            utcNow.AddMinutes(options.Value.CodeExpirationMinutes),
            request.CreatedFromIp);

        await resetCodes.AddAsync(resetCode, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await emailSender.SendPasswordResetCodeAsync(
                user.Email.Value,
                generated.Code,
                options.Value.CodeExpirationMinutes,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception,
                "Password reset email delivery failed for UserId={UserId}.", user.Id);
        }

        return Result.Success(new ForgotPasswordResponse(GenericMessage));
    }
}

using DogPlatform.Identity.Application.Communication;
using DogPlatform.Identity.Application.Features.Authentication;
using DogPlatform.Identity.Application.Security;
using DogPlatform.Identity.Domain.Aggregates.User;
using DogPlatform.Identity.Domain.Errors;
using DogPlatform.Identity.Domain.Repositories;
using DogPlatform.Identity.Domain.ValueObjects;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Identity.Application.Features.Authentication.Register;

internal sealed class RegisterUserCommandHandler
    : IRequestHandler<RegisterUserCommand, Result<RegisterUserResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailVerificationCodeService _verificationCodeService;
    private readonly IEmailSender _emailSender;
    private readonly TimeProvider _timeProvider;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IEmailVerificationCodeService verificationCodeService,
        IEmailSender emailSender,
        TimeProvider timeProvider)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _verificationCodeService = verificationCodeService;
        _emailSender = emailSender;
        _timeProvider = timeProvider;
    }

    public async Task<Result<RegisterUserResponse>> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Create Email ValueObject
        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
            return Result.Failure<RegisterUserResponse>(emailResult.Error);

        var email = emailResult.Value;

        // 2. Create FullName ValueObject
        var fullNameResult = FullName.Create(request.FirstName, request.LastName);
        if (fullNameResult.IsFailure)
            return Result.Failure<RegisterUserResponse>(fullNameResult.Error);

        var fullName = fullNameResult.Value;

        // 3. Check email uniqueness
        var emailExists = await _userRepository.ExistsWithEmailAsync(email, cancellationToken);
        if (emailExists)
            return Result.Failure<RegisterUserResponse>(UserErrors.EmailAlreadyExists);

        // 4. Hash password
        var hashResult = _passwordHasher.Hash(request.Password);

        // 5. Generate Guid and obtain UTC time
        var userId = Guid.NewGuid();
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        // 6. Create User aggregate
        var user = User.Register(
            userId,
            fullName,
            email,
            hashResult.Hash,
            hashResult.Salt,
            utcNow);

        // 7. Apply optional PhoneNumber
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            user.UpdateProfile(request.PhoneNumber, null, utcNow);

        var verificationCode = _verificationCodeService.Generate();
        var issueResult = user.IssueEmailVerificationCode(
            verificationCode.Hash,
            utcNow.Add(EmailVerificationPolicy.CodeLifetime),
            utcNow);

        if (issueResult.IsFailure)
            return Result.Failure<RegisterUserResponse>(issueResult.Error);

        // 8. Persist the account and code hash before sending the email.
        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _emailSender.SendVerificationCodeAsync(
            email.Value,
            verificationCode.Code,
            cancellationToken);

        // 9. Return safe response
        return Result.Success(new RegisterUserResponse(
            user.Id,
            fullName.FirstName,
            fullName.LastName,
            email.Value,
            user.PhoneNumber,
            user.IsEmailConfirmed,
            user.CreatedAt));
    }
}

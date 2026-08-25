using DogPlatform.Identity.Application.Communication;
using DogPlatform.Identity.Application.Features.Authentication;
using DogPlatform.Identity.Application.Features.Legal;
using DogPlatform.Identity.Application.Security;
using DogPlatform.Identity.Domain.Aggregates.User;
using DogPlatform.Identity.Domain.Errors;
using DogPlatform.Identity.Domain.Legal;
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
    private readonly ILegalDocumentRepository _legalDocumentRepository;
    private readonly IUserLegalConsentRepository _legalConsentRepository;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IEmailVerificationCodeService verificationCodeService,
        IEmailSender emailSender,
        TimeProvider timeProvider,
        ILegalDocumentRepository legalDocumentRepository,
        IUserLegalConsentRepository legalConsentRepository)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _verificationCodeService = verificationCodeService;
        _emailSender = emailSender;
        _timeProvider = timeProvider;
        _legalDocumentRepository = legalDocumentRepository;
        _legalConsentRepository = legalConsentRepository;
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

        // Legal validation happens before the user is tracked. Every active,
        // required document must be explicitly accepted at its exact version.
        var requiredDocuments = await _legalDocumentRepository
            .GetActiveRequiredAsync(cancellationToken);
        var providedConsents = request.LegalConsents ?? [];

        foreach (var document in requiredDocuments)
        {
            var sameType = providedConsents.Where(consent =>
                string.Equals(consent.Type, document.Type.ToString(),
                    StringComparison.OrdinalIgnoreCase)).ToList();

            if (sameType.Count == 0)
                return Result.Failure<RegisterUserResponse>(LegalErrors.ConsentRequired);

            if (!sameType.Any(consent => string.Equals(consent.Version, document.Version,
                    StringComparison.OrdinalIgnoreCase)))
                return Result.Failure<RegisterUserResponse>(LegalErrors.DocumentVersionInvalid);
        }

        var validPairs = requiredDocuments
            .Select(document => $"{document.Type}:{document.Version}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var providedPairs = providedConsents
            .Select(consent => $"{consent.Type?.Trim()}:{consent.Version?.Trim()}")
            .ToList();

        if (providedPairs.Count != providedPairs.Distinct(StringComparer.OrdinalIgnoreCase).Count()
            || providedPairs.Any(pair => !validPairs.Contains(pair)))
            return Result.Failure<RegisterUserResponse>(LegalErrors.DocumentVersionInvalid);

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
        await _legalConsentRepository.AddRangeAsync(requiredDocuments.Select(document =>
            UserLegalConsent.Accept(Guid.NewGuid(), user.Id, document.Id, utcNow)), cancellationToken);
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

using DogPlatform.Identity.Application.Security;
using DogPlatform.Identity.Domain.Aggregates.User;
using DogPlatform.Identity.Domain.Errors;
using DogPlatform.Identity.Domain.Repositories;
using DogPlatform.Identity.Domain.ValueObjects;
using DogPlatform.SharedKernel.Primitives;
using FluentValidation;
using MediatR;

namespace DogPlatform.Identity.Application.Features.Authentication.Register;

internal sealed class RegisterUserHandler : IRequestHandler<RegisterUserCommand, RegisterUserResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserHandler(
        IUserRepository userRepository,
        IIdentityUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<RegisterUserResponse> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Create Email Value Object
        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
        {
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure(
                    "Email",
                    emailResult.Error.Description)
            });
        }

        var email = emailResult.Value;

        // 2. Verify email does not already exist
        var emailExists = await _userRepository.ExistsWithEmailAsync(email, cancellationToken);
        if (emailExists)
        {
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure(
                    "Email",
                    UserErrors.EmailAlreadyExists.Description)
            });
        }

        // 3. Create FullName Value Object
        var fullNameResult = FullName.Create(request.FirstName, request.LastName);
        if (fullNameResult.IsFailure)
        {
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure(
                    "FullName",
                    fullNameResult.Error.Description)
            });
        }

        var fullName = fullNameResult.Value;

        // 4. Hash password using IPasswordHasher
        var (passwordHash, passwordSalt) = _passwordHasher.HashPassword(request.Password);

        // 5. Create User aggregate using User.Register()
        var utcNow = DateTime.UtcNow;
        var userId = Guid.NewGuid();

        var user = User.Register(
            userId,
            fullName,
            email,
            passwordHash,
            passwordSalt,
            utcNow);

        // Optionally set phone number if provided
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            user.UpdateProfile(request.PhoneNumber, null, utcNow);
        }

        // 6. Add user using IUserRepository
        await _userRepository.AddAsync(user, cancellationToken);

        // 7. Save changes using IIdentityUnitOfWork
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 8. Return RegisterUserResponse
        return new RegisterUserResponse(
            user.Id,
            email.Value,
            fullName.Display,
            user.IsEmailConfirmed);
    }
}

public class ValidationException : Exception
{
    public ValidationException(IEnumerable<FluentValidation.Results.ValidationFailure> failures)
        : base("Validation failed.")
    {
        Failures = failures.ToDictionary(x => x.PropertyName, x => new[] { x.ErrorMessage });
    }

    public IDictionary<string, string[]> Failures { get; }
}

using DogPlatform.Identity.Domain.DomainEvents;
using DogPlatform.Identity.Domain.Errors;
using DogPlatform.Identity.Domain.ValueObjects;
using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Identity.Domain.Aggregates.User;

public sealed class User : AggregateRoot<Guid>
{
    public const int MaximumEmailVerificationAttempts = 5;

    private readonly List<UserRole> _userRoles = [];

    private User(
        Guid id,
        FullName fullName,
        Email email,
        string passwordHash,
        string passwordSalt,
        DateTime createdAt)
        : base(id)
    {
        FullName = fullName;
        Email = email;
        PasswordHash = passwordHash;
        PasswordSalt = passwordSalt;
        IsEmailConfirmed = false;
        EmailVerificationAttempts = 0;
        IsActive = true;
        CreatedAt = createdAt;
    }

    // Required for ORM hydration.
    private User() { }

    public FullName FullName { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = string.Empty;
    public string PasswordSalt { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public string? ProfilePhotoUrl { get; private set; }
    public bool IsEmailConfirmed { get; private set; }
    public DateTime? EmailConfirmedAt { get; private set; }
    public string? EmailVerificationCodeHash { get; private set; }
    public DateTime? EmailVerificationCodeExpiresAt { get; private set; }
    public int EmailVerificationAttempts { get; private set; }
    public DateTime? EmailVerificationLastSentAt { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? LastLogin { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    // ── Factory ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new user and raises <see cref="UserRegisteredDomainEvent"/>.
    /// PasswordHash and PasswordSalt must be produced by the infrastructure
    /// hashing service before calling this method.
    /// </summary>
    public static User Register(
        Guid id,
        FullName fullName,
        Email email,
        string passwordHash,
        string passwordSalt,
        DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordSalt);

        var user = new User(id, fullName, email, passwordHash, passwordSalt, utcNow);

        user.Raise(new UserRegisteredDomainEvent(
            Guid.NewGuid(),
            utcNow,
            id,
            email.Value,
            fullName.Display));

        return user;
    }

    // ── Behavior ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Confirms the user's email address.
    /// Returns failure if the email is already confirmed.
    /// </summary>
    public Result ConfirmEmail(DateTime utcNow)
    {
        if (IsEmailConfirmed)
            return Result.Failure(UserErrors.EmailAlreadyConfirmed);

        IsEmailConfirmed = true;
        EmailConfirmedAt = utcNow;
        EmailVerificationCodeHash = null;
        EmailVerificationCodeExpiresAt = null;
        EmailVerificationAttempts = 0;
        UpdatedAt = utcNow;

        Raise(new UserEmailConfirmedDomainEvent(
            Guid.NewGuid(),
            utcNow,
            Id,
            Email.Value));

        return Result.Success();
    }

    public Result IssueEmailVerificationCode(
        string codeHash,
        DateTime expiresAtUtc,
        DateTime sentAtUtc)
    {
        if (IsEmailConfirmed)
            return Result.Failure(UserErrors.EmailAlreadyConfirmed);

        ArgumentException.ThrowIfNullOrWhiteSpace(codeHash);

        if (expiresAtUtc <= sentAtUtc)
            throw new ArgumentOutOfRangeException(nameof(expiresAtUtc));

        EmailVerificationCodeHash = codeHash;
        EmailVerificationCodeExpiresAt = expiresAtUtc;
        EmailVerificationAttempts = 0;
        EmailVerificationLastSentAt = sentAtUtc;
        UpdatedAt = sentAtUtc;

        return Result.Success();
    }

    public bool RecordFailedEmailVerificationAttempt(DateTime utcNow)
    {
        if (EmailVerificationCodeHash is null)
            return true;

        EmailVerificationAttempts++;
        UpdatedAt = utcNow;

        if (EmailVerificationAttempts < MaximumEmailVerificationAttempts)
            return false;

        EmailVerificationCodeHash = null;
        EmailVerificationCodeExpiresAt = null;
        return true;
    }

    public void InvalidateEmailVerificationCode(DateTime utcNow)
    {
        EmailVerificationCodeHash = null;
        EmailVerificationCodeExpiresAt = null;
        UpdatedAt = utcNow;
    }

    /// <summary>
    /// Deactivates the user account.
    /// An inactive user cannot authenticate.
    /// </summary>
    public Result Deactivate(DateTime utcNow)
    {
        if (!IsActive)
            return Result.Failure(UserErrors.AlreadyInactive);

        IsActive = false;
        UpdatedAt = utcNow;

        Raise(new UserDeactivatedDomainEvent(
            Guid.NewGuid(),
            utcNow,
            Id));

        return Result.Success();
    }

    /// <summary>
    /// Reactivates a previously deactivated user account.
    /// </summary>
    public Result Activate(DateTime utcNow)
    {
        if (IsActive)
            return Result.Failure(
                Error.Conflict("User.AlreadyActive", "The user account is already active."));

        IsActive = true;
        UpdatedAt = utcNow;

        return Result.Success();
    }

    /// <summary>
    /// Updates the user's password credentials and raises <see cref="PasswordChangedDomainEvent"/>.
    /// PasswordHash and PasswordSalt must be produced by the infrastructure hashing service.
    /// </summary>
    public Result ChangePassword(string newPasswordHash, string newPasswordSalt, DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newPasswordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(newPasswordSalt);

        PasswordHash = newPasswordHash;
        PasswordSalt = newPasswordSalt;
        UpdatedAt = utcNow;

        Raise(new PasswordChangedDomainEvent(
            Guid.NewGuid(),
            utcNow,
            Id));

        return Result.Success();
    }

    /// <summary>
    /// Assigns a role to the user by its identifier.
    /// Returns failure if the role is already assigned.
    /// </summary>
    public Result AssignRole(Guid roleId, Guid userRoleId, DateTime utcNow)
    {
        if (_userRoles.Any(ur => ur.RoleId == roleId))
            return Result.Failure(UserErrors.RoleAlreadyAssigned);

        _userRoles.Add(new UserRole(userRoleId, Id, roleId, utcNow));
        return Result.Success();
    }

    /// <summary>
    /// Removes a role assignment from the user.
    /// Returns failure if the role was not assigned.
    /// </summary>
    public Result RevokeRole(Guid roleId)
    {
        var userRole = _userRoles.FirstOrDefault(ur => ur.RoleId == roleId);

        if (userRole is null)
            return Result.Failure(UserErrors.RoleNotAssigned);

        _userRoles.Remove(userRole);
        return Result.Success();
    }

    /// <summary>
    /// Updates optional profile fields. Pass null to clear a field.
    /// </summary>
    public void UpdateProfile(string? phoneNumber, string? profilePhotoUrl, DateTime utcNow)
    {
        PhoneNumber = phoneNumber?.Trim();
        ProfilePhotoUrl = profilePhotoUrl?.Trim();
        UpdatedAt = utcNow;
    }

    public Result UpdateProfile(
        string firstName,
        string lastName,
        string? phoneNumber,
        DateTime utcNow)
    {
        var fullNameResult = FullName.Create(firstName, lastName);
        if (fullNameResult.IsFailure)
            return Result.Failure(fullNameResult.Error);

        FullName = fullNameResult.Value;
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        UpdatedAt = utcNow;

        return Result.Success();
    }

    public void SetProfilePhotoUrl(string profilePhotoUrl, DateTime utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profilePhotoUrl);
        ProfilePhotoUrl = profilePhotoUrl.Trim();
        UpdatedAt = utcNow;
    }

    /// <summary>
    /// Records the timestamp of a successful login.
    /// </summary>
    public void RecordLogin(DateTime utcNow)
    {
        LastLogin = utcNow;
        UpdatedAt = utcNow;
    }
}

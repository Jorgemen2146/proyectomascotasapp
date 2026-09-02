using DogPlatform.Identity.Application.Communication;
using DogPlatform.Identity.Application.Features.Authentication.Login;
using DogPlatform.Identity.Application.Features.Authentication;
using DogPlatform.Identity.Application.Features.Legal;
using DogPlatform.Identity.Application.Security;
using DogPlatform.Identity.Domain.Aggregates.ExternalLogin;
using DogPlatform.Identity.Domain.Aggregates.User;
using DogPlatform.Identity.Domain.Errors;
using DogPlatform.Identity.Domain.Legal;
using DogPlatform.Identity.Domain.Repositories;
using DogPlatform.Identity.Domain.ValueObjects;
using DogPlatform.SharedKernel.Primitives;
using MediatR;
using RefreshTokenAggregate = DogPlatform.Identity.Domain.Aggregates.RefreshToken.RefreshToken;

namespace DogPlatform.Identity.Application.Features.Authentication.External;

internal static class ExternalAuthSupport
{
    internal static Error MapFailure(ExternalValidationFailure failure) => failure switch
    {
        ExternalValidationFailure.ExpiredToken => ExternalAuthErrors.ExpiredToken,
        ExternalValidationFailure.ProviderNotConfigured => ExternalAuthErrors.ProviderNotConfigured,
        ExternalValidationFailure.ProviderUnavailable => ExternalAuthErrors.ProviderUnavailable,
        _ => ExternalAuthErrors.InvalidToken
    };

    internal static async Task<Result<IReadOnlyCollection<LegalDocument>>> ValidateConsentsAsync(
        ILegalDocumentRepository documents,
        IReadOnlyList<LegalConsentSelection>? selections,
        CancellationToken cancellationToken)
    {
        var required = await documents.GetActiveRequiredAsync(cancellationToken);
        var supplied = selections ?? [];
        foreach (var document in required)
        {
            if (!supplied.Any(item =>
                    string.Equals(item.Type, document.Type.ToString(), StringComparison.OrdinalIgnoreCase)
                    && string.Equals(item.Version, document.Version, StringComparison.OrdinalIgnoreCase)))
                return Result.Failure<IReadOnlyCollection<LegalDocument>>(LegalErrors.ConsentRequired);
        }

        var valid = required.Select(x => $"{x.Type}:{x.Version}").ToHashSet(StringComparer.OrdinalIgnoreCase);
        var provided = supplied.Select(x => $"{x.Type?.Trim()}:{x.Version?.Trim()}").ToArray();
        if (provided.Length != provided.Distinct(StringComparer.OrdinalIgnoreCase).Count()
            || provided.Any(x => !valid.Contains(x)))
            return Result.Failure<IReadOnlyCollection<LegalDocument>>(LegalErrors.DocumentVersionInvalid);

        return Result.Success<IReadOnlyCollection<LegalDocument>>(required);
    }

    internal static async Task<LoginResponse> IssueSessionAsync(User user, DateTime utcNow,
        IJwtTokenGenerator jwtTokens, IRefreshTokenGenerator refreshTokens,
        IRefreshTokenRepository refreshTokenRepository,
        CancellationToken cancellationToken)
    {
        var jwt = jwtTokens.GenerateAccessToken(user);
        var refresh = refreshTokens.Generate(utcNow);
        await refreshTokenRepository.AddAsync(RefreshTokenAggregate.Create(
            Guid.NewGuid(), user.Id, refresh.Token, refresh.ExpiresAtUtc, utcNow), cancellationToken);
        user.RecordLogin(utcNow);
        return new LoginResponse(user.Id, user.FullName.FirstName, user.FullName.LastName,
            user.Email.Value, jwt.AccessToken, jwt.ExpiresAtUtc, refresh.Token, refresh.ExpiresAtUtc);
    }
}

internal sealed class ExternalAuthCommandHandler(
    IExternalIdentityValidator validator,
    IExternalRegistrationTicketService tickets,
    IExternalLoginRepository externalLogins,
    IUserRepository users,
    IRefreshTokenRepository refreshTokenRepository,
    ILegalDocumentRepository legalDocuments,
    IUserLegalConsentRepository legalConsents,
    IJwtTokenGenerator jwtTokens,
    IRefreshTokenGenerator refreshTokens,
    IIdentityUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<ExternalAuthCommand, Result<ExternalAuthOutcome>>
{
    public async Task<Result<ExternalAuthOutcome>> Handle(
        ExternalAuthCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Credential) || request.Credential.Length > 16_384
            || request.Provider == ExternalAuthProvider.Apple
            && (string.IsNullOrWhiteSpace(request.Nonce) || request.Nonce.Length > 512))
            return Result.Failure<ExternalAuthOutcome>(ExternalAuthErrors.InvalidRequest);

        var validation = await validator.ValidateAsync(
            request.Provider, request.Credential, request.Nonce, cancellationToken);
        if (!validation.IsSuccess)
            return Result.Failure<ExternalAuthOutcome>(ExternalAuthSupport.MapFailure(validation.Failure));

        var identity = validation.Identity!;
        var existingLogin = await externalLogins.GetAsync(
            identity.Provider, identity.ProviderUserId, cancellationToken);
        if (existingLogin is not null)
        {
            var existingUser = await users.GetByIdAsync(existingLogin.UserId, cancellationToken);
            if (existingUser is null || !existingUser.IsActive)
                return Result.Failure<ExternalAuthOutcome>(UserErrors.Inactive);
            if (!existingUser.IsEmailConfirmed)
                return Result.Failure<ExternalAuthOutcome>(UserErrors.EmailNotVerified);

            var now = timeProvider.GetUtcNow().UtcDateTime;
            var session = await ExternalAuthSupport.IssueSessionAsync(existingUser, now,
                jwtTokens, refreshTokens, refreshTokenRepository, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(ExternalAuthOutcome.Authenticated(session));
        }

        var normalizedEmail = Email.Create(identity.Email);
        if (normalizedEmail.IsSuccess
            && await users.ExistsWithEmailAsync(normalizedEmail.Value, cancellationToken))
            return Result.Failure<ExternalAuthOutcome>(ExternalAuthErrors.AccountLinkRequired);

        var missing = new List<string>();
        if (normalizedEmail.IsFailure) missing.Add("email");
        if (string.IsNullOrWhiteSpace(identity.FirstName)) missing.Add("firstName");
        if (string.IsNullOrWhiteSpace(identity.LastName)) missing.Add("lastName");
        if (!identity.EmailVerified) missing.Add("emailVerification");
        if (missing.Count > 0)
        {
            string ticket;
            try
            {
                ticket = tickets.Issue(identity, timeProvider.GetUtcNow().UtcDateTime);
            }
            catch (InvalidOperationException)
            {
                return Result.Failure<ExternalAuthOutcome>(ExternalAuthErrors.ProviderNotConfigured);
            }
            var code = normalizedEmail.IsFailure
                ? "EXTERNAL_EMAIL_REQUIRED"
                : "EXTERNAL_REGISTRATION_REQUIRED";
            return Result.Success(ExternalAuthOutcome.Action(code, ticket, missing.ToArray()));
        }

        var consentResult = await ExternalAuthSupport.ValidateConsentsAsync(
            legalDocuments, request.LegalConsents, cancellationToken);
        if (consentResult.IsFailure)
            return Result.Failure<ExternalAuthOutcome>(consentResult.Error);

        var fullName = FullName.Create(identity.FirstName, identity.LastName);
        if (fullName.IsFailure)
            return Result.Failure<ExternalAuthOutcome>(fullName.Error);

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var user = User.RegisterExternal(Guid.NewGuid(), fullName.Value, normalizedEmail.Value,
            true, utcNow);
        await users.AddAsync(user, cancellationToken);
        await externalLogins.AddAsync(ExternalLogin.Create(user.Id, identity.Provider,
            identity.ProviderUserId, normalizedEmail.Value.Value, utcNow), cancellationToken);
        await legalConsents.AddRangeAsync(consentResult.Value.Select(document =>
            UserLegalConsent.Accept(Guid.NewGuid(), user.Id, document.Id, utcNow)), cancellationToken);

        var createdSession = await ExternalAuthSupport.IssueSessionAsync(user, utcNow,
            jwtTokens, refreshTokens, refreshTokenRepository, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(ExternalAuthOutcome.Authenticated(createdSession));
    }
}

internal sealed class CompleteExternalRegistrationCommandHandler(
    IExternalRegistrationTicketService tickets,
    IExternalLoginRepository externalLogins,
    IUserRepository users,
    IRefreshTokenRepository refreshTokenRepository,
    ILegalDocumentRepository legalDocuments,
    IUserLegalConsentRepository legalConsents,
    IEmailVerificationCodeService verificationCodes,
    IEmailSender emailSender,
    IJwtTokenGenerator jwtTokens,
    IRefreshTokenGenerator refreshTokens,
    IIdentityUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<CompleteExternalRegistrationCommand, Result<ExternalAuthOutcome>>
{
    public async Task<Result<ExternalAuthOutcome>> Handle(
        CompleteExternalRegistrationCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RegistrationToken)
            || request.RegistrationToken.Length > 16_384)
            return Result.Failure<ExternalAuthOutcome>(ExternalAuthErrors.InvalidRequest);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var ticketValidation = tickets.Validate(request.RegistrationToken, now);
        if (!ticketValidation.IsSuccess)
            return Result.Failure<ExternalAuthOutcome>(ExternalAuthErrors.RegistrationTicketInvalid);

        var identity = ticketValidation.Identity!;
        if (await externalLogins.GetAsync(identity.Provider, identity.ProviderUserId, cancellationToken) is not null)
            return Result.Failure<ExternalAuthOutcome>(ExternalAuthErrors.LoginAlreadyLinked);

        var emailResult = Email.Create(identity.Email ?? request.Email);
        if (emailResult.IsFailure)
            return Result.Failure<ExternalAuthOutcome>(ExternalAuthErrors.EmailRequired);
        if (await users.ExistsWithEmailAsync(emailResult.Value, cancellationToken))
            return Result.Failure<ExternalAuthOutcome>(ExternalAuthErrors.AccountLinkRequired);

        var nameResult = FullName.Create(identity.FirstName ?? request.FirstName,
            identity.LastName ?? request.LastName);
        if (nameResult.IsFailure)
            return Result.Failure<ExternalAuthOutcome>(ExternalAuthErrors.RegistrationRequired);

        var consentResult = await ExternalAuthSupport.ValidateConsentsAsync(
            legalDocuments, request.LegalConsents, cancellationToken);
        if (consentResult.IsFailure)
            return Result.Failure<ExternalAuthOutcome>(consentResult.Error);

        var providerVerifiedEmail = identity.EmailVerified
            && string.Equals(identity.Email, emailResult.Value.Value, StringComparison.OrdinalIgnoreCase);
        var user = User.RegisterExternal(Guid.NewGuid(), nameResult.Value, emailResult.Value,
            providerVerifiedEmail, now);
        await users.AddAsync(user, cancellationToken);
        await externalLogins.AddAsync(ExternalLogin.Create(user.Id, identity.Provider,
            identity.ProviderUserId, emailResult.Value.Value, now), cancellationToken);
        await legalConsents.AddRangeAsync(consentResult.Value.Select(document =>
            UserLegalConsent.Accept(Guid.NewGuid(), user.Id, document.Id, now)), cancellationToken);

        string? verificationCode = null;
        if (!providerVerifiedEmail)
        {
            var generated = verificationCodes.Generate();
            var issue = user.IssueEmailVerificationCode(generated.Hash,
                now.Add(EmailVerificationPolicy.CodeLifetime), now);
            if (issue.IsFailure) return Result.Failure<ExternalAuthOutcome>(issue.Error);
            verificationCode = generated.Code;
        }

        LoginResponse? session = null;
        if (providerVerifiedEmail)
            session = await ExternalAuthSupport.IssueSessionAsync(user, now,
                jwtTokens, refreshTokens, refreshTokenRepository, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        if (verificationCode is not null)
        {
            await emailSender.SendVerificationCodeAsync(
                emailResult.Value.Value, verificationCode, cancellationToken);
            return Result.Success(ExternalAuthOutcome.Action(
                "EXTERNAL_EMAIL_VERIFICATION_REQUIRED", null, "emailVerification"));
        }

        return Result.Success(ExternalAuthOutcome.Authenticated(session!));
    }
}

internal sealed class LinkExternalLoginCommandHandler(
    IExternalIdentityValidator validator,
    IExternalLoginRepository externalLogins,
    IUserRepository users,
    IIdentityUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<LinkExternalLoginCommand, Result>
{
    public async Task<Result> Handle(LinkExternalLoginCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty || string.IsNullOrWhiteSpace(request.Credential)
            || request.Credential.Length > 16_384
            || request.Provider == ExternalAuthProvider.Apple
            && (string.IsNullOrWhiteSpace(request.Nonce) || request.Nonce.Length > 512))
            return Result.Failure(ExternalAuthErrors.InvalidRequest);

        var user = await users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null || !user.IsActive) return Result.Failure(UserErrors.NotFound);
        var validation = await validator.ValidateAsync(
            request.Provider, request.Credential, request.Nonce, cancellationToken);
        if (!validation.IsSuccess) return Result.Failure(ExternalAuthSupport.MapFailure(validation.Failure));
        var identity = validation.Identity!;
        if (await externalLogins.GetAsync(identity.Provider, identity.ProviderUserId, cancellationToken) is not null)
            return Result.Failure(ExternalAuthErrors.LoginAlreadyLinked);
        await externalLogins.AddAsync(ExternalLogin.Create(user.Id, identity.Provider,
            identity.ProviderUserId, identity.Email, timeProvider.GetUtcNow().UtcDateTime), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

using DogPlatform.Matching.Application.Clients.Pets;
using DogPlatform.Matching.Application.Security;
using DogPlatform.Matching.Domain.Errors;
using DogPlatform.Matching.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Matching.Application.Features.AddFavorite;

public sealed class AddFavoriteCommandHandler : IRequestHandler<AddFavoriteCommand, Result>
{
    private readonly IFavoriteCandidateRepository _favoriteRepository;
    private readonly IPetsMatchingClient _petsClient;
    private readonly IMatchingUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public AddFavoriteCommandHandler(
        IFavoriteCandidateRepository favoriteRepository,
        IPetsMatchingClient petsClient,
        IMatchingUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _favoriteRepository = favoriteRepository;
        _petsClient = petsClient;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(AddFavoriteCommand request, CancellationToken cancellationToken)
    {
        if (request.PetId == request.CandidatePetId)
            return Result.Failure(MatchingErrors.SamePet);

        var sourcePet = await _petsClient.GetPetForMatchingAsync(request.PetId, cancellationToken);
        if (sourcePet is null || sourcePet.IsDeleted)
            return Result.Failure(MatchingErrors.PetNotFound);

        if (sourcePet.OwnerId != _currentUser.UserId)
            return Result.Failure(MatchingErrors.Unauthorized);

        var exists = await _favoriteRepository.ExistsAsync(
            request.PetId, request.CandidatePetId, cancellationToken);
        if (exists)
            return Result.Failure(MatchingErrors.DuplicateFavorite);

        var creation = Domain.Aggregates.FavoriteCandidate.FavoriteCandidate.Create(
            request.PetId,
            sourcePet.OwnerId,
            request.CandidatePetId,
            _timeProvider.GetUtcNow().UtcDateTime);

        if (creation.IsFailure)
            return Result.Failure(creation.Error);

        _favoriteRepository.Add(creation.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

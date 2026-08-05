using DogPlatform.Matching.Application.Security;
using DogPlatform.Matching.Domain.Errors;
using DogPlatform.Matching.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;

namespace DogPlatform.Matching.Application.Features.RemoveFavorite;

public sealed class RemoveFavoriteCommandHandler : IRequestHandler<RemoveFavoriteCommand, Result>
{
    private readonly IFavoriteCandidateRepository _favoriteRepository;
    private readonly IMatchingUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public RemoveFavoriteCommandHandler(
        IFavoriteCandidateRepository favoriteRepository,
        IMatchingUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _favoriteRepository = favoriteRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(RemoveFavoriteCommand request, CancellationToken cancellationToken)
    {
        var favorite = await _favoriteRepository.GetAsync(
            request.PetId, request.CandidatePetId, cancellationToken);

        if (favorite is null)
            return Result.Failure(MatchingErrors.FavoriteNotFound);

        if (favorite.SourceOwnerId != _currentUser.UserId)
            return Result.Failure(MatchingErrors.Unauthorized);

        _favoriteRepository.Remove(favorite);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

using MediatR;

namespace DogPlatform.Pets.Application.Features.Pets.GetVaccinationContexts;

public sealed record PetVaccinationContextResponse(
    Guid UserId,
    Guid PetId,
    int SpeciesId,
    DateTime? BirthDate,
    string PetName);

public sealed record GetVaccinationContextsQuery
    : IRequest<IReadOnlyCollection<PetVaccinationContextResponse>>;

public interface IPetVaccinationContextQueryService
{
    Task<IReadOnlyCollection<PetVaccinationContextResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);
}

public sealed class GetVaccinationContextsQueryHandler(
    IPetVaccinationContextQueryService queryService)
    : IRequestHandler<GetVaccinationContextsQuery, IReadOnlyCollection<PetVaccinationContextResponse>>
{
    public Task<IReadOnlyCollection<PetVaccinationContextResponse>> Handle(
        GetVaccinationContextsQuery request, CancellationToken cancellationToken) =>
        queryService.GetAllAsync(cancellationToken);
}

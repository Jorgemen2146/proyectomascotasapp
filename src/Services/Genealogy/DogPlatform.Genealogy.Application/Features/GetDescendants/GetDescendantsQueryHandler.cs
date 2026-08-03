using DogPlatform.Genealogy.Application.Options;
using DogPlatform.Genealogy.Application.Security;
using DogPlatform.Genealogy.Application.Services;
using DogPlatform.Genealogy.Domain.Errors;
using DogPlatform.Genealogy.Domain.Repositories;
using DogPlatform.SharedKernel.Primitives;
using MediatR;
using Microsoft.Extensions.Options;

namespace DogPlatform.Genealogy.Application.Features.GetDescendants;

public sealed class GetDescendantsQueryHandler
    : IRequestHandler<GetDescendantsQuery, Result<IReadOnlyList<DescendantResponse>>>
{
    private readonly IPetLineageRepository _lineageRepo;
    private readonly IPetVerificationService _petVerification;
    private readonly ICurrentUser _currentUser;
    private readonly GenealogyOptions _options;

    public GetDescendantsQueryHandler(
        IPetLineageRepository lineageRepo,
        IPetVerificationService petVerification,
        ICurrentUser currentUser,
        IOptions<GenealogyOptions> options)
    {
        _lineageRepo     = lineageRepo;
        _petVerification = petVerification;
        _currentUser     = currentUser;
        _options         = options.Value;
    }

    public async Task<Result<IReadOnlyList<DescendantResponse>>> Handle(
        GetDescendantsQuery request,
        CancellationToken cancellationToken)
    {
        var depth = Math.Clamp(request.Depth ?? _options.DefaultTreeDepth, 1, _options.MaximumTreeDepth);

        // Privacy policy: the model currently has no concept of "public" pets, so the
        // safest policy is applied: only the owner of the root pet may query its
        // descendants. Descendant pets may belong to other owners; only PetId-level
        // data is returned for them (no owner-sensitive fields).
        var owns = await _petVerification.PetBelongsToOwnerAsync(
            request.PetId, _currentUser.UserId, cancellationToken);

        if (!owns)
            return Result.Failure<IReadOnlyList<DescendantResponse>>(GenealogyErrors.Unauthorized);

        var results = new List<DescendantResponse>();

        // frontier: petId -> paths describing how it was reached from the root ("father"/"mother" per hop)
        var frontier = new Dictionary<Guid, List<string>> { [request.PetId] = new() };
        var visited = new HashSet<Guid> { request.PetId };
        var generation = 1;

        while (frontier.Count > 0 && generation <= depth)
        {
            if (visited.Count + frontier.Count > _options.MaximumTraversalNodes)
                return Result.Failure<IReadOnlyList<DescendantResponse>>(GenealogyErrors.MaximumTraversalExceeded);

            var children = await _lineageRepo.GetChildrenByParentIdsAsync(frontier.Keys, cancellationToken);

            var nextFrontier = new Dictionary<Guid, List<string>>();

            foreach (var child in children)
            {
                if (visited.Contains(child.PetId))
                    continue; // cycle-safe: never revisit an already-seen pet

                var parentPaths = new List<string>();

                if (child.FatherId is Guid f && frontier.TryGetValue(f, out var fatherPaths))
                    parentPaths.Add(fatherPaths.Count == 0 ? "father" : $"{fatherPaths[0]}.father");

                if (child.MotherId is Guid m && frontier.TryGetValue(m, out var motherPaths))
                    parentPaths.Add(motherPaths.Count == 0 ? "mother" : $"{motherPaths[0]}.mother");

                if (parentPaths.Count == 0)
                    continue;

                nextFrontier[child.PetId] = parentPaths;
            }

            foreach (var (petId, parentPaths) in nextFrontier)
            {
                results.Add(new DescendantResponse(petId, generation, ToRelationship(generation), parentPaths));
                visited.Add(petId);
            }

            frontier = nextFrontier;
            generation++;
        }

        results.Sort((a, b) =>
        {
            var byGeneration = a.Generation.CompareTo(b.Generation);
            return byGeneration != 0 ? byGeneration : a.PetId.CompareTo(b.PetId);
        });

        return Result.Success<IReadOnlyList<DescendantResponse>>(results);
    }

    private static DescendantRelationship ToRelationship(int generation) => generation switch
    {
        1 => DescendantRelationship.Child,
        2 => DescendantRelationship.Grandchild,
        3 => DescendantRelationship.GreatGrandchild,
        _ => DescendantRelationship.Descendant
    };
}

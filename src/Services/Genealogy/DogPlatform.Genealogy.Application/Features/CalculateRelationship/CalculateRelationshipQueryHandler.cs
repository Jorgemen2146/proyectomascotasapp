using DogPlatform.Genealogy.Application.Analysis;
using DogPlatform.Genealogy.Application.Options;
using DogPlatform.Genealogy.Application.Security;
using DogPlatform.Genealogy.Application.Services;
using DogPlatform.Genealogy.Application.Traversal;
using DogPlatform.Genealogy.Domain.Errors;
using DogPlatform.SharedKernel.Primitives;
using MediatR;
using Microsoft.Extensions.Options;

namespace DogPlatform.Genealogy.Application.Features.CalculateRelationship;

/// <summary>
/// Classifies the genealogical relationship between two pets purely from ancestor path
/// distances (never from names), using the same traversal/kinship infrastructure as the
/// statistics feature. See <see cref="ClassifyRelationship"/> for the exact classification
/// rules based on generation distances (n1, n2) to the closest common ancestor.
/// </summary>
public sealed class CalculateRelationshipQueryHandler
    : IRequestHandler<CalculateRelationshipQuery, Result<RelationshipResponse>>
{
    public const string MethodDescription =
        "El tipo de parentesco se determina a partir de las distancias generacionales " +
        "(n1, n2) desde cada mascota hasta su ancestro común más cercano, calculadas " +
        "recorriendo los enlaces de padre/madre registrados (no a partir de nombres). El " +
        "coeficiente de parentesco estimado usa el método de Wright (ver " +
        "WrightKinshipCalculator).";

    private readonly IGenealogyTraversalService _traversal;
    private readonly IKinshipCalculator _kinshipCalculator;
    private readonly IPetVerificationService _petVerification;
    private readonly ICurrentUser _currentUser;
    private readonly GenealogyAnalysisOptions _options;

    public CalculateRelationshipQueryHandler(
        IGenealogyTraversalService traversal,
        IKinshipCalculator kinshipCalculator,
        IPetVerificationService petVerification,
        ICurrentUser currentUser,
        IOptions<GenealogyAnalysisOptions> options)
    {
        _traversal          = traversal;
        _kinshipCalculator  = kinshipCalculator;
        _petVerification    = petVerification;
        _currentUser        = currentUser;
        _options            = options.Value;
    }

    public async Task<Result<RelationshipResponse>> Handle(
        CalculateRelationshipQuery request,
        CancellationToken cancellationToken)
    {
        var depth = Math.Clamp(request.Depth ?? _options.DefaultAnalysisDepth, 1, _options.MaximumAnalysisDepth);

        // Privacy policy: the requesting user must own BOTH pets. Public/cross-owner
        // matching is out of scope for this feature (see documented limitation below).
        var ownsPet1 = await _petVerification.PetBelongsToOwnerAsync(
            request.PetId1, _currentUser.UserId, cancellationToken);
        var ownsPet2 = await _petVerification.PetBelongsToOwnerAsync(
            request.PetId2, _currentUser.UserId, cancellationToken);

        if (!ownsPet1 || !ownsPet2)
            return Result.Failure<RelationshipResponse>(GenealogyErrors.Unauthorized);

        var warnings = new List<string>();

        if (request.PetId1 == request.PetId2)
        {
            return Result.Success(new RelationshipResponse(
                request.PetId1, request.PetId2, RelationshipType.SamePet,
                Array.Empty<CommonAncestorResponse>(), null, 0, 100m, true,
                MethodDescription, new[] { "Ambos identificadores corresponden a la misma mascota." }));
        }

        var graph1 = await _traversal.BuildAncestorGraphAsync(request.PetId1, depth, maxNodes: 1000, cancellationToken);
        var graph2 = await _traversal.BuildAncestorGraphAsync(request.PetId2, depth, maxNodes: 1000, cancellationToken);

        var distances1 = _traversal.EnumerateSelfAndAncestorDistances(request.PetId1, depth, graph1);
        var distances2 = _traversal.EnumerateSelfAndAncestorDistances(request.PetId2, depth, graph2);

        var commonIds = distances1.Keys.Where(distances2.ContainsKey).ToList();

        var commonAncestors = commonIds
            .SelectMany(id => distances1[id].SelectMany(n1 => distances2[id].Select(n2 => (id, n1, n2))))
            .Select(t => new CommonAncestorResponse(t.id, t.n1, t.n2))
            .OrderBy(c => c.DistanceFromPet1 + c.DistanceFromPet2)
            .ToList();

        if (graph1.NodeLimitExceeded || graph2.NodeLimitExceeded)
            warnings.Add("El límite máximo de nodos analizados fue alcanzado; el resultado puede estar incompleto.");

        Guid? closest = null;
        int? minPathLength = null;
        RelationshipType relationshipType;

        var pedigreeIncomplete =
            !graph1.Lineages.ContainsKey(request.PetId1) || !graph2.Lineages.ContainsKey(request.PetId2);

        if (commonAncestors.Count == 0)
        {
            relationshipType = pedigreeIncomplete
                ? RelationshipType.UnknownDueToIncompletePedigree
                : RelationshipType.UnrelatedWithinKnownPedigree;

            if (relationshipType == RelationshipType.UnknownDueToIncompletePedigree)
                warnings.Add("No se registran suficientes datos de linaje para determinar el parentesco con certeza.");
        }
        else
        {
            var best = commonAncestors[0];
            closest = best.AncestorPetId;
            minPathLength = best.DistanceFromPet1 + best.DistanceFromPet2;

            relationshipType = ClassifyRelationship(best, commonAncestors);
        }

        var kinship = _kinshipCalculator.Calculate(request.PetId1, graph1, request.PetId2, graph2, depth);
        warnings.AddRange(kinship.Warnings.Where(w => !warnings.Contains(w)));

        var isCloseRelative = (kinship.Coefficient) >= (decimal)_options.CloseRelationshipCoefficientThreshold;

        return Result.Success(new RelationshipResponse(
            request.PetId1,
            request.PetId2,
            relationshipType,
            commonAncestors,
            closest,
            minPathLength,
            kinship.Percentage,
            isCloseRelative,
            MethodDescription,
            warnings));
    }

    /// <summary>
    /// Classifies the relationship using the generation distances (n1 from pet1, n2 from
    /// pet2) to the closest common ancestor:
    ///   n1=0,n2=1 -> Parent (pet1 IS the ancestor, pet2's direct child of it)
    ///   n1=1,n2=0 -> Child
    ///   n1=0,n2=2 -> Grandparent   n1=2,n2=0 -> Grandchild
    ///   n1=1,n2=1 -> Sibling (Full if two distinct closest ancestors at depth 1, else Half)
    ///   n1=1,n2=2 -> UncleOrAunt   n1=2,n2=1 -> NephewOrNiece
    ///   n1=2,n2=2 -> FirstCousin
    ///   otherwise -> MoreDistantRelative
    /// </summary>
    internal static RelationshipType ClassifyRelationship(
        CommonAncestorResponse best,
        IReadOnlyList<CommonAncestorResponse> allCommon)
    {
        var (n1, n2) = (best.DistanceFromPet1, best.DistanceFromPet2);

        if (n1 == 0 && n2 == 1) return RelationshipType.Parent;
        if (n1 == 1 && n2 == 0) return RelationshipType.Child;
        if (n1 == 0 && n2 == 2) return RelationshipType.Grandparent;
        if (n1 == 2 && n2 == 0) return RelationshipType.Grandchild;

        if (n1 == 1 && n2 == 1)
        {
            var siblingAncestorsAtDepth1 = allCommon.Count(c => c.DistanceFromPet1 == 1 && c.DistanceFromPet2 == 1);
            return siblingAncestorsAtDepth1 >= 2 ? RelationshipType.FullSibling : RelationshipType.HalfSibling;
        }

        if (n1 == 1 && n2 == 2) return RelationshipType.UncleOrAunt;
        if (n1 == 2 && n2 == 1) return RelationshipType.NephewOrNiece;
        if (n1 == 2 && n2 == 2) return RelationshipType.FirstCousin;

        return RelationshipType.MoreDistantRelative;
    }
}

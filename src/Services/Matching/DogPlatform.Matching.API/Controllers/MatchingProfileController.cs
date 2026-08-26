using DogPlatform.Matching.API.Requests;
using DogPlatform.Matching.Application.Features.GetMatchingProfile;
using DogPlatform.Matching.Application.Features.DeactivateMatchingProfile;
using DogPlatform.Matching.Application.Features.UpsertMatchingProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DogPlatform.Matching.API.Controllers;

/// <summary>
/// Manages the matching profile (search preferences) for a pet. OwnerId is
/// always derived from the JWT via ICurrentUser inside the handlers.
/// </summary>
[ApiController]
[Route("api/v1/matching/profiles")]
[Authorize]
public sealed class MatchingProfileController : MatchingApiControllerBase
{
    private readonly IMediator _mediator;

    public MatchingProfileController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateMatchingProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpsertMatchingProfileCommand(
            request.PetId, request.IsActive, request.PreferredBreedIds,
            request.MinimumAgeMonths, request.MaximumAgeMonths, request.RequirePedigree,
            request.RequireGenealogyValidation, request.MaximumEstimatedInbreedingCoefficient,
            request.MinimumCompatibilityScore, request.LookingForSex, request.AllowMixedBreed,
            request.Description, request.AvailableFromUtc), cancellationToken);
        return result.IsFailure ? FromError(result.Error) : StatusCode(201, result.Value);
    }

    /// <summary>Gets the matching profile configured for a pet.</summary>
    [HttpGet("{petId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByPetId(Guid petId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMatchingProfileQuery(petId), cancellationToken);

        return result.IsFailure ? FromError(result.Error) : Ok(result.Value);
    }

    /// <summary>Creates or updates the matching profile for a pet.</summary>
    [HttpPut("{petId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Upsert(
        Guid petId,
        [FromBody] UpsertMatchingProfileRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpsertMatchingProfileCommand(
            petId,
            request.IsActive,
            request.PreferredBreedIds,
            request.MinimumAgeMonths,
            request.MaximumAgeMonths,
            request.RequirePedigree,
            request.RequireGenealogyValidation,
            request.MaximumEstimatedInbreedingCoefficient,
            request.MinimumCompatibilityScore,
            request.LookingForSex,
            request.AllowMixedBreed,
            request.Description,
            request.AvailableFromUtc);

        var result = await _mediator.Send(command, cancellationToken);

        return result.IsFailure ? FromError(result.Error) : Ok(result.Value);
    }

    [HttpDelete("{matchingProfileId:guid}")]
    public async Task<IActionResult> Deactivate(Guid matchingProfileId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new DeactivateMatchingProfileCommand(matchingProfileId), cancellationToken);
        return result.IsFailure ? FromError(result.Error) : NoContent();
    }
}

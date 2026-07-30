using DogPlatform.Pets.Application.Features.PetPhotos.Create;
using DogPlatform.Pets.Application.Features.PetPhotos.Delete;
using DogPlatform.Pets.Application.Features.PetPhotos.GetByPet;
using DogPlatform.Pets.Application.Features.PetPhotos.SetMain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DogPlatform.Pets.API.Controllers;

[ApiController]
[Route("api/v1/pets/{petId:guid}/photos")]
[Authorize]
public sealed class PetPhotosController : ControllerBase
{
    private readonly IMediator _mediator;

    public PetPhotosController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Add a photo to a pet.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddPhoto(
        [FromRoute] Guid petId,
        [FromBody] AddPetPhotoRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddPetPhotoCommand(petId, request.ImageUrl);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "Pet.NotFound" => NotFound(),
                "Pet.AlreadyDeleted" => NotFound(),
                "Pet.Unauthorized" => Forbid(),
                _ => BadRequest(result.Error)
            };
        }

        return CreatedAtAction(
            nameof(GetPhotos),
            new { petId },
            result.Value);
    }

    /// <summary>
    /// Get all active photos for a pet.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPhotos(
        [FromRoute] Guid petId,
        CancellationToken cancellationToken)
    {
        var query = new GetPetPhotosQuery(petId);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "Pet.NotFound" => NotFound(),
                "Pet.Unauthorized" => Forbid(),
                _ => BadRequest(result.Error)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Set a photo as the main photo for a pet.
    /// </summary>
    [HttpPut("{photoId:guid}/main")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetMainPhoto(
        [FromRoute] Guid petId,
        [FromRoute] Guid photoId,
        CancellationToken cancellationToken)
    {
        var command = new SetMainPetPhotoCommand(petId, photoId);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "Pet.NotFound" => NotFound(),
                "Pet.Photo.NotFound" => NotFound(),
                "Pet.Unauthorized" => Forbid(),
                _ => BadRequest(result.Error)
            };
        }

        return NoContent();
    }

    /// <summary>
    /// Delete a photo from a pet.
    /// </summary>
    [HttpDelete("{photoId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePhoto(
        [FromRoute] Guid petId,
        [FromRoute] Guid photoId,
        CancellationToken cancellationToken)
    {
        var command = new DeletePetPhotoCommand(petId, photoId);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "Pet.NotFound" => NotFound(),
                "Pet.Photo.NotFound" => NotFound(),
                "Pet.Unauthorized" => Forbid(),
                _ => BadRequest(result.Error)
            };
        }

        return NoContent();
    }
}

/// <summary>Request body for adding a pet photo.</summary>
public sealed record AddPetPhotoRequest(string ImageUrl);

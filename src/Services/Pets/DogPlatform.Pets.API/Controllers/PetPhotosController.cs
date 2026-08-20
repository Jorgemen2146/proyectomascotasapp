using System.Text;
using DogPlatform.Pets.Application.Features.PetPhotos.ConfirmUpload;
using DogPlatform.Pets.Application.Features.PetPhotos.Create;
using DogPlatform.Pets.Application.Features.PetPhotos.CreateUploadUrl;
using DogPlatform.Pets.Application.Features.PetPhotos.Delete;
using DogPlatform.Pets.Application.Features.PetPhotos.GetByPet;
using DogPlatform.Pets.Application.Features.PetPhotos.SetMain;
using DogPlatform.Pets.Application.Features.PetPhotos.Upload;
using DogPlatform.Pets.Application.Features.PetPhotos.GetContent;
using DogPlatform.Pets.API.Requests.PetPhotos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

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

    // ── S3 pre-signed upload flow ────────────────────────────────────────────

    /// <summary>
    /// Step 1: Request a pre-signed S3 PUT URL. The frontend uploads the image directly to S3.
    /// </summary>
    [HttpPost("upload-url")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateUploadUrl(
        [FromRoute] Guid petId,
        [FromBody] CreateUploadUrlRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreatePetPhotoUploadUrlCommand(
            petId,
            request.FileName,
            request.ContentType,
            request.FileSize);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "Pet.NotFound"       => NotFound(),
                "Pet.AlreadyDeleted" => NotFound(),
                "Pet.Unauthorized"   => Forbid(),
                _                    => BadRequest(result.Error)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Receives raw image bytes for an upload URL generated by the Local provider.
    /// </summary>
    [HttpPut("upload/{uploadToken}")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [Consumes("image/jpeg", "image/png", "image/webp")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> UploadLocal(
        [FromRoute] Guid petId,
        [FromRoute] string uploadToken,
        CancellationToken cancellationToken)
    {
        var contentType = Request.ContentType?.Split(';', 2)[0].Trim() ?? string.Empty;
        var result = await _mediator.Send(new UploadPetPhotoCommand(
            petId,
            uploadToken,
            contentType,
            Request.ContentLength,
            Request.Body), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "Pet.NotFound" => NotFound(),
                "Pet.Unauthorized" => Forbid(),
                _ => BadRequest(result.Error)
            };
        }

        return NoContent();
    }

    /// <summary>Returns confirmed local image content to an authenticated owner.</summary>
    [HttpGet("content/{encodedObjectKey}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContent(
        [FromRoute] Guid petId,
        [FromRoute] string encodedObjectKey,
        CancellationToken cancellationToken)
    {
        string objectKey;
        try
        {
            objectKey = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedObjectKey));
        }
        catch (FormatException)
        {
            return NotFound();
        }

        var result = await _mediator.Send(
            new GetPetPhotoContentQuery(petId, objectKey),
            cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "Pet.Unauthorized" => Forbid(),
                _ => NotFound()
            };
        }

        return File(
            result.Value.Content,
            result.Value.ContentType,
            enableRangeProcessing: true);
    }

    /// <summary>
    /// Step 2: Confirm a completed S3 upload. Registers the photo in the database.
    /// </summary>
    [HttpPost("confirm")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmUpload(
        [FromRoute] Guid petId,
        [FromBody] ConfirmUploadRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ConfirmPetPhotoUploadCommand(petId, request.ObjectKey);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "Pet.NotFound"              => NotFound(),
                "Pet.AlreadyDeleted"        => NotFound(),
                "Pet.Photo.ObjectNotFound"  => BadRequest(result.Error),
                "Pet.Photo.InvalidObjectKey"=> BadRequest(result.Error),
                "Pet.Unauthorized"          => Forbid(),
                "Pet.Photo.Duplicate"       => Conflict(result.Error),
                _                           => BadRequest(result.Error)
            };
        }

        return CreatedAtAction(nameof(GetPhotos), new { petId }, result.Value);
    }

    // ────────────────────────────────────────────────────────────────────────────

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

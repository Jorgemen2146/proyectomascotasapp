using DogPlatform.Health.Application.Features.Vaccinations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DogPlatform.Health.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/health/pets/{petId:guid}")]
public sealed class PetVaccinationsController : HealthApiControllerBase
{
    private readonly IMediator _mediator;
    public PetVaccinationsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("vaccinations")]
    public async Task<IActionResult> GetHistory(Guid petId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPetVaccinationsQuery(petId), cancellationToken);
        return result.IsFailure ? FromError(result.Error) : Ok(result.Value);
    }

    [HttpPost("vaccinations")]
    public async Task<IActionResult> Create(Guid petId, CreateVaccinationRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateVaccinationCommand(petId, request.VaccineId, request.DoseNumber,
            request.AppliedAtUtc.UtcDateTime, request.VeterinarianName, request.ClinicName, request.BatchNumber, request.Notes);
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsFailure ? FromError(result.Error) : Created($"/api/v1/health/pets/{petId:D}/vaccinations", result.Value);
    }

    [HttpPut("vaccinations/{petVaccinationId:guid}")]
    public async Task<IActionResult> Update(Guid petId, Guid petVaccinationId, UpdateVaccinationRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateVaccinationCommand(petId, petVaccinationId, request.DoseNumber,
            request.AppliedAtUtc.UtcDateTime, request.VeterinarianName, request.ClinicName, request.BatchNumber, request.Notes);
        var result = await _mediator.Send(command, cancellationToken);
        return result.IsFailure ? FromError(result.Error) : Ok(result.Value);
    }

    [HttpDelete("vaccinations/{petVaccinationId:guid}")]
    public async Task<IActionResult> Delete(Guid petId, Guid petVaccinationId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteVaccinationCommand(petId, petVaccinationId), cancellationToken);
        return result.IsFailure ? FromError(result.Error) : NoContent();
    }

    [HttpGet("vaccination-status")]
    public async Task<IActionResult> GetStatus(Guid petId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPetVaccinationStatusQuery(petId), cancellationToken);
        return result.IsFailure ? FromError(result.Error) : Ok(result.Value);
    }
}

public sealed record CreateVaccinationRequest(int VaccineId, int? DoseNumber, DateTimeOffset AppliedAtUtc,
    string? VeterinarianName, string? ClinicName, string? BatchNumber, string? Notes);
public sealed record UpdateVaccinationRequest(int? DoseNumber, DateTimeOffset AppliedAtUtc,
    string? VeterinarianName, string? ClinicName, string? BatchNumber, string? Notes);

namespace DogPlatform.Matching.API.Requests;

public sealed record CreateMatchRequestRequest(Guid PetId, Guid CandidatePetId, string? Message);

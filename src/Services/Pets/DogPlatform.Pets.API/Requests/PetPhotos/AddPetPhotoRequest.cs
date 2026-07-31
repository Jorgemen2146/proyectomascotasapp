namespace DogPlatform.Pets.API.Requests.PetPhotos;

/// <summary>Request body for adding a pet photo via direct URL (legacy).</summary>
public sealed record AddPetPhotoRequest(string ImageUrl);

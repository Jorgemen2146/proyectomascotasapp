namespace DogPlatform.Pets.API.Requests.PetPhotos;

public sealed record AddPetPhotoRequest(
    string FileName,
    string ContentType,
    string ImageBase64);

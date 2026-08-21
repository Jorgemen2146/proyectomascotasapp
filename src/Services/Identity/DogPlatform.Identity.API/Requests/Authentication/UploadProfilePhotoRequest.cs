namespace DogPlatform.Identity.API.Requests.Authentication;

public sealed record UploadProfilePhotoRequest(
    string FileName,
    string ContentType,
    string ImageBase64);

namespace DogPlatform.Pets.Application.Features.PetPhotos;

internal static class PetPhotoUrls
{
    public static string Content(Guid petId, Guid photoId) =>
        $"/api/v1/pets/{petId:D}/photos/{photoId:D}/content";
}

namespace DogPlatform.Pets.Application.Storage;

public sealed record PhotoContent(
    Stream Content,
    string ContentType,
    long ContentLength);

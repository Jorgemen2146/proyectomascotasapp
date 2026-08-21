using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Identity.Infrastructure.Storage;

internal static class ProfileImageValidation
{
    private const int MaximumBytes = 10 * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, string[]> Extensions =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = [".jpg", ".jpeg"],
            ["image/png"] = [".png"],
            ["image/webp"] = [".webp"]
        };

    public static bool TryValidate(
        byte[] content, string contentType, string fileName,
        out string safeExtension, out string normalizedContentType, out Error error)
    {
        safeExtension = string.Empty;
        normalizedContentType = string.Empty;
        error = Error.None;

        var matchedType = Extensions.Keys.FirstOrDefault(
            type => type.Equals(contentType, StringComparison.OrdinalIgnoreCase));
        if (matchedType is null)
        {
            error = Error.Validation("Profile.Photo.InvalidContentType", "Only JPEG, PNG and WebP images are allowed.");
            return false;
        }

        var allowedExtensions = Extensions[matchedType];
        var requestedExtension = Path.GetExtension(fileName);
        if (!allowedExtensions.Contains(requestedExtension, StringComparer.OrdinalIgnoreCase))
        {
            error = Error.Validation("Profile.Photo.ExtensionMismatch", "The file extension does not match its declared image type.");
            return false;
        }

        if (content.Length == 0 || content.Length > MaximumBytes)
        {
            error = Error.Validation("Profile.Photo.InvalidSize", "The image must be between 1 byte and 10 MB.");
            return false;
        }

        if (!HasExpectedSignature(matchedType, content))
        {
            error = Error.Validation("Profile.Photo.ContentSignatureMismatch", "The file content does not match its declared image type.");
            return false;
        }

        normalizedContentType = matchedType;
        safeExtension = matchedType == "image/jpeg" ? ".jpg" : allowedExtensions[0];
        return true;
    }

    private static bool HasExpectedSignature(string contentType, ReadOnlySpan<byte> content) =>
        contentType switch
        {
            "image/jpeg" => content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF,
            "image/png" => content.Length >= 8 && content[..8].SequenceEqual(
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            "image/webp" => content.Length >= 12 && content[..4].SequenceEqual("RIFF"u8) &&
                content.Slice(8, 4).SequenceEqual("WEBP"u8),
            _ => false
        };
}

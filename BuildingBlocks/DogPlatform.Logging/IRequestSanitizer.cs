namespace DogPlatform.Logging;

public interface IRequestSanitizer
{
    string SanitizeJson(string value);
    string SanitizeQueryString(string value);
    string SanitizeText(string value);
    bool IsSensitiveName(string name);
}

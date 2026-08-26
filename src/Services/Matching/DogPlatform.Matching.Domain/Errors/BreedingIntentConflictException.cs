namespace DogPlatform.Matching.Domain.Errors;

public sealed class BreedingIntentConflictException : Exception
{
    public BreedingIntentConflictException(Exception innerException)
        : base("A concurrent open breeding intent already exists for this match.", innerException)
    {
    }
}

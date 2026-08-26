namespace DogPlatform.Matching.API.Requests;

public sealed record ProposeBreedingIntentRequest(string? Notes, DateTime? ExpectedDateUtc);

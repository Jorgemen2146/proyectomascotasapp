namespace DogPlatform.Genealogy.Application.Analysis;

/// <summary>
/// Result of an inbreeding coefficient estimation for a single pet.
/// </summary>
/// <param name="Coefficient">Estimated coefficient F, in the [0, 1] range (decimal precision).</param>
/// <param name="Percentage">Coefficient expressed as a percentage (Coefficient * 100).</param>
/// <param name="CommonAncestorCount">Number of distinct ancestors shared by both parents that contributed to F.</param>
/// <param name="CalculationMethod">Human readable description of the exact formula/method applied.</param>
/// <param name="Warnings">Data-quality or methodological warnings (e.g. incomplete pedigree, depth truncation).</param>
public sealed record InbreedingResult(
    decimal Coefficient,
    decimal Percentage,
    int CommonAncestorCount,
    string CalculationMethod,
    IReadOnlyList<string> Warnings);

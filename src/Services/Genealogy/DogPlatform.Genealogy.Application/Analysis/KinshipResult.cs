namespace DogPlatform.Genealogy.Application.Analysis;

/// <summary>
/// Result of estimating the kinship (relationship) coefficient between two pets.
/// </summary>
/// <param name="Coefficient">Estimated additive relationship/kinship coefficient (2x the coefficient of
/// coancestry, expressed in the [0,1] range), computed via the same Wright path-counting method.</param>
/// <param name="Percentage">Coefficient expressed as a percentage.</param>
/// <param name="CalculationMethod">Description of the exact method applied.</param>
/// <param name="Warnings">Data-quality or methodological warnings.</param>
public sealed record KinshipResult(
    decimal Coefficient,
    decimal Percentage,
    string CalculationMethod,
    IReadOnlyList<string> Warnings);

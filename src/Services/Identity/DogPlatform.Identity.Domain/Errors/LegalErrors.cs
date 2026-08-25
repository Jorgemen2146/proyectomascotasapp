using DogPlatform.SharedKernel.Primitives;

namespace DogPlatform.Identity.Domain.Errors;

public static class LegalErrors
{
    public static readonly Error ConsentRequired = Error.Validation(
        "LEGAL_CONSENT_REQUIRED", "Debes aceptar todos los documentos legales requeridos.");

    public static readonly Error DocumentNotFound = Error.NotFound(
        "LEGAL_DOCUMENT_NOT_FOUND", "El documento legal activo no existe.");

    public static readonly Error DocumentVersionInvalid = Error.Validation(
        "LEGAL_DOCUMENT_VERSION_INVALID", "La versión del documento legal no es válida o ya no está activa.");

    public static readonly Error ConsentAlreadyExists = Error.Conflict(
        "LEGAL_CONSENT_ALREADY_EXISTS", "El documento legal ya fue aceptado por el usuario.");
}
